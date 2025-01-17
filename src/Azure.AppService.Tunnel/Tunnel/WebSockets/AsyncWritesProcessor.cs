using System;
using System.Threading.Tasks;
using JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Threading;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets;

internal class AsyncWritesProcessor(Lifetime lifetime)
{
    private readonly ILog _logger = Log.GetLog<AsyncWritesProcessor>();
    private readonly AsyncChannel<IOutgoingMessage> _channel = new(lifetime);

    public async Task QueueAndAwaitCompletion(IOutgoingMessage outgoingMessage)
    {
        var request = new CompletableOutgoingMessage(outgoingMessage);
        await _channel.SendAsync(request);

        await request.TaskCompletionSource.Task;
    }

    public void Queue(IOutgoingMessage outgoingMessage)
    {
        _channel.SendBlocking(outgoingMessage);
    }

    public void BeginProcessing()
    {
        Task.Run(async () =>
        {
            while (lifetime.IsAlive)
            {
                var message = await _channel.ReceiveAsync();
                await ExecuteSafe(message);
            }
        }, lifetime);
    }

    private async Task ExecuteSafe(IOutgoingMessage outgoingMessage)
    {
        try
        {
            await outgoingMessage.Execute();
        }
        catch (Exception e)
        {
            if (e is not OperationCanceledException && !outgoingMessage.SilentFailure)
                _logger.Error(e);
        }
    }
}