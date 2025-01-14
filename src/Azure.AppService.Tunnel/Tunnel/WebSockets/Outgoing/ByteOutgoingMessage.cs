using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;

internal class ByteOutgoingMessage(Lifetime lifetime, WebSocket webSocket, byte[] message) : IOutgoingMessage
{
    public bool SilentFailure => false;

    public async Task Execute()
    {
        if (webSocket.GetState() != WebSocketState.Open) return;

        await webSocket.SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true,
            lifetime);
    }
}