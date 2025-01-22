using System.Threading.Tasks;
using JetBrains.Azure.AppService.Tunnel.SiteManagement;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Agent;

internal class DebuggerAgent
{
    private const string Version = "20250121.44.0";
    private const string Name = $"jetbrains_debugger_agent_{Version}.exe";
    private static readonly string PathToExe = $@"{Kudu.GetJetBrainsFolder()}\DebuggerAgent\{Name}";
    
    private readonly ILog _logger = Log.GetLog<DebuggerAgent>();

    public async Task<DebuggerAgentProcess> Start(Lifetime lifetime, string login, string password)
    {
        await DebuggerAgentDownloader.DownloadIfNeeded(PathToExe, Name);
        return await StartProcess(lifetime, login, password);
    }

    private Task<DebuggerAgentProcess> StartProcess(Lifetime lifetime, string login, string password)
    {
        _logger.Info("Starting debugger agent");
        return DebuggerAgentProcess.Start(lifetime, PathToExe, login, password);
    }
}