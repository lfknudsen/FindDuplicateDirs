using System.Globalization;
using System.IO;

namespace FindDuplicateDirs;

public class DirView : IObservable<DirView> {
    public static bool ShowSizeInBytes { get; set; } = false;

    public readonly string Path;
    public string LastModified { get; }
    public string Created  { get; }

    /** Size (in bytes) of this directory.
     * Since this is computed asynchronously, the default value will determine how wide
     * the column will be by default. */
    public string DirSize { get; private set; } = "                              ";

    private readonly List<IObserver<DirView>> _observers = [];

    public override string ToString() {
        return Path;
    }

    public DirView(DirectoryInfo dir) {
        if (!dir.Exists) {
            throw new DirectoryNotFoundException(dir.FullName);
        }
        Path = dir.FullName;
        LastModified = dir.LastWriteTime.ToString(CultureInfo.CurrentCulture);
        Created = dir.CreationTime.ToString(CultureInfo.CurrentCulture);
        _ = ComputeSize();
    }

    public DirView(string path) {
        if (!Directory.Exists(path)) {
            throw new DirectoryNotFoundException(path);
        }

        Path = path;
        LastModified = Directory.GetLastWriteTime(path).ToString(CultureInfo.CurrentCulture);
        Created = Directory.GetCreationTime(path).ToString(CultureInfo.CurrentCulture);
        _ = ComputeSize();
    }

    public async Task ComputeSize() {
        if (!Directory.Exists(Path)) {
            throw new DirectoryNotFoundException(Path);
        }

        await Task.Run(() => {
            var dir = new DirectoryInfo(Path);
            long size = dir.EnumerateFiles()
                           .Aggregate<FileInfo, long>(0, Acc)
                      + dir.EnumerateDirectories()
                           .Aggregate<DirectoryInfo, long>(0, Acc);
            DirSize = FormatBytes(size);
        });
        NotifySubscribers();
    }

    private static long Acc(long seed, FileInfo element) {
        return seed + element.Size();
    }

    private static long Acc(long seed, DirectoryInfo element) {
        return seed + element.Size();
    }

    private static string FormatBytes(long size) {
        if (ShowSizeInBytes) {
            return $"{size} B";
        }

        double truncatedSize = size;
        UnitSymbols truncations = UnitSymbols.B;
        while (truncatedSize >= 1000 && truncations <= UnitSymbols.ZB) {
            truncatedSize /= 1000;
            truncations++;
        }

        return $"{truncatedSize:0.##} " + UnitSymbolToString(truncations);
    }

    private enum UnitSymbols {
        B,
        KB,
        MB,
        GB,
        TB,
        PB,
        EB,
        ZB,
    }

    private static string UnitSymbolToString(UnitSymbols shorthand) => shorthand switch {
        UnitSymbols.B  => "B",
        UnitSymbols.KB => "KB",
        UnitSymbols.MB => "MB",
        UnitSymbols.GB => "GB",
        UnitSymbols.TB => "TB",
        UnitSymbols.PB => "PB",
        UnitSymbols.EB => "EB",
        UnitSymbols.ZB => "ZB",
        _ => throw
            new ArgumentOutOfRangeException(nameof(shorthand), shorthand,
                                            "Cannot convert the shorthand into a string format.")
    };

    public IDisposable Subscribe(IObserver<DirView> observer) {
        if (!_observers.Contains(observer)) {
            _observers.Add(observer);
        }

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