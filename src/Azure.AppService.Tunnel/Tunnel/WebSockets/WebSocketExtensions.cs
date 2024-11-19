using System;
using System.Linq;
using System.Net.WebSockets;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets;

internal static class WebSocketExtensions
{
    public static bool StateIs(this WebSocket webSocket, params WebSocketState[] states)
    {
        var state = webSocket.GetState();
        return states.Contains(state);
    }

    public static WebSocketState GetState(this WebSocket webSocket)
    {
        try
        {
            return webSocket.State;
        }
        catch (ObjectDisposedException)
        {
            return WebSocketState.Closed;
        }
    }
}