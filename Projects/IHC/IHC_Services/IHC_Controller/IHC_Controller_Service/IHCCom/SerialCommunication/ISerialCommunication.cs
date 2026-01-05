
namespace IHC_Controller_Service.IHCCom.SerialCommunication
{
    public interface ISerialCommunication
    {
        bool Connected { get; }

        Task ExecuteAsync(CancellationToken stoppingToken);
        byte ReadByte();
        void Write(byte[] data);
    }
}