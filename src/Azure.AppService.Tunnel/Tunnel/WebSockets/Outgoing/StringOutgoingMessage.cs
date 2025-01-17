using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;

internal class StringOutgoingMessage(Lifetime lifetime, WebSocket webSocket, string message) : IOutgoingMessage
{
    public bool SilentFailure => false;

    public async Task Execute()
    {
        if (webSocket.GetState() != WebSocketState.Open) return;

        await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(message)),
            WebSocketMessageType.Text, true,
            lifetime);
    }
}