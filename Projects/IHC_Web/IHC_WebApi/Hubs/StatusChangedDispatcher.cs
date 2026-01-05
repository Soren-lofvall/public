using IHCShared;
using IHCShared.IHCController;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Win32;
using Utilities.MessageQueue;

namespace IHC_WebApi.Hubs
{
    public class StatusChangedDispatcher
    {
        private readonly IHubContext<IHCTerminalStatusHub> hub;
        private readonly ILogger<StatusChangedDispatcher> logger;

        private const int MaxRetries = 3;

        public StatusChangedDispatcher(
            IHubContext<IHCTerminalStatusHub> hub, 
            IMessageQueueRegistry<TerminalStatusUpdated> statusQueueRegistry,
            ILogger<StatusChangedDispatcher> logger)
        {
            this.hub = hub;
            statusQueueRegistry.AddCallbackConsumer(async (message, ct) => await TerminalStatusUpdatedAsync(message.Terminal, ct));
            this.logger = logger;
        }

        public async Task TerminalStatusUpdatedAsync(IHCTerminal terminal, CancellationToken ct)
        {
            try
            {
                logger.LogInformation($"Dispathing: \"TerminalStatusUpdated\" {terminal.TerminalType} : {terminal.ControllerNumber} : {terminal.State}");
                await hub.Clients.All.SendAsync("TerminalStatusUpdated", terminal, ct);
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to TerminalStatusUpdate. Exception: {ex.Message}");
            }
        }
    }
}
