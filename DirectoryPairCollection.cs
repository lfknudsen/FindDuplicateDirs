using System.Collections.ObjectModel;
using System.IO;

namespace FindDuplicateDirs;

public class DirectoryPairCollection : ObservableCollection<Tuple<DirView, DirView>> {
    public new bool Add(Tuple<DirView, DirView> item) {
        if (!Contains(item.Item1.FullName, item.Item2.FullName)) {
            base.Add(item);
            return true;
        }

        return false;
    }

    public bool Add(IEnumerable<Tuple<DirView, DirView>> items) {
        return items.Aggregate(false, (collectionChanged, item) => collectionChanged | Add(item));
    }

    public bool Add(DirView a, DirView b) {
        if (!Contains(a.FullName, b.FullName)) {
            base.Add(new Tuple<DirView, DirView>(a, b));
            return true;
        }

        return false;
    }

    public bool Add(string pathA, string pathB) {
        if (!Contains(pathA, pathB)) {
            base.Add(new Tuple<DirView, DirView>(new DirView(pathA), new DirView(pathB)));
            return true;
        }

        return false;
    }

    public bool Contains(string pathA, string pathB) {
        return this.Any(item => item.Item1.FullName == pathA && item.Item2.FullName == pathB);
    }

    public bool Remove(string fullName) {
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.FullName == fullName || dirs.Item2.FullName == fullName) {
                return base.Remove(dirs);
            }
        }

        return false;
    }

    public bool Remove(string fullNameA, string fullNameB) {
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.FullName == fullNameA && dirs.Item2.FullName == fullNameB) {
                return base.Remove(dirs);
            }
        }

        return false;
    }

    public bool Remove(Tuple<string, string> target) {
        return Remove(target.Item1, target.Item2);
    }

    public bool RemoveAll(string fullName) {
        bool changed = false;
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.FullName == fullName || dirs.Item2.FullName == fullName) {
                changed |= base.Remove(dirs);
            }
        }

        return changed;
    }

    public bool RemoveAll(string fullNameA, string fullNameB) {
        bool changed = false;
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.FullName == fullNameA && dirs.Item2.FullName == fullNameB) {
                changed |= base.Remove(dirs);
            }
        }

        return changed;
    }

    //==========================================================================
    // Methods which interface with DirectoryInfo
    //==========================================================================

    public bool Add(IEnumerable<Tuple<DirectoryInfo, DirectoryInfo>> items) {
        bool changed = false;
        foreach (Tuple<DirectoryInfo, DirectoryInfo> item in items) {
            changed |= Add(new Tuple<DirView, DirView>(
                               new DirView(item.Item1), new DirView(item.Item2)));
        }

        return changed;
    }

    public bool Add(DirectoryInfo a, DirectoryInfo b) {
        return Add(new DirView(a.FullName), new DirView(b.FullName));
    }

    public bool Add(Tuple<DirectoryInfo, DirectoryInfo> item) {
        return Add(item.Item1.FullName, item.Item2.FullName);
    }

    public bool Remove(DirectoryInfo target) {
        return Remove(target.FullName);
    }

    public bool RemoveAll(DirectoryInfo target) {
        return RemoveAll(target.FullName);
    }
}