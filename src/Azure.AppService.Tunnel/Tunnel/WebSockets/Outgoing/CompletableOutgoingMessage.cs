using System.Threading.Tasks;
using JetBrains.Core;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;

internal class CompletableOutgoingMessage(IOutgoingMessage innerRequest) : IOutgoingMessage
{
    public TaskCompletionSource<Unit> TaskCompletionSource { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool SilentFailure => innerRequest.SilentFailure;

    public async Task Execute()
    {
        try
        {
            await innerRequest.Execute();
        }
        finally
        {
            TaskCompletionSource.SetResult(Unit.Instance);
        }
    }
}