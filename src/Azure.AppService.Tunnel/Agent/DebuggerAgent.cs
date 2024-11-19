using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Azure.AppService.Tunnel.IO;
using JetBrains.Azure.AppService.Tunnel.SiteManagement;
using JetBrains.Diagnostics;

namespace JetBrains.Azure.AppService.Tunnel.Agent;

internal class DebuggerAgent
{
    private const string Version = "1_0_1";
    private const string Name = $"jetbrains_debugger_agent_{Version}.exe";
    private static readonly string Path = $@"{Kudu.ThisExtension.GetFolder()}\bin\{Name}";
    private const string Url = $"https://download.jetbrains.com/rider/ssh-remote-debugging/windows-x64/{Name}";

    private readonly ILog _logger = Log.GetLog<DebuggerAgent>();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private DebuggerAgentProcess? _process;

    public async Task<DebuggerAgentProcess> Start(string login, string password)
    {
        await _semaphore.WaitAsync();
        try
        {
            await DownloadIfNeeded();
            
            if (_process is { HasExited: false }) return _process;
            return _process = await StartProcess(login, password);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private Task<DebuggerAgentProcess> StartProcess(string login, string password)
    {
        _logger.Info("Starting debugger agent");
        return DebuggerAgentProcess.Start(Path, login, password);
    }
    
    private async Task DownloadIfNeeded()
    {
        if (!IsDownloaded())
        {
            _logger.Info("Begin downloading debugger agent");
            await Download();
        }
    }
    
    private static bool IsDownloaded()
    {
        return File.Exists(Path);
    }

    private static Task Download()
    {
        return FileDownloader.Download(Url, Path);
    }
}