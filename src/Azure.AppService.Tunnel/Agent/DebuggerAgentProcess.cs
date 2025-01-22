using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Agent;

internal class DebuggerAgentProcess
{
    private readonly ILog _logger = Log.GetLog<DebuggerAgentProcess>();
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
                RedirectStandardError = true,
                Arguments = $"-login {login} -password {password} -port {port}"
            },
            EnableRaisingEvents = true
        };
    }

    public int Port { get; private set; }

    public static async Task<DebuggerAgentProcess> Start(
        Lifetime lifetime,
        string path,
        string login,
        string password,
        int port = 0)
    {
        var debuggerAgentProcess = new DebuggerAgentProcess(path, login, password, port);
        await debuggerAgentProcess.Start(lifetime);

        return debuggerAgentProcess;
    }

    private async Task Start(Lifetime lifetime)
    {
        lifetime.OnTermination(() =>
        {
            if (!_process.HasExited) _process.Kill();
        });
        
        _process.Exited += (_, _) => _taskCompletionSource.SetResult(_process.ExitCode);
        _process.Start();

        Port = await ReadPortNumber(_process);

        Task.Run(() => WriteOutputToLogger(lifetime, _process.StandardOutput, _logger.Info), lifetime);
        Task.Run(() => WriteOutputToLogger(lifetime, _process.StandardError, _logger.Error), lifetime);
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

    private async Task WriteOutputToLogger(Lifetime lifetime, StreamReader stream, Action<string> logAction)
    {
        try
        {
            while (await stream.ReadLineAsync() is { } line && lifetime.IsAlive)
            {
                logAction(line);
            }
        }
        catch (Exception exception)
        {
            _logger.Error(exception);
        }
    }
}