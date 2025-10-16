using System.Diagnostics.CodeAnalysis;
using System.IO;
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

    /** Is this instance subscribed to change events on <see cref="DirList"/>? */
    private bool _subscribed = false;

    /** Print extraneous information to the standard output while running? */
    public const bool VERBOSE = true;

    /** When a folder choosing dialogue is opened, this is where it starts. */
    private string _initialDirectory = Environment.CurrentDirectory;

    /** Complete path to the directory wherein the config file is stored. */
    private static readonly string CONFIG_DIRECTORY = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FindDuplicateDirs");

    /** Complete path to the configuration file. */
    private static readonly string CONFIG_FILENAME = Path.Combine(CONFIG_DIRECTORY, "config.toml");

    /** TOML key for the default folder chooser directory. */
    private const string CONFIG_DEFAULT_DIR = "default_dir";
    /** TOML key for the array of most recent input directories. */
    private const string CONFIG_LAST_DIR_LIST = "last_dir_list";
    
    /** String which identifies the <see cref="StaticResourceExtension">static resource</see>
     * <see cref="DirectoryCollection"/>.*/
    private const string DirResource = "DirList";
    
    /** String which identifies the <see cref="StaticResourceExtension">static resource</see>
     * <see cref="DirectoryPairCollection"/>.*/
    private const string DupListResource = "DuplicateList";

    /** The <see cref="DirectoryCollection"/> resource which backs the <see cref="ListView"/> on the left-hand
     * side of the programme. It contains the top directories to search through. */
    private DirectoryCollection? DirList {
        get { return field ??= TryFindResource(DirResource) as DirectoryCollection; }
    }

    /** The <see cref="DirectoryPairCollection"/> resource which backs the <see cref="ListView"/> on the right-hand
     * side of the programme. It contains the child directories which have a non-unique
     * name across all input directories. */
    private DirectoryPairCollection? DuplicateList {
        get { return field ??= TryFindResource(DupListResource) as DirectoryPairCollection; }
    }

    /** Limits the number of directories written to the config file to be
     * re-opened next time the programme is started up.
     * <remarks> null means to write everything </remarks> */
    private static readonly int? MAX_RECENT_DIRS_TO_SAVE = null;

    /** Displays a standard file explorer dialogue to the user which
     * allows them to select however many directories they wish.
     * The user's selection is appended to the <see cref="DirList"/>. */
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

    /** Function called when the user clicks the button to select directories
     * to add to the <see cref="DirList"/>. */
    private void SelectDirs_OnClick(object sender, RoutedEventArgs e) {
        SelectDirectories();
    }

    /** Function called when the user clicks the button to clear the contents of the
     * <see cref="DirList"/>. */
    private void Clear_OnClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        DirList?.Clear();
        SaveConfig();
    }

    /** Opens a folder chooser dialogue allowing the user to select a single folder.
     From this point on, it becomes the starting directory when the user opens
     a folder dialogue through the programme. */
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

    /** Function called when the user calls the function to change the default
     * initial directory which the file chooser opens into when clicking the
     * button to select directories to add to the list. */
    private void SetInitialDir_OnClick(object sender, RoutedEventArgs e) {
        SetInitialDirectory();
    }

    /** Function called when the user clicks the <see cref="BtnStart">"start"</see> button.
     * First removes all duplicates from the <see cref="DirList"/>, then
     * proceeds to fill the <see cref="DuplicateList"/> with tuples of
     * subdirectories with identical names but different paths. */
    private void Activate_OnClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        if (DirList is null || DirList.Count < 2) {
            if (VERBOSE)
                Console.WriteLine("Directory list null or consists of fewer than 2 elements.");
            return;
        }

        DuplicateList?.Clear();
        DirList?.RemoveDuplicates();
        DuplicateList?.Add(FindDupDirectories());
    }

    /** Yields a collection of pairs of directories with identical names
     * but different paths.<br/>
     * This function is not recursive; it <em>only</em> looks for
     * <see cref="DirectoryInfo.EnumerateDirectories()">immediate children</see> of
     * the directories in the <see cref="DirList"/>.
     */
    private IEnumerable<Tuple<DirectoryInfo, DirectoryInfo>> FindDupDirectories() {
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

    /** Loads the configuration file at path <c>.AppData/Roaming/FindDuplicateDirs/config.toml</c>
     * if this exists.<br/><br/>
     * Two bits of information are stored within:
     * <list type="number">
     * <item><see cref="_initialDirectory">Default directory</see> for the folder chooser dialogues</item>
     * <item>The contents of the <see cref="DirList"/> last the programme was used.</item>
     * </list>
     * The information is automatically saved to this file whenever either is changed
     * in the programme. */
    private void LoadConfig() {
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

    /** Save information to the user's configuration file in
     * <c>.AppData/Roaming/FindDuplicateDirs/config.toml</c>.
     * Called whenever the <see cref="DirList"/> is updated by this class.
     * The following information is stored:
     * <list type="number">
     * <item><see cref="_initialDirectory">Default directory</see> for the folder chooser dialogues</item>
     * <item>The absolute paths of the contents of the <see cref="DirList"/>.</item>
     * </list>*/
    private void SaveConfig() {
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

    /** Writes the absolute path of the current
     * <see cref="_initialDirectory">default directory</see> (when
     * opening a folder dialogue) to the config file. */
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
    
    /** Writes the absolute paths of the current contents of the 
    <see cref="DirList"/> to the config file as an <see cref="TomlArray">array</see>. */
    private void WriteRecentDirectories(StreamWriter writer) {
        if (DirList != null && DirList.Count > 0) {
            var arr = DirList.ToTomlArray(MAX_RECENT_DIRS_TO_SAVE);
            writer.Write(CONFIG_LAST_DIR_LIST);
            writer.Write(" = ");
            writer.WriteLine(arr.ToInlineToml());
            writer.Flush();
        }
    }

    /** Delete the config file. Called when an error occurred
     * while attempting to parse the file. */
    private static void ClearConfigFile() {
        if (File.Exists(CONFIG_FILENAME)) {
            File.Delete(CONFIG_FILENAME);
        }
    }

    /** Function called when the user has clicked an option in the <see cref="ContextMenu">context menu</see>
     * for an <see cref="ListViewItem">item</see> in the <see cref="ListView"/> on the left side of the application (i.e. the
     * view of the <see cref="DirList"/>). */
    private void DirItem_OnRightClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        var item = e.Source as MenuItem;
        var context = item?.Parent as ContextMenu;
        var placement = context?.PlacementTarget as ListViewItem;
        if (placement?.Content is DirectoryInfo target) {
            DirList?.TryRemoveDirect(target);
            SaveConfig();
        }
    }

    /** Function called when the user has clicked an option in the <see cref="ContextMenu">context menu</see>
     * for an <see cref="ListViewItem">item</see> in the <see cref="ListView"/> on the right side of the application (i.e. the
     * view of the <see cref="DuplicateList"/>). */
    private void DupItem_OnRightClick(object sender, RoutedEventArgs e) {
        EnsureSubscribed();
        var item = e.Source as MenuItem;
        var context = item?.Parent as ContextMenu;
        var placement = context?.PlacementTarget as ListViewItem;
        if (placement?.Content is Tuple<DirectoryInfo, DirectoryInfo> target) {
            DuplicateList?.Remove(target);
        }
    }

    /** Attempts to subscribe to the <see cref="DirList"/> if it isn't already. */
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

    /** <inheritdoc cref="IObserver{DirectoryCollection}.OnNext"/><br/><br/>
     When the DirList is modified, this function is called.
     Disables the activation button if the number of elements in
     the <see cref="DirectoryCollection"/> is less than 2, and vice versa. */
    public void OnNext(DirectoryCollection value) {
        SetBtnStartState(value);
    }
    
    /** Sets the <c>IsEnabled</c> flag of the button which starts the duplication-finding process.
     * If the input <see cref="DirectoryCollection"/> is null or has fewer than 2 elements in it, then
     * the button is disabled. Otherwise, it is enabled. */
    [SuppressMessage("Performance", "CA1822:Mark members as static")] // Incorrect. BtnStart is non-static.
    private void SetBtnStartState(DirectoryCollection? value) {
        if (value == null) {
            BtnStart.IsEnabled = false;
        } else {
            BtnStart.IsEnabled = value.Count >= 2;
        }
    }
}