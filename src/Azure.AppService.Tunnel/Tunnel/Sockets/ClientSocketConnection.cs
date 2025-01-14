using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.Sockets;

internal class ClientSocketConnection(Lifetime lifetime) : SocketConnection(lifetime)
{
    public Task ConnectAndStartProcessing(int port)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        socket.Connect(new IPEndPoint(IPAddress.Loopback, port));
        Socket.SetIfEmpty(socket);

        return StartProcessingMessages();
    }
}