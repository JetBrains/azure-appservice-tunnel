using System;
using System.IO;

namespace JetBrains.Azure.AppService.Tunnel.SiteManagement;

internal static class Kudu
{
    private const string LogFiles = "LogFiles";
    private const string SiteExtensions = "SiteExtensions";
    public static KuduSiteExtension ThisExtension { get; } = new("JetBrains.Azure.AppService.Tunnel");
    
    public static string GetLogFilesFolder()
    {
        return Path.Combine(GetHomePath(), LogFiles);
    }

    public static string GetSiteExtensionsFolder()
    {
        return Path.Combine(GetHomePath(), SiteExtensions);
    }

    private static string GetHomePath() => Environment.GetEnvironmentVariable("HOME") ??
                                           throw new InvalidOperationException("Home path is not set");
}