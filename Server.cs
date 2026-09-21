
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace TempAndFanServer
{
    public class Server(string host = "0.0.0.0",int port = 1648)
    {
        public record Data(float CpuTemp, float GpuTemp, float CpuFan, float GpuFan, float Fps)
        {
            public byte[] GetBytes()
            {
                byte[] bytes = new byte[20];
                Buffer.BlockCopy(BitConverter.GetBytes(CpuTemp / 100.0f), 0, bytes, 0, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(GpuTemp / 100.0f), 0, bytes, 4, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(CpuFan / 100.0f), 0, bytes, 8, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(GpuFan / 100.0f), 0, bytes, 12, 4);
                Buffer.BlockCopy(BitConverter.GetBytes(Fps), 0, bytes, 16, 4);

                return bytes;
            }
        }

        public bool ShortFormat { get; set; } = false;
        private readonly string host = host;
        public readonly int port = port;
        private Data data = new(0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
        public event Action<string>? OnLog;
        public event Func<Data>? OnGetData;
        public bool Running { get; private set; }
        private TcpListener? Listener { get; set; }

        private async Task RunServerAsync(CancellationToken token)
        {
            if (Listener == null)
                return;

            while (!token.IsCancellationRequested)
            {
                TcpClient client = await Listener.AcceptTcpClientAsync(token);
                if (client.Client.RemoteEndPoint is not IPEndPoint endpoint)
                {
                    client.Close();
                    return;
                }
                _ = Task.Run(() => HandleClientAsync(client, endpoint, token), token);

            }
            Listener.Stop();
            OnLog?.Invoke("Server stopped");
            
        }

        private async void HandleClientAsync(TcpClient client, IPEndPoint endpoint, CancellationToken token)
        {
            var stream = client.GetStream();
            stream.ReadTimeout = 1000;
            stream.WriteTimeout = 1000;
            int errorCounter = 0;

            OnLog?.Invoke($"New connection with {endpoint}");

            while (!token.IsCancellationRequested && client.Connected)
            {
                Data? data = OnGetData?.Invoke();
                
                if (data == null)
                {
                    OnLog?.Invoke("Can't get stats, disconnecting");
                    break;
                }

                try
                {
                    await stream.WriteAsync(data.GetBytes().AsMemory(0, ShortFormat ? 2 * 4 : 5 * 4), token);
                    await stream.FlushAsync(token);
                    byte[] acknowledge = new byte[1];
                    if (0 == await stream.ReadAsync(acknowledge.AsMemory(0, 1), token))
                        break;
                    
                    errorCounter = 0;
                }
                catch
                {
                    if (++errorCounter == 10)
                        break;
                    OnLog?.Invoke($"Error N° {errorCounter} with client {endpoint}, retrying..");
                    await Task.Delay(500, token);
                }
                await Task.Delay(10,token);
            }
            client.Close();
            OnLog?.Invoke($"Connection with {endpoint.Address} closed");
            
        }

        internal bool Start()
        {
            try
            {
                IPAddress address = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
                Listener = new TcpListener(address, port);
                Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Listener.Start();

                CancellationToken token = new CancellationToken();
                Task.Run(() => RunServerAsync(token));


                Running = true;
                OnLog?.Invoke($"Server started at {host}:{port}");
                return true;
            }
            catch (Exception e)
            {
                OnLog?.Invoke(e.Message);
                Running = false;
                return false;
            }
        }
    }

}