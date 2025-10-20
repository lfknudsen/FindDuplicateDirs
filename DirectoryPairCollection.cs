using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;

namespace FindDuplicateDirs;

public class DirectoryPairCollection : ObservableCollection<Tuple<DirView, DirView>>,
                                       IObserver<DirView> {
    public new bool Add(Tuple<DirView, DirView> item) {
        if (!Contains(item.Item1.Path, item.Item2.Path)) {
            Subscribe(item);
            base.Add(item);
            return true;
        }

        return false;
    }

    public bool Add(IEnumerable<Tuple<DirView, DirView>> items) {
        return items.Aggregate(false, (collectionChanged, item) => collectionChanged | Add(item));
    }

    public bool Add(DirView a, DirView b) {
        if (!Contains(a.Path, b.Path)) {
            var tuple = new Tuple<DirView, DirView>(a, b);
            Subscribe(tuple);
            base.Add(tuple);
            return true;
        }

        return false;
    }

    public bool Add(string pathA, string pathB) {
        if (!Contains(pathA, pathB)) {
            var a = new DirView(pathA);
            var b = new DirView(pathB);
            var tuple = new Tuple<DirView, DirView>(a, b);
            Subscribe(tuple);
            base.Add(tuple);
            return true;
        }

        return false;
    }

    public bool Contains(string pathA, string pathB) {
        return this.Any(item => item.Item1.Path == pathA && item.Item2.Path == pathB);
    }

    public bool Remove(string fullName) {
        foreach (Tuple<DirView, DirView> dirs in this) {
            if ((dirs.Item1.Path == fullName || dirs.Item2.Path == fullName)
             && base.Remove(dirs)) {
                Unsubscribe(dirs);
                return true;
            }
        }

        return false;
    }

    public bool Remove(string fullNameA, string fullNameB) {
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.Path == fullNameA
             && dirs.Item2.Path == fullNameB
             && base.Remove(dirs)) {
                Unsubscribe(dirs);
                return true;
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
            if ((dirs.Item1.Path == fullName || dirs.Item2.Path == fullName)
             && base.Remove(dirs)) {
                Unsubscribe(dirs);
                changed = true;
            }
        }

        return changed;
    }

    public bool RemoveAll(string fullNameA, string fullNameB) {
        bool changed = false;
        foreach (Tuple<DirView, DirView> dirs in this) {
            if (dirs.Item1.Path == fullNameA
             && dirs.Item2.Path == fullNameB
             && base.Remove(dirs)) {
                Unsubscribe(dirs);
                changed = true;
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

    //==========================================================================
    // Event-related methods
    //==========================================================================

    private void Subscribe(Tuple<DirView, DirView> directoryPair) {
        directoryPair.Item1.Subscribe(this);
        directoryPair.Item2.Subscribe(this);
    }

    private void Unsubscribe(Tuple<DirView, DirView> tuple) {
        tuple.Item1.Unsubscribe(this);
        tuple.Item2.Unsubscribe(this);
    }

    private void Update() {
        OnCollectionChanged(
            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(DirView value) {
        Update();
    }
}