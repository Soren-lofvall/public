using IHC_Controller_Service.IHCCom.SerialCommunication;
using IHC_Controller_Service.IHCModules;
using IHCShared;
using IHCShared.IHCController;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Utilities.MessageQueue;

namespace IHC_Controller_Service.IHCCom;

public sealed class RS485Controller(
    IMessageQueueRegistry<IHCClientCommand> commandQueueRegistry,
    Modules Modules,
    IConfiguration config,
    ILogger<RS485Controller> logger) : BackgroundService
{
    private readonly ConcurrentQueue<RS485Packet> packetQueue = [];
    ISerialCommunication? serial;

    private async Task StartSerialCommunication(CancellationToken stoppingToken)
    {
        var serialConfig = config.GetValue<string>("IHCController:Connection");
        ArgumentException.ThrowIfNullOrEmpty(serialConfig);

        if (serialConfig.Equals("Tcp", StringComparison.OrdinalIgnoreCase))
        {
            var hostname = config.GetValue<string>("IHCController:Tcp:HostName");
            ArgumentNullException.ThrowIfNull(hostname);

            var port = config.GetValue<int>("IHCController:Tcp:Port");
            if (port == 0) throw new ArgumentException("Port number cannot be 0");

            serial = new TcpSerial(hostname, port, logger);
        }

        else if (serialConfig.Equals("Serial", StringComparison.OrdinalIgnoreCase))
        {
            var port = config.GetValue<string>("IHCController:Serial:Port");
            ArgumentNullException.ThrowIfNull(port);

            var baud = config.GetValue<int>("IHCController:Serial:Baud");
            if (baud == 0) throw new ArgumentException("Baud rate cannot be 0");

            serial = new UARTSerial(port, baud, logger);
        }
        else
        {
            throw new ArgumentException($"Invalid IHCController connection type: {serialConfig}");
        }

        _ = Task.Run(async () => await serial.ExecuteAsync(stoppingToken), stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retreiveInputs = config.GetValue<bool>("IHCController:RetreiveInputs", false);
        var inputsPollingInterval = config.GetValue<int>("IHCController:InputsPollingIntervalMs", 200);
        var retreiveOutputs = config.GetValue<bool>("IHCController:RetreiveOutputs", true);
        var outputsPollingInterval = config.GetValue<int>("IHCController:OutputsPollingIntervalMs", 500);

        commandQueueRegistry.AddCallbackConsumer(async (message, ct) => await IHCCommandHandlerAsync(message));

        await StartSerialCommunication(stoppingToken);

        while (stoppingToken.IsCancellationRequested == false && serial!.Connected == false)
        {
            logger.LogInformation($"{DateTime.Now} - Waiting for serial connection to RS485 interface");
            Task.Delay(500, stoppingToken).Wait(stoppingToken);
        }

        logger.LogInformation($"{DateTime.Now} - Connected to serial");

        RS485Packet inputPacket = new(Definitions.ID_IHC, Definitions.GET_INPUTS, null);
        RS485Packet outputPacket = new(Definitions.ID_IHC, Definitions.GET_OUTPUTS, null);

        // Get output states at startup
        packetQueue.Enqueue(outputPacket);

        var inputsStopWatch = Stopwatch.StartNew();
        var outputsStopWatch = Stopwatch.StartNew();
        bool sendRetreiveInputNext = false;

        while (stoppingToken.IsCancellationRequested == false && serial!.Connected)
        {
            RS485Packet packet = await GetPacketAsync(serial, Definitions.ID_PC, stoppingToken);
            if (packet == null) continue;

            if (stoppingToken.IsCancellationRequested)
                break;

            switch (packet.DataType)
            {
                case Definitions.ACK:
                    logger.LogDebug("IHCInterface: Packet was ACK'ed");
                    break;

                case Definitions.DATA_READY:
                    logger.LogDebug($"{DateTime.Now} - IHCInterface: Data ready received");
                    if (!packetQueue.IsEmpty)
                    {
                        SendQueueData();
                    }
                    else
                    {
                        if (sendRetreiveInputNext)
                        {
                            if (inputsStopWatch.ElapsedMilliseconds < inputsPollingInterval)
                                continue;

                            logger.LogDebug($"{DateTime.Now} - IHCInterface: Sending input states request");
                            logger.LogTrace(inputPacket.ToString());
                            serial.Write([.. inputPacket.Packet]);
                            
                            sendRetreiveInputNext = false;

                            inputsStopWatch.Restart();
                        }
                        else
                        {
                            if (outputsStopWatch.ElapsedMilliseconds < outputsPollingInterval)
                                continue;

                            logger.LogDebug($"{DateTime.Now} - IHCInterface: Sending output states request");
                            logger.LogTrace(outputPacket.ToString());
                            serial.Write([.. outputPacket.Packet]);
                            if (retreiveInputs)
                                sendRetreiveInputNext = true;

                            outputsStopWatch.Restart();
                        }
                    }
                    break;

                case Definitions.INP_STATE:
                    logger.LogDebug($"{DateTime.Now} - IHCInterface: Input states updating...");
                    await Modules.UpdateStates(IHCType.Input, new UpdateStates(packet.Data, packet.DataSumValue), stoppingToken);
                    break;

                case Definitions.OUTP_STATE:
                    logger.LogDebug($"{DateTime.Now} - IHCInterface: Output states updating...");
                    await Modules.UpdateStates(IHCType.Output, new UpdateStates(packet.Data, packet.DataSumValue), stoppingToken);
                    break;
            }

            Task.Delay(50, stoppingToken).Wait(stoppingToken);
        }
    }

    private void SendQueueData()
    {
        bool result = packetQueue.TryDequeue(out RS485Packet? toSend);
        if (result && toSend != null)
        {
            logger.LogDebug($"{DateTime.Now} - IHCInterface: Sending request. Command: (0x{((byte)toSend.DataType):X2})");
            logger.LogDebug(toSend.ToString());
            serial!.Write([.. toSend.Packet]);
        }
    }

    private async Task<RS485Packet> GetPacketAsync(ISerialCommunication serial, byte ReceiverId, CancellationToken stoppingToken)
    {
        int out_of_frame_bytes;

        while (stoppingToken.IsCancellationRequested == false)
        {
            List<byte> packet = [];
            out_of_frame_bytes = 0;
            byte c = serial.ReadByte();

            while (c != Definitions.STX)
            { // Syncing with STX
                out_of_frame_bytes++;
                c = serial.ReadByte();
            }

            packet.Add(c);
            while (c != Definitions.ETB)
            { // Reading frame
                c = serial.ReadByte();
                packet.Add(c);
            }
            c = serial.ReadByte(); // Reading CRC
            packet.Add(c);
            RS485Packet p = new(packet);

            if (p.IsComplete)
            {
                logger.LogDebug(p.ToString());
                if (p.ReceiverId == ReceiverId)
                    return p;
            }
        }

        return new RS485Packet([]); // Only gets here if stoppingToken is cancelled
    }

    private async Task IHCCommandHandlerAsync(IHCClientCommand message)
    {
        switch (message.CommandType)
        {
            case IHCClientCommandType.ActivateInput:
                await ActivateInput(message.ControllerNumber);
                break;
            case IHCClientCommandType.SetOutputState:
                await SetOutputState(message.ControllerNumber, message.State ?? false);
                break;
        }
    }

    private async Task ActivateInput(uint controllerNumber)
    {
        List<byte> data = [(byte)controllerNumber,1];

        RS485Packet inputPacket = new(Definitions.ID_IHC, Definitions.ACT_INPUT, data);
        packetQueue.Enqueue(inputPacket);
    }

    private async Task SetOutputState(uint controllerNumber, bool state)
    {
        List<byte> data = [(byte)controllerNumber,state ? (byte)1 : (byte)0];

        RS485Packet outputPacket = new(Definitions.ID_IHC, Definitions.SET_OUTPUT, data);
        packetQueue.Enqueue(outputPacket);
    }
}
