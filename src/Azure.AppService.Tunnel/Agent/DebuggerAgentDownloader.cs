using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Azure.AppService.Tunnel.IO;
using JetBrains.Diagnostics;

namespace JetBrains.Azure.AppService.Tunnel.Agent;

public class DebuggerAgentDownloader
{
    private const string UrlPattern = "https://download.jetbrains.com/rider/ssh-remote-debugging/windows-x64/{0}";
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static readonly ILog Logger = Log.GetLog<DebuggerAgentDownloader>();
    
    public static async Task DownloadIfNeeded(string path, string fileName)
    {
        await Semaphore.WaitAsync();

        try
        {
            if (!IsDownloaded(path))
            {
                Logger.Info("Begin downloading debugger agent");
                await Download(path, fileName);
            }
        }
        catch (Exception exception)
        {
            Logger.Error(exception, "Failed to download debugger agent");
            throw;
        }
        finally
        {
            Semaphore.Release();
        }
    }
    
    private static bool IsDownloaded(string path)
    {
        return File.Exists(path);
    }

    private static Task Download(string path, string fileName)
    {
        return FileDownloader.Download(string.Format(UrlPattern, fileName), path);
    }
}