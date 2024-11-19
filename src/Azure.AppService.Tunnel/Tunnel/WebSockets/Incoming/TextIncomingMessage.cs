namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Incoming;

internal class TextIncomingMessage(string text) : IIncomingMessage
{
    public string Text { get; } = text;
}