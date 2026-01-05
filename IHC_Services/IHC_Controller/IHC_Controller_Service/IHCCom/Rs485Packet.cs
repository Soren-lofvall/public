using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace IHC_Controller_Service.IHCCom;

internal class RS485Packet
{
    public byte ReceiverId { get; private set; }
    public byte DataType { get; private set; }

    public List<byte> Data { get; private set; } = [];
    public uint DataSumValue { get; private set; }
    public List<byte> Packet { get; private set; } = [];
    public bool IsComplete { get; private set; }

    public RS485Packet(byte receiverId, byte dataType, List<byte>? data)
    {
        ReceiverId = receiverId;
        DataType = dataType;
        IsComplete = false;

        Packet.Add(Definitions.STX);
        Packet.Add((byte)receiverId);
        Packet.Add((byte)dataType);

        if (data != null)
        {
            for (int j = 0; j < data.Count; j++)
            {
                Packet.Add(data[j]);
                Data.Add(data[j]);
                DataSumValue += data[j];
            }
        }
        Packet.Add(Definitions.ETB);

        int crc = 0;
        for (int j = 0; j < Packet.Count; j++)
        {
            crc += Packet[j];
        }
        Packet.Add((byte) (crc & 0xFF));
        IsComplete = true;
    }

    public RS485Packet(List<byte> data)
    {
        if (data.Count > 2 && data[0] == Definitions.STX)
        {
            int crc = 0;

            ReceiverId = data[1];
            DataType = data[2];
            crc += data[0];
            crc += data[1];
            crc += data[2];
            Packet.Add(data[0]);
            Packet.Add(data[1]);
            Packet.Add(data[2]);
            for (int j = 3; j < data.Count; j++)
            {
                Packet.Add(data[j]);
                crc += data[j];
                if (data[j] != (byte)Definitions.ETB)
                {
                    Data.Add(data[j]);
                    DataSumValue += data[j];
                }
                else if (data[j] == (byte)Definitions.ETB)
                {
                    if ((j + 1) == (data.Count - 1))
                    {
                        if (data[j + 1] == (byte)(crc & 0xFF))
                        {
                            Packet.Add(data[j + 1]);
                            IsComplete = true;
                            break;
                        }
                    }
                }
            }
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"{DateTime.Now} - RS485 Packet:");
        sb.Append($"  Receiver ID: {ReceiverId} (0x{((byte)ReceiverId):X2})");
        sb.Append($"  Data Type: {DataType} (0x{((byte)DataType):X2})");
        sb.Append($"  Data Length: {Data.Count}");
        sb.Append($"  Data Sum: {DataSumValue}");
        sb.Append($"  Packet Length: {Packet.Count}");
        sb.Append("  Packet: ");
        foreach (var b in Packet)
        {
            sb.Append($"0x{b:X2} ");
        }
        return sb.ToString();
    }
}
