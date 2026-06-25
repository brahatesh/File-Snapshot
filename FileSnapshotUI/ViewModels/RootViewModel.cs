using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileSnapshotUI.ViewModels;

public partial class RootViewModel : INotifyPropertyChanged {
    public ObservableCollection<FileItem> Files { get; } = [];

    private FileItem? _selectedFile;
    public FileItem? SelectedFile {
        get => _selectedFile;
        set {
            if(_selectedFile != value) {
                if(_selectedFile != null) {
                    _selectedFile.PropertyChanged -= SelectedFile_PropertyChanged;
                }

                _selectedFile = value;

                if(_selectedFile != null) {
                    _selectedFile.PropertyChanged += SelectedFile_PropertyChanged;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedFileLastBackup));
            }
        }
    }

    public string SelectedFileLastBackup => SelectedFile?.LastBackupString ?? "Never";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SelectedFile_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if(e.PropertyName == nameof(FileItem.LastBackupString)) {
            OnPropertyChanged(nameof(SelectedFileLastBackup));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}