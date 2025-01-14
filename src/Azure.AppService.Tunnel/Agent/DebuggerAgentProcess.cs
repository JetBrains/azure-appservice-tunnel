using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace JetBrains.Azure.AppService.Tunnel.Agent;

internal class DebuggerAgentProcess
{
    private readonly Process _process;
    private readonly TaskCompletionSource<int> _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private DebuggerAgentProcess(string path, string login, string password, int port)
    {
        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = path,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = $"-login {login} -password {password} -port {port}"
            },
            EnableRaisingEvents = true
        };
    }

    public int Port { get; private set; }
    public bool HasExited => _process.HasExited;
    
    public static async Task<DebuggerAgentProcess> Start(string path, string login, string password, int port = 0)
    {
        var debuggerAgentProcess = new DebuggerAgentProcess(path, login, password, port);
        await debuggerAgentProcess.Start();

        return debuggerAgentProcess;
    }

    private async Task Start()
    {
        _process.Exited += (_, _) => _taskCompletionSource.SetResult(_process.ExitCode);
        _process.Start();
        
        Port = await ReadPortNumber(_process);
    }

    public Task<int> WaitForExit()
    {
        return _taskCompletionSource.Task;
    }
    
    private static async Task<int> ReadPortNumber(Process process)
    {
        var output = process.StandardOutput;

        var port = await ExtractKeyWord(output, "Port");
        if (port is null) throw new InvalidOperationException("Port is null");
        
        return Convert.ToInt32(port);
    }

    private static async Task<string?> ExtractKeyWord(StreamReader reader, string keyword)
    {
        var keywordWithColon = keyword + ":";

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (line.StartsWith(keywordWithColon)) return line.Substring(keywordWithColon.Length).Trim();
        }

        return null;
    }
}