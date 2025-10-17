using System.Collections;
using System.IO;

namespace FindDuplicateDirs;

public class FullNameComparer :
        IEqualityComparer,
        IEqualityComparer<string>,
        IEqualityComparer<DirectoryInfo> {
    private static FullNameComparer? _instance;
    public static FullNameComparer Instance => _instance ??= new FullNameComparer();

    public new bool Equals(object? x, object? y) {
        if (x is string xDir && y is string yDir) {
            return Path.GetFullPath(xDir) == Path.GetFullPath(yDir);
        }
        return false;
    }

    int IEqualityComparer.GetHashCode(object obj) {
        return obj switch {
            string stringObj  => Path.GetFullPath(stringObj).GetHashCode(),
            DirectoryInfo dir => dir.FullName.GetHashCode(),
            _                 => obj.GetHashCode()
        };
    }

    public bool Equals(string? x, string? y) {
        if (x is null || y is null) return false;
        return Path.GetFullPath(x) == Path.GetFullPath(y);
    }

    public int GetHashCode(string obj) {
        return Path.GetFullPath(obj).GetHashCode();
    }

    public bool Equals(DirectoryInfo? x, DirectoryInfo? y) {
        if (x is null || y is null) return false;
        return x.FullName == y.FullName;
    }

    public int GetHashCode(DirectoryInfo obj) {
        return obj.FullName.GetHashCode();
    }
}