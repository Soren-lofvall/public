
using IHCShared.IHC_Controller;
using IHCShared.IHCController;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Utilities.MessageQueue;

namespace IHCShared
{
    public class IHCClientOptions
    {
        public string? Url { get; set; }
    }

    public class IHCClientService : BackgroundService
    {
        public Uri ControllerUri { get; private set; }

        private readonly IHCClient ihcClient;
        private readonly IHCCache cache;
        private readonly ILogger<IHCClientService> logger;
        private readonly BoundedQueue<IHCClientCommand> commandConsumerQueue;

        private CancellationToken stoppingToken;

        public IHCClientService(IOptions<IHCClientOptions> options, 
            IHCCache cache,
            IMessageQueueRegistry<IHCClientCommand> registry,
            ILogger<IHCClientService> logger)
        {
            this.ControllerUri = new Uri(options.Value.Url ?? throw new ArgumentException("No or wrong URL specified for IHC Hub"));
            this.cache = cache;
            this.commandConsumerQueue = registry.NewQueueConsumer(100);
            this.logger = logger;

            ihcClient = new IHCClient(
                baseUrl: ControllerUri,
                onReconnected: async () => await OnReconnected(),
                onTerminalStatusChanged: async (terminal) => await OnTerminalStatusChanged(terminal)
            );
        }

        private async Task UpdateAllModules()
        {
            var modules = await ihcClient.Server.GetAllModules();
            foreach (var module in modules)
            {
                await cache.UpdateModuleAsync(module);
            }
        }

        private async Task UpdateAllTerminals(CancellationToken stoppingToken)
        {
            var terminals = await ihcClient.Server.GetAllTerminals();
            foreach (var terminal in terminals)
            {
                await cache.UpdateTerminalAsync(terminal, stoppingToken);
            }
        }

        private async Task OnReconnected()
        {
            await UpdateAllModules();
            await UpdateAllTerminals(stoppingToken);
        }

        private async Task OnTerminalStatusChanged(IHCTerminal terminal)
        {
            await cache.UpdateTerminalAsync(terminal, stoppingToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            this.stoppingToken = stoppingToken;

            while (!stoppingToken.IsCancellationRequested)
            {
                while (!stoppingToken.IsCancellationRequested && !ihcClient.IsConnected)
                {
                    logger.LogInformation($"Connecting to IHC Controller Hub at {ihcClient.Uri}");
                    try
                    {
                        await ihcClient.StartAsync(stoppingToken);

                        await UpdateAllModules();
                        await UpdateAllTerminals(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError($"Could not connect to IHC Controller Hub at {ihcClient.Uri}");
                        logger.LogError($"Exception Details: {ex.Message}");
                        Task.Delay(5000, stoppingToken).Wait(stoppingToken);
                    }
                }

                while (!stoppingToken.IsCancellationRequested && ihcClient.IsConnected)
                {
                    var ihcCommand = await commandConsumerQueue.DequeueAsync(stoppingToken);

                    switch (ihcCommand.CommandType)
                    {
                        case IHCClientCommandType.SetOutputState:
                            logger.LogInformation($"Sending SetOuputState to IHC: {ihcCommand.ControllerNumber} : {ihcCommand.State ?? false}");
                            await ihcClient.Server.SetOutputState(ihcCommand.ControllerNumber, ihcCommand.State ?? false);
                            break;
                        case IHCClientCommandType.ActivateInput:
                            logger.LogInformation($"Sending ActivateInput to IHC: {ihcCommand.ControllerNumber}");
                            await ihcClient.Server.ActivateInput(ihcCommand.ControllerNumber);
                            break;
                        default:
                            break;
                    }
                }

                await ihcClient.StopAsync(stoppingToken);
            }
        }
    }
}
