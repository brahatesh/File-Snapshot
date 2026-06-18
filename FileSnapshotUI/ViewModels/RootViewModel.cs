using System.Collections.ObjectModel;
using FileSnapshotUI.Models;

namespace FileSnapshotUI.ViewModels;

public class RootViewModel {
    public ObservableCollection<FileItem> Files { get; } = new();

    private FileItem? _selectedFile;
    public FileItem? SelectedFile {
        get => _selectedFile;
        set => _selectedFile = value;
    }
}