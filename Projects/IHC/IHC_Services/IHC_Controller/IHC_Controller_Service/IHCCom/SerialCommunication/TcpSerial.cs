using IHC_Controller_Service.IHCCom;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace IHC_Controller_Service.IHCCom.SerialCommunication;

public class TcpSerial(string hostname, int port, ILogger<RS485Controller> logger) : ISerialCommunication
{
    private readonly TcpClient client = new();
    private BinaryReader? reader;
    private BinaryWriter? writer;

    public bool Connected { get; private set; }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (stoppingToken.IsCancellationRequested == false)
        {
            Connect(stoppingToken);

            Task.Delay(1000, stoppingToken).Wait(stoppingToken);
        }

        CloseConnection();
    }

    private void Connect(CancellationToken stoppingToken)
    {
        if (client.Connected)
        {
            return;
        }

        while (stoppingToken.IsCancellationRequested == false && client.Connected == false)
        {
            logger.LogInformation($"Connecting to RS485 interface at {hostname}:{port}");
            try
            {
                client.Connect(hostname, port);
            }
            catch (Exception ex)
            {
                logger.LogError($"Could not connect to {hostname}:{port}");
                logger.LogError($"Exception Details: {ex.Message}");
                Task.Delay(1000, stoppingToken).Wait(stoppingToken);
            }

        }
        logger.LogInformation($"Connected to {hostname}:{port}");

        NetworkStream stream = client.GetStream();
        reader = new(stream);
        writer = new(stream);
        Connected = true;
    }

    private void CloseConnection()
    {
        try
        {
            writer?.Close();
            reader?.Close();
            client.Close();
        }
        catch (Exception) {}

        Connected = false;
    }

    public byte ReadByte()
    {
        try
        {
            return reader?.ReadByte() ?? throw new InvalidOperationException();
        }
        catch (Exception)
        {
            CloseConnection();
            return 0;
        }
    }

    public void Write(byte[] data)
    {
        try
        {
            writer?.Write(data);

        }
        catch (Exception)
        {
            CloseConnection();
        }
    }
}