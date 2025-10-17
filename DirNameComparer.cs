using System.Collections;
using System.IO;

namespace FindDuplicateDirs;

public class DirNameComparer : IEqualityComparer, IEqualityComparer<string>,
                               IEqualityComparer<DirectoryInfo> {
    private static DirNameComparer? _instance;
    public static DirNameComparer Instance => _instance ??= new DirNameComparer();

    public new bool Equals(object? x, object? y) {
        if (x is string xDir && y is string yDir) {
            return Path.GetFileName(xDir) == Path.GetFileName(yDir);
        }

        return false;
    }

    int IEqualityComparer.GetHashCode(object obj) {
        return obj switch {
            string stringObj  => Path.GetFileName(stringObj).GetHashCode(),
            DirectoryInfo dir => dir.Name.GetHashCode(),
            _                 => obj.GetHashCode()
        };
    }

    public bool Equals(string? x, string? y) {
        if (x is null || y is null) return false;
        return Path.GetFileName(x) == Path.GetFileName(y);
    }

    public int GetHashCode(string obj) {
        return Path.GetFileName(obj).GetHashCode();
    }

    public bool Equals(DirectoryInfo? x, DirectoryInfo? y) {
        if (x is null || y is null) return false;
        return x.Name == y.Name;
    }

    public int GetHashCode(DirectoryInfo obj) {
        return obj.Name.GetHashCode();
    }
}