using System.Collections.ObjectModel;
using System.DirectoryServices;
using System.IO;
using Tommy;

namespace FindDuplicateDirs;

public class DirectoryCollection : ObservableCollection<DirectoryEntry> {
    public void AddTomlArray(TomlArray array) {
        foreach (TomlNode entry in array) {
            if (entry.IsString) {
                string dirPath = entry.AsString.Value;
                if (Directory.Exists(dirPath)) {
                    Add(new DirectoryEntry(dirPath));
                }
            }
        }
    }

    public IEnumerable<TomlString> MarshallToml() {
        return this.Select(entry => new TomlString {
            Value = entry.Path
        });
    }

    public TomlArray ToTomlArray() {
        var arr = new TomlArray();
        arr.AddRange(MarshallToml());
        return arr;
    }
}