using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Azure.AppService.Tunnel.SiteManagement;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;

namespace JetBrains.Azure.AppService.Tunnel.Logging;

internal static class SiteExtensionLogging
{
    private static readonly Lazy<bool> LogFactoryLazy = new(CreateAndSetFactory, isThreadSafe: true);

    public static bool Initialize()
    {
        return LogFactoryLazy.Value;
    }

    private static bool CreateAndSetFactory()
    {
        var path = Path.Combine(Kudu.ThisExtension.GetLogsFolder(), GetLoggingFile());
        Log.DefaultFactory = Log.CreateFileLogFactory(Lifetime.Eternal, path);
        return true;
    }
    
    private static string GetLoggingFile()
    {
        var time = Process.GetCurrentProcess().StartTime.ToString("yyyy-MM-dd__HH-mm-ss");
        return $"{Kudu.ThisExtension.Name}_{time}.log";
    }
}