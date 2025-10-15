using System.Collections.ObjectModel;
using System.IO;
using Tommy;

namespace FindDuplicateDirs;

public class DirectoryPairCollection : ObservableCollection<Tuple<DirectoryInfo, DirectoryInfo>> {
    public void Add(IEnumerable<Tuple<DirectoryInfo, DirectoryInfo>> items) {
        foreach (Tuple<DirectoryInfo, DirectoryInfo> item in items) {
            Add(item);
        }
    }
}