namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Incoming;

internal class CloseIncomingMessage : IIncomingMessage
{
    public static CloseIncomingMessage Instance { get; } = new();
}