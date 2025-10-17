using System.Collections.ObjectModel;
using System.IO;
using Tommy;

namespace FindDuplicateDirs;

/** Set of DirectoryInfo elements. Elements are compared via
 * <see cref="DirectoryInfo.FullName"/>
 * rather than reference.<br/>
 * Has a couple of convenience methods for outputting in TOML format.<br/>
 * When elements are added/removed, the instances in <see cref="_listeners"/> are
 * notified. */
public class DirectoryCollection : ObservableCollection<DirectoryInfo>,
                                   IObservable<DirectoryCollection>,
                                   IDisposable {
    private readonly List<IObserver<DirectoryCollection>> _listeners = [];

    public new void Add(DirectoryInfo item) {
        if (!Contains(item)) {
            base.Add(item);
            NotifySubscribers();
            if (MainWindow.VERBOSE) Console.WriteLine("+ " + item.FullName);
        }
    }

    public void Add(IEnumerable<DirectoryInfo> items) {
        foreach (DirectoryInfo item in items) {
            Add(item);
        }
    }

    public void Add(TomlArray array) {
        foreach (TomlNode entry in array) {
            if (!entry.IsString) continue;

            string dirPath = entry.AsString.Value;
            if (Directory.Exists(dirPath)) {
                Add(new DirectoryInfo(dirPath));
            }
        }
    }

    public bool Remove(string path) {
        foreach (DirectoryInfo dir in this) {
            if (dir.FullName == path && base.Remove(dir)) {
                NotifySubscribers();
                if (MainWindow.VERBOSE) Console.WriteLine("- " + dir.FullName);
                return true;
            }
        }

        return false;
    }

    public bool RemoveAll(string path) {
        var changed = false;
        foreach (DirectoryInfo dir in this) {
            if (dir.FullName == path) {
                changed |= base.Remove(dir);
                if (MainWindow.VERBOSE) Console.WriteLine("- " + dir.FullName);
            }
        }

        if (changed) {
            NotifySubscribers();
        }

        return changed;
    }

    public new bool Remove(DirectoryInfo target) {
        return Remove(target.FullName);
    }

    public bool RemoveAll(DirectoryInfo target) {
        return RemoveAll(target.FullName);
    }

    /** Attempts to remove the specific DirectoryInfo instance.
     * Used when right-clicking a <see cref="System.Windows.Controls.ListViewItem">
     * list view element</see> to try to delete that particular element instead of
     * simply the first one that had an identical path. */
    public bool TryRemoveDirect(DirectoryInfo target) {
        if (base.Remove(target)) {
            NotifySubscribers();
            return true;
        }

        return Remove(target.FullName);
    }

    public bool RemoveDuplicates() {
        HashSet<DirectoryInfo> set = this.ToHashSet(FullNameComparer.Instance);
        bool changed = set.Count < Count;
        if (MainWindow.VERBOSE) Console.WriteLine("Cleared directory list.");
        Clear();
        Add(set);
        NotifySubscribers();
        return changed;
    }

    public bool Contains(string path) {
        return this.Any(dir => dir.FullName == path);
    }

    public new bool Contains(DirectoryInfo item) {
        return Contains(item.FullName);
    }

    public bool ContainsAll(string[] paths) {
        return paths.All(Contains);
    }

    public bool ContainsAll(IEnumerable<string> paths) {
        return paths.All(Contains);
    }

    public bool ContainsAll(DirectoryInfo[] items) {
        return items.All(e => Contains(e.FullName));
    }

    public bool ContainsAll(IEnumerable<DirectoryInfo> items) {
        return items.All(e => Contains(e.FullName));
    }

    //==========================================================================
    // TOML writing
    //==========================================================================

    private IEnumerable<TomlString> ToTomlEnumerable(int? limit = null) {
        int safeLimit = Count;
        if (limit >= 0 && limit < Count) {
            safeLimit = limit.Value;
        }

        for (int i = 0; i < safeLimit; i++) {
            yield return new TomlString {
                Value = this[i].FullName
            };
        }
    }

    public TomlArray ToTomlArray(int? limit = null) {
        var arr = new TomlArray();
        arr.AddRange(ToTomlEnumerable(limit));
        return arr;
    }

    //==========================================================================
    // Event Handling
    //==========================================================================

    public IDisposable Subscribe(IObserver<DirectoryCollection> observer) {
        _listeners.Add(observer);
        return new Unsubscriber(this, observer);
    }

    private void Unsubscribe(IObserver<DirectoryCollection> observer) {
        _listeners.Remove(observer);
    }

    private void NotifySubscribers() {
        _listeners.ForEach(observer => observer.OnNext(this));
    }

    private void CloseSubscriptions() {
        _listeners.ForEach(observer => observer.OnCompleted());
    }

    private class Unsubscriber(DirectoryCollection observable,
                               IObserver<DirectoryCollection> observer) : IDisposable {
        private readonly DirectoryCollection _observable = observable;
        private readonly IObserver<DirectoryCollection> _observer = observer;

        public void Dispose() {
            _observable.Unsubscribe(_observer);
            GC.SuppressFinalize(this);
        }
    }

    public void Dispose() {
        CloseSubscriptions();
        GC.SuppressFinalize(this);
    }
}