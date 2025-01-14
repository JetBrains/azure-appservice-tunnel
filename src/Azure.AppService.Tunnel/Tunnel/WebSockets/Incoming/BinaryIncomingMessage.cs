namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Incoming;

internal class BinaryIncomingMessage(byte[] bytes) : IIncomingMessage
{
    public byte[] Bytes { get; } = bytes;
}