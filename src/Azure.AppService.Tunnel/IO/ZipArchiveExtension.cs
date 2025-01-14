using System.IO;
using System.IO.Compression;

namespace JetBrains.Azure.AppService.Tunnel.IO;

internal static class ZipArchiveExtension {
    
    public static void CreateEntry(this ZipArchive archive, string pathToFileOrFolder, string entryName = "")
    {
        var nextEntryName = Path.Combine(entryName, Path.GetFileName(pathToFileOrFolder));
        
        if (Directory.Exists(pathToFileOrFolder))
        {
            archive.CreateEntryFromDirectory(pathToFileOrFolder, nextEntryName);
        }
        else
        {
            archive.CreateEntryFromFile(pathToFileOrFolder, nextEntryName, CompressionLevel.Optimal);
        }
    }

    public static void CreateEntryFromDirectory(this ZipArchive archive, string sourceDirName, string entryName = "")
    {
        foreach (var file in Directory.EnumerateFileSystemEntries(sourceDirName))
        {
            archive.CreateEntry(file, entryName);
        }
    }
}