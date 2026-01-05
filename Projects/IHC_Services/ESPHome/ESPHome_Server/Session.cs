using IHCShared;
using IHCShared.IHCController;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Utilities.MessageQueue;

namespace ESPHome_Server;

public partial class Session(
    TcpClient espHomeConnection, 
    IServiceProvider services)
{
    private readonly Dictionary<uint, uint> deviceIdCache = [];

    private bool _subscribedStates;
    protected readonly NetworkStream networkStream = espHomeConnection.GetStream();
    private readonly string? _password = Environment.GetEnvironmentVariable("ESPHOME_PASSWORD");

    private readonly IConfiguration config = services.GetRequiredService<IConfiguration>();
    private readonly ILogger<ESPHomeServer> logger = services.GetRequiredService<ILogger<ESPHomeServer>>();
    private readonly IHCCache ihcCache = services.GetRequiredService<IHCCache>();
    private readonly IMessageQueue<IHCClientCommand> ihcCommandMessageQueue = services.GetMessageQueue<IHCClientCommand>();
    private readonly IMessageQueueRegistry<TerminalStatusUpdated> statusUpdateRegistry = services.GetMessageQueueRegistry<TerminalStatusUpdated>();

    private static string GetDefaultMacAddress()
    {
        string? adress = NetworkInterface
                        .GetAllNetworkInterfaces()
                        .Where(nic => nic.OperationalStatus == OperationalStatus.Up && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                        .Select(nic => nic.GetPhysicalAddress().ToString())
                        .FirstOrDefault();

        if (adress != null && adress.Length == 12)
        {
            adress = string.Join(":", Enumerable.Range(0, 6).Select(i => adress.Substring(i * 2, 2)));
        }
        return adress ?? "";
    }

    private async Task OnTerminalStatusChanged(IHCTerminal terminal, CancellationToken ct)
    {
        logger.LogInformation($"{terminal.TerminalType} : {terminal.ControllerNumber} : {terminal.State}");

        await SendStateChangeAsync(terminal, terminal.State, ct);
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogDebug("Client connected");

        statusUpdateRegistry.AddCallbackConsumer( async (message, ct) => await OnTerminalStatusChanged(message.Terminal, ct));

        try
        {
            while (espHomeConnection.Connected && stoppingToken.IsCancellationRequested == false)
            {
                if (espHomeConnection.Available == 0)
                {
                    await Task.Delay(100, stoppingToken);
                    continue;
                }

                var (type, payload) = await FrameHelper.ReadMessageAsync(networkStream, stoppingToken);
                logger.LogInformation($"Received message of type: {type}");

                switch (type)
                {
                    case EspHomeApiMessageId.HelloRequest:
                        var helloRequest = HelloRequest.Parser.ParseFrom(payload);
                        await HelloAsync(helloRequest, stoppingToken);
                        break;
                    case EspHomeApiMessageId.AuthenticationRequest:
                        var authenticationRequest = AuthenticationRequest.Parser.ParseFrom(payload);
                        await AuthenticateAsync(authenticationRequest, stoppingToken);
                        break;
                    case EspHomeApiMessageId.DisconnectRequest:
                        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.DisconnectResponse, new DisconnectResponse(), stoppingToken);
                        Console.WriteLine("Client disconnected");
                        return;
                    case EspHomeApiMessageId.PingRequest:
                        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.PingResponse, new PingResponse(), stoppingToken);
                        break;
                    case EspHomeApiMessageId.DeviceInfoRequest:
                        var deviceInfoRequest = DeviceInfoRequest.Parser.ParseFrom(payload);
                        await Device_InfoAsync(deviceInfoRequest, stoppingToken);
                        break;
                    case EspHomeApiMessageId.ListEntitiesRequest:
                        var listEntitiesRequest = ListEntitiesRequest.Parser.ParseFrom(payload);
                        await List_EntitiesAsync(listEntitiesRequest, stoppingToken);
                        break;
                    case EspHomeApiMessageId.SubscribeStatesRequest:
                        var subscribeStatesRequest = SubscribeStatesRequest.Parser.ParseFrom(payload);
                        await Subscribe_StatesAsync(subscribeStatesRequest, stoppingToken);
                        break;
                    case EspHomeApiMessageId.SwitchCommandRequest:
                        var switchCommandRequest = SwitchCommandRequest.Parser.ParseFrom(payload);
                        await Switch_CommandAsync(switchCommandRequest, stoppingToken);
                        break;
                    default:
                        Console.WriteLine($"Unhandled message type: {type}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Session ended: {ex.Message}");
        }
        finally { try { espHomeConnection.Close(); } catch { } }

        logger.LogDebug($"Session ended");
    }

    private async Task HelloAsync(HelloRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        logger.LogInformation($"Hello from client: {request.ClientInfo} v{request.ApiVersionMajor}.{request.ApiVersionMinor}");

        var helloResp = new HelloResponse
        {
            ServerInfo = config.GetValue<string>("ESPHomeServerApi:ServerInfo:Name", "IHC485 ESPHome Server"),
            ApiVersionMajor = request.ApiVersionMajor,
            ApiVersionMinor = request.ApiVersionMinor,
            Name = config.GetValue<string>("ESPHomeServerApi:DeviceName", "ihc_controller_esphome"),
        };
        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.HelloResponse, helloResp, ct);

        logger.LogDebug("Handshake complete");
    }

    private async Task AuthenticateAsync(AuthenticationRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        bool invalid = false;
        if (!string.IsNullOrEmpty(_password)) invalid = request.Password != _password;
        var authResp = new AuthenticationResponse { InvalidPassword = invalid };
        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.AuthenticationResponse, authResp, ct);
        if (invalid) { Console.WriteLine("Invalid password; closing session"); espHomeConnection.Close(); return; }
    }


    private async Task Device_InfoAsync(DeviceInfoRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        uint deviceId = 0;
        IEnumerable<IHCModule> modules = ihcCache.GetAllModules();
        List<DeviceInfo> devices = [];
        foreach (var module in modules)
        {
            deviceId++;
            var deviceInfo = new DeviceInfo
            {
                DeviceId = deviceId,
                Name = module.Name
            };
            deviceIdCache[module.UniqueId] = deviceId;
            devices.Add(deviceInfo);
        }

        DeviceInfo[] deviceInfoList = [.. devices];
        var info = new DeviceInfoResponse
        {
            UsesPassword = !string.IsNullOrEmpty(_password),
            FriendlyName = config.GetValue<string>("ESPHomeServerApi:ServerInformation:FriendlyName", "IHC Controller for ESPHome"),
            Name = config.GetValue<string>("ESPHomeServerApi:DeviceName", "ihc_controller_esphome"),
            MacAddress = GetDefaultMacAddress(),
            ApiEncryptionSupported = false,
            Model = "IHC via RS485 for ESPHome",
            Devices = { deviceInfoList },
            EsphomeVersion = config.GetValue<string>("ESPHomeServerApi:ServerInformation:Version", "0.1.0"),
        };
        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.DeviceInfoResponse, info, ct);
    }

    private async Task List_EntitiesAsync(ListEntitiesRequest? request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (var terminal in ihcCache.GetAllTerminals())
        {
            IHCModule? module = ihcCache.GetModuleForTerminal(terminal);
            if (module == null)
            {
                logger.LogWarning($"Module not found for terminal: {terminal.UniqueId}");
                continue;
            }

            string objectId = config.GetValue<string>("ESPHomeServerApi:DeviceName", "ihc_controller_esphome");
            if (terminal.TerminalType == IHCType.Input)
                objectId += $"_input_{terminal.ControllerNumber}";
            else
                objectId += $"_output_{terminal.ControllerNumber}";

            var ent = new ListEntitiesSwitchResponse
            {
                Key = terminal.UniqueId,
                Name = terminal.Name,
                DisabledByDefault = terminal.DisabledByDefault,
                DeviceId = deviceIdCache[module.UniqueId],
                EntityCategory = EntityCategory.None,
                Icon = terminal.TerminalType == IHCType.Input ? "mdi:gesture-tap-button" : "mdi:power-socket",
                DeviceClass = terminal.TerminalType == IHCType.Input ? "button" : "outlet",
                ObjectId = objectId
            };
            await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.ListEntitiesSwitchResponse, ent, ct);
        }

        await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.ListEntitiesDoneResponse, new ListEntitiesDoneResponse(), ct);
    }

    private async Task Subscribe_StatesAsync(SubscribeStatesRequest? request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        _subscribedStates = true; 
        logger.LogDebug("Client subscribed to states");

        _ = Task.Run(async () =>
        {
            foreach (var terminal in ihcCache.GetAllTerminals())
            {
                if (ct.IsCancellationRequested || !espHomeConnection.Connected)
                    break;

                IHCModule? module = ihcCache.GetModuleForTerminal(terminal);
                if (module == null)
                {
                    logger.LogWarning($"Module not found for terminal: {terminal.UniqueId}");
                    continue;
                }

                var resp = new SwitchStateResponse
                {
                    Key = terminal.UniqueId,
                    State = terminal.State,
                    DeviceId = deviceIdCache[module.UniqueId]
                };
                try { await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.SwitchStateResponse, resp, ct); }
                catch (Exception ex) { logger.LogError($"Push of initial state: {ex.Message}"); break; }
            }
        }, ct);
    }

    private async Task Switch_CommandAsync(SwitchCommandRequest? request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        IHCTerminal? terminal = ihcCache.GetTerminalById(request.Key);
        if (terminal == null)
        {
            logger.LogWarning($"Switch command for unknown terminal: {request.Key}");
            return;
        }

        logger.LogInformation($"Switch command received: Config {request.Key}, New State: {request.State}");
        
        if (!espHomeConnection.Connected) 
            return;

        if (terminal.TerminalType == IHCType.Output)
        {
            IHCClientCommand iHCCommand = new(IHCClientCommandType.SetOutputState, IHCType.Output, terminal.ControllerNumber, request.State);
            await ihcCommandMessageQueue.EnqueueAsync(iHCCommand, ct);
        }
        else
        {
            IHCClientCommand iHCCommand = new(IHCClientCommandType.ActivateInput, IHCType.Input, terminal.ControllerNumber);
            await ihcCommandMessageQueue.EnqueueAsync(iHCCommand, ct);
        }
    }

    private async Task SendStateChangeAsync(IHCTerminal terminal, bool newState, CancellationToken ct)
    {
        if (espHomeConnection.Connected && _subscribedStates)
        {
            logger.LogInformation($"State change: Config {terminal.TerminalType}-{terminal.ControllerNumber}, New State: {newState}");

            IHCModule? module = ihcCache.GetModuleForTerminal(terminal);
            if (module == null)
            {
                logger.LogWarning($"Module not found for terminal: {terminal.UniqueId}");
                return;
            }

            var resp = new SwitchStateResponse
            {
                Key = terminal.UniqueId,
                State = newState,
                DeviceId = deviceIdCache[module.UniqueId]
            };
            await FrameHelper.WriteMessageAsync(networkStream, EspHomeApiMessageId.SwitchStateResponse, resp, ct);
        }
    }
}
