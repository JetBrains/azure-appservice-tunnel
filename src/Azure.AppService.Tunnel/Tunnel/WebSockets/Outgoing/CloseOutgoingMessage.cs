using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;

internal class CloseOutgoingMessage(WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription) : IOutgoingMessage
{
    public bool SilentFailure => true;

    public async Task Execute()
    {
        if (webSocket.StateIs(WebSocketState.Closed, WebSocketState.CloseSent, WebSocketState.Aborted))
        {
            return;
        }

        await webSocket.CloseOutputAsync(closeStatus, statusDescription, CancellationToken.None);
    }
}