using System.IO;

namespace FindDuplicateDirs;

public class DirView {
    private readonly DirectoryInfo _dir;

    public string FullName => _dir.FullName;

    public long Size { get; private set; } = -1;

    public string DirSize { get; private set; } = "";

    private readonly Task _computeSizeTask;


    public DirView(DirectoryInfo dir) {
        _dir = dir;
        _computeSizeTask = Task.Run(ComputeSize);
    }

    public DirView(string path) : this(new DirectoryInfo(path)) { }

    private void ComputeSize() {
        if (!_dir.Exists)
            return;
        Size = _dir.EnumerateFiles()
                   .Aggregate<FileInfo, long>(0, Acc<FileInfo>)
             + _dir.EnumerateDirectories()
                   .Aggregate<DirectoryInfo, long>(0, Acc<DirectoryInfo>);
        DirSize = Size.ToString();
    }

    private static long Acc<T>(long seed, FileInfo element) {
        return seed + element.Size();
    }

    private static long Acc<T>(long seed, DirectoryInfo element) {
        return seed + element.Size();
    }
}