using FileSnapshotUI.Models;
using FileSnapshotUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileSnapshotUI.ViewModels;

/// <summary>
/// Serves as the primary view model for the application, maintaining the central 
/// collection of tracked files and providing global application state management.
/// </summary>
public partial class RootViewModel : INotifyPropertyChanged {
    /// <summary>
    /// Gets the collection of <see cref="FileItem"/> objects currently tracked by the application.
    /// </summary>
    public ObservableCollection<FileItem> Files { get; } = [];

    // Unread notifications
    private int _unreadCount;
    public int UnreadCount {
        get => _unreadCount;
        set {
            _unreadCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnreadNotifications));
        }
    }
    public bool HasUnreadNotifications => UnreadCount > 0;

    private readonly NotificationService _notificationService;

    // Currently selected file in the UI
    private FileItem? _selectedFile;
    public FileItem? SelectedFile {
        get => _selectedFile;
        set {
            if (_selectedFile != value) {
                if (_selectedFile != null) {
                    _selectedFile.PropertyChanged -= SelectedFile_PropertyChanged;
                }

                _selectedFile = value;

                if (_selectedFile != null) {
                    _selectedFile.PropertyChanged += SelectedFile_PropertyChanged;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedFileLastBackup));
            }
        }
    }

    // Setting the last backup time string to never if no file selected
    public string SelectedFileLastBackup => SelectedFile?.LastBackupString ?? "Never";

    public event PropertyChangedEventHandler? PropertyChanged;

    public RootViewModel() {
        _notificationService = App.Services.GetRequiredService<NotificationService>();
        _notificationService.ViewModel.Notifications.CollectionChanged += Notifications_CollectionChanged;
    }

    // New notification added, update unread counter
    private void Notifications_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null) {
            App.MainDispatcher.TryEnqueue(() => {
                UnreadCount += e.NewItems.Count;
            });
        }
    }

    // Selected file is changed
    private void SelectedFile_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(FileItem.LastBackupString)) {
            OnPropertyChanged(nameof(SelectedFileLastBackup));
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}