using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Tommy;

namespace FindDuplicateDirs;

public class DirectoryCollection : ObservableCollection<DirectoryInfo>, ICommand {
    public new void Add(DirectoryInfo item) {
        if (!Contains(item)) {
            base.Add(item);
        }
    }

    public new bool Contains(DirectoryInfo item) {
        return this.Any(dir => dir.FullName == item.FullName);
    }

    public void Remove(string path) {
        foreach (DirectoryInfo dir in this) {
            if (dir.FullName == path) {
                base.Remove(dir);
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

    public void Add(IEnumerable<DirectoryInfo> items) {
        foreach (DirectoryInfo item in items) {
            Add(item);
        }
    }

    public void RemoveDuplicates() {
        HashSet<DirectoryInfo> set = this.ToHashSet(FullNameComparer.Instance);
        Clear();
        Add(set);
    }
    
    public void AddTomlArray(TomlArray array) {
        foreach (TomlNode entry in array) {
            if (entry.IsString) {
                string dirPath = entry.AsString.Value;
                if (Directory.Exists(dirPath)) {
                    Add(new DirectoryInfo(dirPath));
                }
            }
        }
    }

    public IEnumerable<TomlString> AsTomlEnumerable(int? limit = null) {
        int safeLimit = Count;
        if (limit != null && limit >= 0 && limit < Count) {
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
        arr.AddRange(AsTomlEnumerable(limit));
        return arr;
    }

    public bool CanExecute(object? parameter) {
        return parameter is DirectoryInfo;
    }

    public void Execute(object? parameter) {
        Console.WriteLine("Entered execution of command?!");
        Remove(parameter as DirectoryInfo ?? throw new InvalidOperationException());
    }

    public event EventHandler? CanExecuteChanged;
}