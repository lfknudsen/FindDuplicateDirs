using System.Collections.ObjectModel;
using System.IO;

namespace FindDuplicateDirs;

public class DirectoryPairCollection : ObservableCollection<Tuple<DirectoryInfo, DirectoryInfo>> {
    public void Add(IEnumerable<Tuple<DirectoryInfo, DirectoryInfo>> items) {
        foreach (Tuple<DirectoryInfo, DirectoryInfo> item in items) {
            base.Add(item);
        }
    }

    public void Add(DirectoryInfo a, DirectoryInfo b) {
        var tuple = new Tuple<DirectoryInfo, DirectoryInfo>(a, b);
        if (!Contains(a.FullName, b.FullName)) {
            base.Add(tuple);
        }
    }

    public void Add(string pathA, string pathB) {
        if (!Contains(pathA, pathB)) {
            base.Add(
                new Tuple<DirectoryInfo, DirectoryInfo>(
                    new DirectoryInfo(pathA),
                    new DirectoryInfo(pathB)));
        }
    }

    public new void Add(Tuple<DirectoryInfo, DirectoryInfo> item) {
        if (!Contains(item.Item1.FullName, item.Item2.FullName)) {
            base.Add(item);
        }
    }

    public bool Contains(string pathA, string pathB) {
        return this.Any(item => item.Item1.FullName == pathA && item.Item2.FullName == pathB);
    }

    public void Remove(string targetString) {
        foreach (Tuple<DirectoryInfo, DirectoryInfo> dirs in this) {
            if (dirs.Item1.FullName == targetString || dirs.Item2.FullName == targetString) {
                base.Remove(dirs);
                return;
            }
        }
    }

    public void Remove(string targetA, string targetB) {
        foreach (Tuple<DirectoryInfo, DirectoryInfo> dirs in this) {
            if (dirs.Item1.FullName == targetA || dirs.Item2.FullName == targetB) {
                base.Remove(dirs);
                return;
            }
        }
    }

    public void Remove(Tuple<string, string> target) {
        Remove(target.Item1, target.Item2);
    }

    public void Remove(DirectoryInfo target) {
        Remove(target.FullName);
    }

    public void RemoveAll(string targetString) {
        foreach (Tuple<DirectoryInfo, DirectoryInfo> dirs in this) {
            if (dirs.Item1.FullName == targetString || dirs.Item2.FullName == targetString) {
                base.Remove(dirs);
            }
        }
    }

    public void RemoveAll(DirectoryInfo target) {
        RemoveAll(target.FullName);
    }
}