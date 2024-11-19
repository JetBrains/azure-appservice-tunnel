using System.IO;
using System.IO.Compression;
using System.Net;
using System.Web;
using JetBrains.Azure.AppService.Tunnel.IO;
using JetBrains.Azure.AppService.Tunnel.SiteManagement;

namespace JetBrains.Azure.AppService.Tunnel;

public class LogsHandler : IHttpHandler
{
    public bool IsReusable => true;
    
    public void ProcessRequest(HttpContext context)
    {
        if (context.IsWebSocketRequest) return;
        
        var logsFolder = Kudu.ThisExtension.GetLogsFolder();
        
        if (Directory.Exists(logsFolder))
        {
            WriteLogs(context, logsFolder);
        }
        else
        {
            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
        }
    }

    private static void WriteLogs(HttpContext context, string logsFolder)
    {
        context.Response.ContentType = "application/zip";
        context.Response.AddHeader("Content-Disposition", "attachment; filename=logs.zip");

        // Response.Output stream is not seekable, but ZipArchive tries to seek when creating entries
        using var memoryStream = new MemoryStream();
        using var zipStream = new ZipArchive(memoryStream, ZipArchiveMode.Create);
        zipStream.CreateEntryFromDirectory(logsFolder);

        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.CopyTo(context.Response.OutputStream);
    }
}