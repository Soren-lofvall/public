using System.Globalization;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace IHC_Terminal_Client
{
    internal partial class Program
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial IntPtr GetStdHandle(int nStdHandle);

        [LibraryImport("kernel32.dll")]
        public static partial uint GetLastError();

        static NetworkStream stream;
        static bool exitRequested = false;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("da-DK");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("da-DK");

            Console.Write("Hostname (ihc-pi): ");
            string hostname = Console.ReadLine();
            if (string.IsNullOrEmpty(hostname))
            {
                hostname = "ihc-pi";
            }

            Console.Write("Port (2001): ");
            string port = Console.ReadLine();
            if (string.IsNullOrEmpty(port))
            {
                port = "2001";
            }

            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                Console.ReadKey();
                return;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }

            RunMethod(hostname, port);
        }

        private static void RunMethod(string hostname, string port)
        {
            TcpClient? client = null;

            try
            {
                client = new TcpClient();
                client.Connect(hostname, Int32.Parse(port));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not connect to {hostname}:{port}");
                Console.WriteLine($"Exception Details: {ex.Message}");
                Console.ReadKey();
            }

            if (client != null && client.Connected)
            {
                Console.WriteLine($"Connected to {hostname}:{port}");

                stream = client.GetStream();
                BinaryWriter writer = new(stream);

                Thread thread = new Thread(DataReaderThread);
                thread.IsBackground = true;
                thread.Start(client);

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.X && key.Modifiers == ConsoleModifiers.Control)
                    {
                        exitRequested = true;
                        thread.Join(1000);
                        writer.Flush();
                        writer.Close();
                        client.Close();
                        break;
                    }

                    writer.Write((byte)key.Key);
                }
            }
        }

        static void DataReaderThread(object inObject)
        {
            TcpClient? client = inObject as TcpClient;
            if (client == null)
                return;

            BinaryReader reader = new(stream);

            while (exitRequested == false)
            {
                if (!client.Connected)
                    break;

                int dataSize = client.Available;
                if (dataSize > 0)
                {
                    byte[] data = reader.ReadBytes(dataSize);
                    string dataStr = Encoding.UTF8.GetString(data);
                    Console.Write(dataStr);
                }
                Thread.Sleep(10);
            }

            reader.Close();
        }
    }
}
