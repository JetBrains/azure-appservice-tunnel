using System.IO;

namespace JetBrains.Azure.AppService.Tunnel.SiteManagement;

public class KuduSiteExtension(string name)
{
    public string Name => name; 
    
    public string GetLogsFolder()
    {
        return Path.Combine(Kudu.GetLogFilesFolder(), Name);
    }

    public string GetFolder()
    {
        return Path.Combine(Kudu.GetSiteExtensionsFolder(), Name);
    }
}