using System.DirectoryServices;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Tommy;

namespace FindDuplicateDirs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, IDisposable {
    public MainWindow() {
        InitializeComponent();
        DataContext = this;
        LoadConfig();
    }

    /** When the folder find dialogue is opened, this is where it starts. */
    private string _initialDirectory = Environment.CurrentDirectory;

    private static readonly string CONFIG_DIRECTORY = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FindDuplicateDirs");

    private static readonly string CONFIG_FILENAME = Path.Combine(CONFIG_DIRECTORY, "config.toml");

    private const string CONFIG_DEFAULT_DIR = "default_dir";
    private const string CONFIG_LAST_DIR_LIST = "last_dir_list";
    private const string DirResource = "DirList";
    
    /** Cached string builder for writing to the config file. */
    private readonly StringBuilder _sb = new StringBuilder(500);

    private int SelectDirectories() {
        var dialogue = new OpenFolderDialog {
            Multiselect = true,
            ShowHiddenItems = true,
            AddToRecent = false,
            InitialDirectory = _initialDirectory,
            Title = "Select directories to traverse."
        };
        bool? pressedOK = dialogue.ShowDialog();
        if (!pressedOK.HasValue || !pressedOK.Value) {
            return 0;
        }

        var collection = (DirectoryCollection?)TryFindResource(DirResource);
        if (collection == null) {
            return 0;
        }

        foreach (string path in dialogue.FolderNames) {
            var entry = new DirectoryEntry(path);
            collection.Add(entry);
            Console.WriteLine(entry);
        }

        SaveConfig();

        return collection.Count;
    }

    private void SelectDirs_OnClick(object sender, RoutedEventArgs e) {
        SelectDirectories();
    }

    private void Clear_OnClick(object sender, RoutedEventArgs e) {
        var collection = (DirectoryCollection?)TryFindResource(DirResource);
        collection?.Clear();
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

    private void LoadConfig() {
        if (!Directory.Exists(CONFIG_DIRECTORY)) {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
            return;
        }
        if (!File.Exists(CONFIG_FILENAME)) {
            return;
        }
        
        FileStream fs = File.Open(CONFIG_FILENAME, FileMode.Open, FileAccess.Read);
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
                var collection = (DirectoryCollection?)TryFindResource(DirResource);
                collection?.AddTomlArray(lastList.AsArray);
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
        if (!Directory.Exists(CONFIG_DIRECTORY)) {
            Directory.CreateDirectory(CONFIG_DIRECTORY);
        }
        SaveNewConfig();
    }

    private void SaveNewConfig() {
        _sb.Clear();
        FileStream f = File.Open(CONFIG_FILENAME, FileMode.Create, FileAccess.Write);
        var writer = new StreamWriter(f);
        if (_initialDirectory != Environment.CurrentDirectory) {
            var tomlString = new TomlString {
                Value = _initialDirectory
            };
            _sb.Append(CONFIG_DEFAULT_DIR)
                .Append(" = ")
                .Append(tomlString.ToString());
        }
        
        var collection = (DirectoryCollection?)TryFindResource(DirResource);
        var arr = collection?.ToTomlArray();
        _sb.Append(CONFIG_LAST_DIR_LIST)
            .Append(" = ")
            .Append(arr?.ToString());
        
        writer.Write(_sb.ToString());
        writer.Flush();
        writer.Close();
    }

    private static void ClearConfigFile() {
        if (File.Exists(CONFIG_FILENAME)) {
            File.Delete(CONFIG_FILENAME);
        }
    }
}