using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace ESPHome_Server;

public static class FrameHelper
{
    public static async Task<uint> ReadVarUInt32Async(Stream stream, CancellationToken ct)
    {
        uint result = 0; int shift = 0;
        while (true && !ct.IsCancellationRequested)
        {
            int b = stream.ReadByte();
            if (b == -1) 
                throw new EndOfStreamException("Unexpected end of stream while reading varint.");
            
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0) 
                break;
            
            shift += 7;
            if (shift > 35) 
                throw new InvalidDataException("VarInt too long");
        }
        return result;
    }

    public static async Task WriteVarUInt32Async(Stream stream, uint value, CancellationToken ct)
    {
        while (true && !ct.IsCancellationRequested)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0) 
                b |= 0x80;
            await stream.WriteAsync(new[] { b }, 0, 1, ct);
            if (value == 0) break;
        }
    }

    public static async Task WriteMessageAsync(Stream stream, EspHomeApiMessageId type, IMessage message, CancellationToken ct)
    {
        try
        {
            var payload = message.ToByteArray();
            await WriteVarUInt32Async(stream, 0, ct);
            await WriteVarUInt32Async(stream, (uint)payload.Length, ct);
            await WriteVarUInt32Async(stream, (uint)type, ct);
            await stream.WriteAsync(payload, 0, payload.Length, ct);
            await stream.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing message: {ex.Message}");
            throw;
        }
    }

    public static async Task<(EspHomeApiMessageId type, byte[] payload)> ReadMessageAsync(Stream stream, CancellationToken ct)
    {
        uint encryption = await ReadVarUInt32Async(stream, ct);
        uint length = await ReadVarUInt32Async(stream, ct);
        uint type = await ReadVarUInt32Async(stream, ct);
        byte[] payload = new byte[length];
        int read = 0;
        while (read < payload.Length)
        {
            int n = await stream.ReadAsync(payload, read, payload.Length - read, ct);
            if (n == 0) throw new EndOfStreamException("Unexpected EOF while reading payload");
            read += n;
        }
        return ((EspHomeApiMessageId)type, payload);
    }
}
