using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.Sockets;

internal abstract class SocketConnection
{
    private readonly Lifetime _lifetime;
    private readonly ILog _logger = Log.GetLog<SocketConnection>();
    private readonly Signal<byte[]> _dataReceived = new();
    protected readonly ViewableProperty<Socket> Socket = new();
    public ISource<byte[]> DataReceived => _dataReceived;

    protected SocketConnection(Lifetime lifetime)
    {
        _lifetime = lifetime;
        Socket.AdviseOnce(lifetime, socket =>
        {
            lifetime.OnTermination(() => CloseSocket(socket));
        });
    }

    public void Send(byte[] data)
    {
        Socket.Value.Send(data);
    }

    protected Task StartProcessingMessages()
    {
        return Task.Factory.StartNew(() =>
        {
            _logger.Info("Begin processing messages");

            var receiveBuffer = new byte[16384];

            var socket = Socket.Value;

            while (socket.Connected && _lifetime.IsAlive)
            {
                try
                {
                    var receivedBytes = socket.Receive(receiveBuffer);

                    if (receivedBytes > 0)
                        _dataReceived.Fire(receiveBuffer.Take(receivedBytes).ToArray());
                }
                catch (Exception e)
                {
                    switch (e)
                    {
                        case SocketException { SocketErrorCode: SocketError.TimedOut or SocketError.WouldBlock }:
                            continue;

                        case SocketException:
                        case ObjectDisposedException:
                            _logger.Verbose($"Exception during Receive: {e.GetType().Name} {e.Message}");
                            break;

                        default:
                            _logger.Error(e);
                            throw;
                    }
                }
            }
        }, _lifetime, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void CloseSocket(Socket socket)
    {
        _logger.CatchAndDrop(() => socket.Shutdown(SocketShutdown.Both));
        _logger.CatchAndDrop(() => socket.Close(0));
    }
}