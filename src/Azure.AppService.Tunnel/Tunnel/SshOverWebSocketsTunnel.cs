using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web.WebSockets;
using JetBrains.Azure.AppService.Tunnel.Agent;
using JetBrains.Azure.AppService.Tunnel.Tunnel.Sockets;
using JetBrains.Azure.AppService.Tunnel.Tunnel.WebSockets;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Tunnel;

internal class SshOverWebSocketsTunnel
{
    private readonly AspNetWebSocketContext _context;
    private readonly LifetimeDefinition _lifetimeDefinition = new();
    private readonly ClientSocketConnection _socketConnection;
    private readonly WebSocketConnection _webSocketConnection; 
    
    private static readonly ILog Logger = Log.GetLog<SshOverWebSocketsTunnel>();
    private static readonly DebuggerAgent DebuggerAgent = new();
    
    private Lifetime Lifetime => _lifetimeDefinition.Lifetime;
    
    public SshOverWebSocketsTunnel(AspNetWebSocketContext context)
    {
        _context = context;
        _socketConnection = new ClientSocketConnection(Lifetime);
        _webSocketConnection = new WebSocketConnection(context.WebSocket);
        
        _socketConnection.DataReceived.Advise(Lifetime, data => _webSocketConnection.Send(data));
        _webSocketConnection.OnBinaryMessage.Advise(Lifetime, data => _socketConnection.Send(data));
    }
    
    public async Task Start()
    {
        try
        {
            var debuggerAgentProcess = await StartDebuggerAgent();

            var debuggerAgentProcessing = debuggerAgentProcess.WaitForExit();
            var socketProcessing = _socketConnection.ConnectAndStartProcessing(debuggerAgentProcess.Port);
            var webSocketProcessing = _webSocketConnection.Start();
            
            var completedTask = await Task.WhenAny(debuggerAgentProcessing, socketProcessing, webSocketProcessing);

            if (completedTask == debuggerAgentProcessing)
            {
                Terminate(WebSocketCloseStatus.NormalClosure, "Debugger Agent process exited");
            }
            else if (completedTask == socketProcessing)
            {
                Terminate(WebSocketCloseStatus.NormalClosure, "Socket connection closed");
            }
            else
            {
                Terminate("Web Socket connection closed");
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception);
            Terminate(WebSocketCloseStatus.InternalServerError, "Unknown error occured");
        }
    }

    private Task<DebuggerAgentProcess> StartDebuggerAgent()
    {
        var login = _context.Headers["Agent-username"];
        var password = _context.Headers["Agent-password"];
        return DebuggerAgent.Start(login, password);
    }
    
    private void Terminate(string reason)
    {
        _lifetimeDefinition.Terminate();
        Logger.Info($"Terminating tunnel, reason: {reason}");
    }

    private void Terminate(WebSocketCloseStatus closeStatus, string reason)
    {
        Terminate(reason);
        _webSocketConnection.Close(closeStatus, reason).Wait();
    }
}