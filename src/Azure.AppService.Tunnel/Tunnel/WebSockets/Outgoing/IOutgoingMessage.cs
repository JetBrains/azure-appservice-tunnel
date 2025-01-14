using System.Threading.Tasks;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;

internal interface IOutgoingMessage
{
    bool SilentFailure { get; }
    Task Execute();
}