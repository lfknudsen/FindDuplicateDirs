using System.Collections;
using System.IO;

namespace FindDuplicateDirs;

public class FullNameComparer : IEqualityComparer, IEqualityComparer<string>, IEqualityComparer<DirectoryInfo> {
    private static DirNameComparer? _instance;
    public static DirNameComparer Instance => _instance ??= new DirNameComparer();
    
    public new bool Equals(object? x, object? y) {
        if (x is string xDir && y is string yDir) {
            return Path.GetFullPath(xDir) == Path.GetFullPath(yDir);
        }
        return false;
    }

    int IEqualityComparer.GetHashCode(object obj) {
        switch (obj) {
            case string stringObj:
                return Path.GetFullPath(stringObj).GetHashCode();
            case DirectoryInfo dir:
                return dir.FullName.GetHashCode();
            default:
                return obj.GetHashCode();
        }
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