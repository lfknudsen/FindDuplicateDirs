using System.Collections.ObjectModel;
using System.IO;
using Tommy;

namespace FindDuplicateDirs;

public class DirectoryCollection :
    ObservableCollection<DirectoryInfo>,
    IObservable<DirectoryCollection>,
    IDisposable {
    public bool AtLeastTwoElements => Count > 2;

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

    public void Remove(string path) {
        foreach (DirectoryInfo dir in this) {
            if (dir.FullName == path && base.Remove(dir)) {
                NotifySubscribers();
                if (MainWindow.VERBOSE) Console.WriteLine("- " + dir.FullName);
                return;
            }
        }
    }

    public new void Remove(DirectoryInfo target) {
        Remove(target.FullName);
    }

    public bool Contains(string path) {
        return this.Any(dir => dir.FullName == path);
    }

    public new bool Contains(DirectoryInfo item) {
        return Contains(item.FullName);
    }

    public void RemoveDuplicates() {
        HashSet<DirectoryInfo> set = this.ToHashSet(FullNameComparer.Instance);
        if (MainWindow.VERBOSE) Console.WriteLine("Cleared directory list.");
        Clear();
        Add(set);
        NotifySubscribers();
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

    private class Unsubscriber(DirectoryCollection observable, IObserver<DirectoryCollection> observer)
        : IDisposable {
        DirectoryCollection _observable = observable;
        IObserver<DirectoryCollection> _observer = observer;

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