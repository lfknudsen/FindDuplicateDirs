using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tommy;

namespace FindDuplicateDirs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable, IObserver<DirectoryCollection> {
    public MainWindow() {
        InitializeComponent();
        DataContext = this;
        LoadConfig();
        Title = "Duplicate Directory Finder";
        EnsureSubscribed();
        SetBtnStartState(DirList);
    }

    private bool _subscribed = false;

    public const bool VERBOSE = true;

    /** When the folder find dialogue is opened, this is where it starts. */
    private string _initialDirectory = Environment.CurrentDirectory;

    private static readonly string CONFIG_DIRECTORY = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FindDuplicateDirs");

    private static readonly string CONFIG_FILENAME = Path.Combine(CONFIG_DIRECTORY, "config.toml");

    private const string CONFIG_DEFAULT_DIR = "default_dir";
    private const string CONFIG_LAST_DIR_LIST = "last_dir_list";
    private const string DirResource = "DirList";
    private const string DupListResource = "DuplicateList";

    private DirectoryCollection? DirList {
        get { return field ??= TryFindResource(DirResource) as DirectoryCollection; }
    }

    private DirectoryPairCollection? DuplicateList {
        get { return field ??= TryFindResource(DupListResource) as DirectoryPairCollection; }
    }

    /** Null = all */
    private static readonly int? MAX_RECENT_DIRS_TO_SAVE = null;

    private int SelectDirectories() {
        EnsureSubscribed();
        var dialogue = new OpenFolderDialog {
            Multiselect = true,
            ShowHiddenItems = true,
            AddToRecent = false,
            InitialDirectory = _initialDirectory,
            Title = "Select directories to traverse."
        };
        bool? pressedOK = dialogue.ShowDialog();
        if (!pressedOK.HasValue || !pressedOK.Value) {
            DirList?.RemoveDuplicates();
            return 0;
        }

        if (DirList == null) {
            return 0;
        }

        DirList.Add(dialogue.FolderNames.Select(n => new DirectoryInfo(n)));
        foreach (string path in dialogue.FolderNames) {
            var entry = new DirectoryInfo(path);
            DirList.Add(entry);
        }

        SaveConfig();

        return DirList.Count;
    }

    private void SelectDirs_OnClick(object sender, RoutedEventArgs e) {
        SelectDirectories();
    }

    private void Clear_OnClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        DirList?.Clear();
        SaveConfig();
    }

    private string SetInitialDirectory() {
        var dialogue = new OpenFolderDialog {
            Multiselect = false,
            ShowHiddenItems = true,
            AddToRecent = false,
            InitialDirectory = _initialDirectory,
            Title = "Select a new initial directory."
        };
        bool? pressedOK = dialogue.ShowDialog();
        if (pressedOK.HasValue && pressedOK.Value) {
            _initialDirectory = dialogue.FolderName;
            SaveConfig();
        }

        return _initialDirectory;
    }

    private void SetInitialDir_OnClick(object sender, RoutedEventArgs e) {
        SetInitialDirectory();
    }

    private void Activate_OnClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        if (DirList is null || DirList.Count < 2) {
            if (VERBOSE)
                Console.WriteLine("Directory list null or consists of fewer than 2 elements.");
            return;
        }

        DuplicateList?.Clear();
        DirList?.RemoveDuplicates();
        DuplicateList?.Add(DupDirectories());
    }

    private IEnumerable<Tuple<DirectoryInfo, DirectoryInfo>> DupDirectories() {
        EnsureSubscribed();
        if (DirList == null || DirList.Count < 2 || DuplicateList == null) yield break;

        var hashes = new HashSet<DirectoryInfo>(DirNameComparer.Instance);
        int count = DirList.Count;
        for (int i = 0; i < count; ++i) {
            IEnumerable<DirectoryInfo> subs = DirList[i].EnumerateDirectories();
            foreach (DirectoryInfo entry in subs) {
                DirectoryInfo? existingDirInfo;
                bool existing = hashes.TryGetValue(entry, out existingDirInfo);
                if (existing && existingDirInfo != null) {
                    yield return new Tuple<DirectoryInfo, DirectoryInfo>(existingDirInfo, entry);
                } else {
                    hashes.Add(entry);
                }
            }
        }
    }

    private void LoadConfig() {
        EnsureSubscribed();
        
        if (!Directory.Exists(CONFIG_DIRECTORY)) {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
            return;
        }

        if (!File.Exists(CONFIG_FILENAME)) {
            return;
        }

        FileStream fs = File.Open(CONFIG_FILENAME, FileMode.Open, FileAccess.Read);
        if (fs.Length > int.MaxValue) {
            return;
        }

        BufferedStream bs = new BufferedStream(fs);
        TextReader reader = new StreamReader(bs);
        try {
            TomlTable config = TOML.Parse(reader);
            TomlNode? defaultDirectory = config[CONFIG_DEFAULT_DIR];
            if (defaultDirectory is { IsString: true }) {
                string dirPath = defaultDirectory.AsString.Value;
                if (Directory.Exists(dirPath)) {
                    _initialDirectory = dirPath;
                }
            }

            TomlNode? lastList = config[CONFIG_LAST_DIR_LIST];
            if (lastList is { IsArray : true }) {
                DirList?.Add(lastList.AsArray);
            }
        } catch (TomlParseException _) {
            Console.Error.WriteLine("Failed to load config. Deleting it.");
            ClearConfigFile();
        }
    }

    public void Dispose() {
        SaveConfig();
        GC.SuppressFinalize(this);
    }

    ~MainWindow() {
        Dispose();
    }

    private void SaveConfig() {
        EnsureSubscribed();
        
        if (!Directory.Exists(CONFIG_DIRECTORY)) {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
        }

        try {
            using FileStream fs = File.Open(CONFIG_FILENAME, FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(fs);
            writer.AutoFlush = false;
            WriteInitialDirectory(writer);
            WriteRecentDirectories(writer);
        } catch (Exception _) {
            // Non-fatal. We just move on.
        }
    }

    private void WriteInitialDirectory(StreamWriter writer) {
        if (_initialDirectory != Environment.CurrentDirectory) {
            var tomlString = new TomlString {
                Value = _initialDirectory
            };
            writer.Write(CONFIG_DEFAULT_DIR);
            writer.Write(" = ");
            writer.WriteLine(tomlString.ToInlineToml());
            writer.Flush();
        }
    }

    private void WriteRecentDirectories(StreamWriter writer) {
        if (DirList != null && DirList.Count > 0) {
            var arr = DirList.ToTomlArray(MAX_RECENT_DIRS_TO_SAVE);
            writer.Write(CONFIG_LAST_DIR_LIST);
            writer.Write(" = ");
            writer.WriteLine(arr.ToInlineToml());
            writer.Flush();
        }
    }

    private static void ClearConfigFile() {
        if (File.Exists(CONFIG_FILENAME)) {
            File.Delete(CONFIG_FILENAME);
        }
    }

    private void DirItem_OnRightClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        var item = e.Source as MenuItem;
        var context = item?.Parent as ContextMenu;
        var placement = context?.PlacementTarget as ListViewItem;
        if (placement?.Content is DirectoryInfo target) {
            DirList?.Remove(target);
            SaveConfig();
        }
    }

    private void DupItem_OnRightClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        var item = e.Source as MenuItem;
        var context = item?.Parent as ContextMenu;
        var placement = context?.PlacementTarget as ListViewItem;
        if (placement?.Content is Tuple<DirectoryInfo, DirectoryInfo> target) {
            DuplicateList?.Remove(target);
        }
    }

    /** If this instance isn't subscribed to DirList, attempt to subscribe. */
    private void EnsureSubscribed() {
        if (!_subscribed) {
            _subscribed = DirList?.Subscribe(this) != null;
        }
    }

    public void OnCompleted() {
        // Irrelevant
    }

    public void OnError(Exception error) {
        // Irrelevant
    }

    /** When the DirList is modified, this function is called.
     Disables the activation button if the number of elements in
     the DirectoryCollection is less than 2, and vice versa. */
    public void OnNext(DirectoryCollection value) {
        SetBtnStartState(value);
    }
    
    [SuppressMessage("Performance", "CA1822:Mark members as static")] // Incorrect. BtnStart is non-static.
    private void SetBtnStartState(DirectoryCollection? value) {
        if (value == null) {
            BtnStart.IsEnabled = false;
        } else {
            BtnStart.IsEnabled = value.Count >= 2;
        }
    }
}