using System.IO;

namespace FindDuplicateDirs;

public static class DirInfoExtensions {
    public static long Size(this DirectoryInfo dir) {
        if (!dir.Exists) return 0;
        return dir.EnumerateFiles()
                   .Aggregate<FileInfo, long>(0, Acc<FileInfo>)
               + dir.EnumerateDirectories()
                   .Aggregate<DirectoryInfo, long>(0, Acc<DirectoryInfo>);
    }

    public static long Size(this FileInfo file) {
        return file.Exists ? file.Length : 0;
    }

    private static long Acc<T>(long seed, FileInfo element) {
        return seed + element.Size();
    }

    private static long Acc<T>(long seed, DirectoryInfo element) {
        return seed + element.Size();
    }
}