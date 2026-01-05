using IHC_Controller_Service.IHCCom;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection.PortableExecutable;
using System.Text;

namespace IHC_Controller_Service.IHCCom.SerialCommunication;

public class UARTSerial(string Port, int BaudRate, ILogger<RS485Controller> logger) : ISerialCommunication
{
    public bool Connected => serial?.IsOpen ?? false;

    private SerialPort? serial;

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        serial = new SerialPort(Port, BaudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = 1000,   // ms
            WriteTimeout = 1000  // ms
        };

        while (stoppingToken.IsCancellationRequested == false)
        {
            try
            {
                logger.LogInformation($"Opening serial {Port} @ {BaudRate} baud.");
                serial.Open();
            }
            catch (Exception ex)
            {
                logger.LogError($"Uanble to open serial: {ex.Message}");
                Task.Delay(1000, stoppingToken).Wait(stoppingToken);
            }
        }

        try
        {
            while (stoppingToken.IsCancellationRequested == false)
            {
                Task.Delay(1000, stoppingToken).Wait(stoppingToken);
            }
        }
        finally
        {
            serial.Close();
            serial.Dispose();
        }
    }

    public byte ReadByte()
    {
        if (serial != null)
            return (byte) serial.ReadByte();
        throw new InvalidOperationException();
    }

    public void Write(byte[] data)
    {
        serial?.Write(data, 0, data.Length);
    }
}
