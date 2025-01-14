using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Incoming;
using JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets.Outgoing;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets;

internal class WebSocketConnection
{
    private readonly ILog _logger = Log.GetLog<WebSocketConnection>();
    private readonly AsyncWritesProcessor _asyncWritesProcessor;
    private readonly LifetimeDefinition _lifetimeDefinition = new();
    private readonly Lifetime _lifetime;
    private readonly WebSocket _webSocket;
    private readonly Signal<string> _onTextMessage = new();
    private readonly Signal<byte[]> _onBinaryMessage = new();

    public ISource<string> OnTextMessage => _onTextMessage;
    public ISource<byte[]> OnBinaryMessage => _onBinaryMessage;

    public WebSocketConnection(WebSocket webSocket)
    {
        _lifetime = _lifetimeDefinition.Lifetime;
        _webSocket = webSocket;
        _asyncWritesProcessor = new AsyncWritesProcessor(_lifetime);
    }

    public void Send(byte[] message)
    {
        var outgoingMessage = new ByteOutgoingMessage(_lifetime, _webSocket, message);
        _asyncWritesProcessor.Queue(outgoingMessage);
    }

    public void Send(string message)
    {
        var outgoingMessage = new StringOutgoingMessage(_lifetime, _webSocket, message);
        _asyncWritesProcessor.Queue(outgoingMessage);
    }

    public Task Start()
    {
        return Task.Run(async () =>
        {
            _logger.Info("Begin processing messages");

            _asyncWritesProcessor.BeginProcessing();

            var receiveBuffer = new byte[16384];

            try
            {
                while (_lifetime.IsAlive)
                {
                    var message = await ReceiveMessage(receiveBuffer);
                    var shouldContinueProcessing = await ProcessMessage(message);
                    if (!shouldContinueProcessing) break;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
            {
                _logger.Error(ex);
                throw;
            }
        }, _lifetime);
    }

    public async Task Close(WebSocketCloseStatus closeStatus, string statusDescription)
    {
        await Task.WhenAny(SendCloseRequest(), Task.Delay(500));
        _lifetimeDefinition.Terminate();
        return;

        Task SendCloseRequest()
        {
            var message = new CloseOutgoingMessage(_webSocket, closeStatus, statusDescription);
            return _asyncWritesProcessor.QueueAndAwaitCompletion(message);
        }
    }
    
    private async Task<bool> ProcessMessage(IIncomingMessage message)
    {
        switch (message)
        {
            case CloseIncomingMessage:
            {
                _logger.Info("Close message received");
                await Close(WebSocketCloseStatus.NormalClosure, string.Empty);
                return false;
            }
            case BinaryIncomingMessage binary:
            {
                _onBinaryMessage.Fire(binary.Bytes);
                return true;
            }
            case TextIncomingMessage text:
            {
                _onTextMessage.Fire(text.Text);
                return true;
            }
            default: throw new InvalidOperationException($"Unknown message type: {message.GetType()}");
        }
    }
    
    private async Task<IIncomingMessage> ReceiveMessage(byte[] receiveBuffer)
    {
        var segment = new ArraySegment<byte>(receiveBuffer);
        var receiveResult = await _webSocket.ReceiveAsync(segment, _lifetime);

        if (receiveResult.MessageType == WebSocketMessageType.Close)
        {
            return CloseIncomingMessage.Instance;
        }

        if (receiveResult.EndOfMessage)
        {
            return CreateMessage(receiveResult.MessageType, segment.Array.Take(receiveResult.Count).ToArray());
        }

        return await ReadRemainingMessage(receiveResult, receiveBuffer);
    }

    private async Task<IIncomingMessage> ReadRemainingMessage(
        WebSocketReceiveResult firstResult,
        byte[] receiveBuffer)
    {
        var segment = new ArraySegment<byte>(receiveBuffer);

        using var memoryStream = new MemoryStream();
        await memoryStream.WriteAsync(receiveBuffer, 0, firstResult.Count, _lifetime);

        WebSocketReceiveResult receiveResult;
        do
        {
            receiveResult = await _webSocket.ReceiveAsync(segment, _lifetime);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return CloseIncomingMessage.Instance;
            }

            if (receiveResult.MessageType != firstResult.MessageType)
            {
                throw new InvalidOperationException("Incorrect message type");
            }

            await memoryStream.WriteAsync(receiveBuffer, 0, receiveResult.Count, _lifetime);
        } while (!receiveResult.EndOfMessage);

        return CreateMessage(firstResult.MessageType, memoryStream.ToArray());
    }

    private static IIncomingMessage CreateMessage(WebSocketMessageType messageType, byte[] bytes)
    {
        return messageType switch
        {
            WebSocketMessageType.Binary => new BinaryIncomingMessage(bytes),
            WebSocketMessageType.Text => new TextIncomingMessage(Encoding.UTF8.GetString(bytes)),
            WebSocketMessageType.Close => throw new InvalidOperationException(
                "Close message should be processed earlier"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}