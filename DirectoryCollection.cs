using System.Collections.ObjectModel;
using System.IO;
using Tommy;

namespace FindDuplicateDirs;

public class DirectoryCollection : ObservableCollection<DirectoryInfo> {
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
}