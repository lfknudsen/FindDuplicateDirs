using System.IO;

namespace FindDuplicateDirs;

public class DirView : IObservable<DirView> {
    public readonly string Path;

    /** Size (in bytes) of this directory.
     * Since this is computed asynchronously, the default value will determine how wide
     * the column will be by default. */
    public string DirSize { get; private set; } = "                              ";

    private readonly List<IObserver<DirView>> _observers = [];

    public override string ToString() {
        return Path;
    }

    public DirView(DirectoryInfo dir) {
        Path = dir.FullName;
        _ = ComputeSize();
    }

    public DirView(string path) {
        Path = path;
        _ = ComputeSize();
    }

    private async Task ComputeSize() {
        if (!File.Exists(Path)) {
            return;
        }

        await Task.Run(() => {
            var dir = new DirectoryInfo(Path);
            long size = dir.EnumerateFiles()
                       .Aggregate<FileInfo, long>(0, Acc<FileInfo>)
                 + dir.EnumerateDirectories()
                       .Aggregate<DirectoryInfo, long>(0, Acc<DirectoryInfo>);
            DirSize = size.ToString();
        });
        NotifySubscribers();
    }

    private static long Acc<T>(long seed, FileInfo element) {
        return seed + element.Size();
    }

    private static long Acc<T>(long seed, DirectoryInfo element) {
        return seed + element.Size();
    }

    public IDisposable Subscribe(IObserver<DirView> observer) {
        _observers.Add(observer);
        return new Unsubscriber(this, observer);
    }

    /** Removes the observer from the internal list of subscribers. This method
     * allows observers to be removed without keeping track of the IDisposable
     * they received from <see cref="Subscribe"/> (which saves a lot of space in
     * DirectoryPairCollection). */
    public void Unsubscribe(IObserver<DirView> observer) {
        _observers.Remove(observer);
    }

    public bool IsSubscribed(IObserver<DirView> observer) {
        return _observers.Contains(observer);
    }

    private void NotifySubscribers() {
        _observers.ForEach(observer => observer.OnNext(this));
    }

    private class Unsubscriber(DirView observable,
                               IObserver<DirView> observer) : IDisposable {
        private readonly DirView _observable = observable;
        private readonly IObserver<DirView> _observer = observer;

        public void Dispose() {
            _observable.Unsubscribe(_observer);
            GC.SuppressFinalize(this);
        }
    }
}