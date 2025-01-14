using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;
using JetBrains.Azure.AppService.Tunnel.Logging;
using JetBrains.Azure.AppService.Tunnel.Tunnel;

namespace JetBrains.Azure.AppService.Tunnel;

public class TunnelHandler : IHttpHandler
{
    public bool IsReusable => true;

    public void ProcessRequest(HttpContext context)
    {
        if (context.IsWebSocketRequest)
            context.AcceptWebSocketRequest(HandleWebSocketConnection);
    }

    private static async Task HandleWebSocketConnection(AspNetWebSocketContext context)
    {
        SiteExtensionLogging.Initialize();

        var tunnel = new SshOverWebSocketsTunnel(context);
        await tunnel.Start();
    }
}