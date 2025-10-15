using System.DirectoryServices;
using System.Windows;
using Microsoft.Win32;

namespace FindDuplicateDirs;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = this;
    }
    
    private string _initialDirectory = Environment.CurrentDirectory;

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
        
        var collection = (DirectoryCollection?)TryFindResource("DirList");
        if (collection == null) {
            return 0;
        }
        
        foreach (string path in dialogue.FolderNames) {
            var entry = new DirectoryEntry(path);
            collection.Add(entry);
            Console.WriteLine(entry);
        }
        return collection.Count;
    }

    private void SelectDirs_OnClick(object sender, RoutedEventArgs e) {
        SelectDirectories();
    }
    
    private void Clear_OnClick(object sender, RoutedEventArgs e) {
        var collection = (DirectoryCollection?)TryFindResource("DirList");
        collection?.Clear();
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
            return _initialDirectory = dialogue.FolderName;
        }
        return _initialDirectory;
    }
    
    private void SetInitialDir_OnClick(object sender, RoutedEventArgs e) {
        SetInitialDirectory();
    }
}