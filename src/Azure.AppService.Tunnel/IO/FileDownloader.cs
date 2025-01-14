using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace JetBrains.Azure.AppService.Tunnel.IO;

internal static class FileDownloader
{
    public static async Task Download(string url, string path)
    {
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await BeginDownloading(url, async bytes =>
        {
            await fileStream.WriteAsync(bytes.Array, 0, bytes.Count);
        });
    }

    private static async Task BeginDownloading(string url, Func<ArraySegment<byte>, Task> onPartDownloaded)
    {
        using var client = new HttpClient();
        var stream = await client.GetStreamAsync(url);

        var buffer = new byte[16384];
        int length;

        do
        {
            length = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (length > 0) await onPartDownloaded(new ArraySegment<byte>(buffer, 0, length));
        } while (length > 0);
    }
}