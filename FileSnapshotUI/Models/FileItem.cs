using FileSnapshotUI.Helpers;
using LibGit2Sharp;
using Microsoft.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace FileSnapshotUI.Models;

/// <summary>
/// Represents a file tracked by the application, managing its snapshot lifecycle, 
/// associated Git repository, and UI-specific display properties.
/// </summary>
/// <remarks>
/// Upon initialization, this model automatically creates a local directory 
/// within the application's data folder and initializes a Git repository 
/// to manage the file's historical snapshots.
/// </remarks>
public partial class FileItem : INotifyPropertyChanged {
    private string _fileName = string.Empty;
    private string _fullPath = string.Empty;
    private readonly Guid _id;
    private string _backupPath = string.Empty;  // Git repo path
    private DateTime _lastBackupUTC = DateTime.MinValue;

    // Default root path is set in %LocalAppData%/FileSnapshot
    private readonly string _defaultBackupPathRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileSnapshot");
    private bool _isDarkMode = false;           // Checking app dark mode, used to set icon in App
    private TimeSpan _snapshotIntervalDuration;
    private bool _isProcessing;                 // Set when the file is under any kind of process, used to lock UI

    // Default icon is set to Others
    private string _iconGlyphPath { get; set; } = "/Assets/OtherLogoLightMode48x48.png";

    public ObservableCollection<SnapshotDetails> Snapshots { get; } = [];
    public event PropertyChangedEventHandler? PropertyChanged;      // Necessary to let the UI know when a property has changed

    /// <summary>
    /// Initializes a new instance of the <see cref="FileItem"/> class.
    /// </summary>
    /// <param name="filePath">The absolute path to the file to track.</param>
    /// <param name="backupPath">Optional custom path for storing backups. If null, uses the application data root.</param>
    public FileItem(string filePath, string? backupPath = null) {
        _id = Guid.NewGuid();

        // Get Dark Mode status
        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        var bgColor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        _isDarkMode = bgColor == Colors.Black;

        FullPath = filePath;

        // Create backup folder is custom path not specified
        if (string.IsNullOrEmpty(backupPath)) {
            _backupPath = Path.Combine(_defaultBackupPathRoot, _id.ToString());
        }
        else {
            _backupPath = backupPath;
        }
        Directory.CreateDirectory(_backupPath);
        Repository.Init(_backupPath);

        // Set default snapshot interval to 1 day
        _snapshotIntervalDuration = TimeSpan.FromDays(1);
    }

    public DateTime LastBackup {
        get => _lastBackupUTC.ToLocalTime();
        set {
            _lastBackupUTC = value.ToUniversalTime();
            OnPropertyChanged(nameof(LastBackupString));
        }
    }

    public bool IsProcessing {
        get => _isProcessing;
        set {
            if (_isProcessing != value) {
                _isProcessing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotProcessing));
            }
        }
    }

    public bool IsNotProcessing => !IsProcessing;

    public string LastBackupString {
        get => _lastBackupUTC == DateTime.MinValue ? "Never" : LastBackup.ToString("G");
    }

    public TimeSpan SnapshotIntervalDuration {
        get => _snapshotIntervalDuration;
        set {
            if (_snapshotIntervalDuration != value) {
                _snapshotIntervalDuration = value;
                OnPropertyChanged();
            }
        }
    }

    public string SnapshotIntervalDurationString {
        get => TimeSpanJiraStringConverter.TimeSpanToJira(_snapshotIntervalDuration);
    }

    public Guid Id {
        get => _id;
    }

    public string FileName {
        get => _fileName;
    }

    public string IconGlyphPath {
        get => _iconGlyphPath;
        private set {
            if (_iconGlyphPath != value) {
                _iconGlyphPath = value;
                OnPropertyChanged();
            }
        }
    }

    // To determine file type to determine the steps needed to take snapshot
    public enum FileTypeEnum { Excel, Text, Word, Powerpoint, Other };
    private FileTypeEnum _fileType { get; set; }

    public FileTypeEnum FileType {
        get => _fileType;
    }

    public string FullPath {
        get => _fullPath;
        set {
            _fullPath = value ?? string.Empty;
            _fileName = Path.GetFileName(_fullPath);
            UpdateTypeAndIcon();

            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
        }
    }

    public string BackupPath {
        get => _backupPath;
        set {
            _backupPath = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Update the icon and type based on file name.
    /// </summary>
    private void UpdateTypeAndIcon() {
        var ext = Path.GetExtension(FileName).ToLowerInvariant();
        _fileType = ext switch {
            ".xlsx" or ".xls" => FileTypeEnum.Excel,
            ".docx" or ".doc" => FileTypeEnum.Word,
            ".pptx" or ".ppt" => FileTypeEnum.Powerpoint,
            ".txt" or ".md" => FileTypeEnum.Text,
            _ => FileTypeEnum.Other
        };

        IconGlyphPath = GetIconGlyphPathFromFileType(_fileType, _isDarkMode);
    }

    /// <summary>
    /// Update the icon path based on file type.
    /// </summary>
    private static string GetIconGlyphPathFromFileType(FileTypeEnum fileType, bool isDarkMode) {
        string themeSuffix = isDarkMode ? "DarkMode" : "LightMode";
        var glyphPath = fileType switch {
            FileTypeEnum.Excel => "/Assets/ExcelLogo48x48.png",
            FileTypeEnum.Word => "/Assets/WordLogo48x48.png",
            FileTypeEnum.Powerpoint => "/Assets/PowerpointLogo48x48.png",
            FileTypeEnum.Text => $"/Assets/TextLogo{themeSuffix}48x48.png",
            _ => $"/Assets/OtherLogo{themeSuffix}48x48.png"
        };
        return glyphPath;
    }

    /// <summary>
    /// Adds a new snapshot record to the <see cref="Snapshots"/> collection.
    /// </summary>
    /// <param name="snapshotTime">The timestamp of the snapshot.</param>
    /// <param name="commit">The <see cref="Commit"/> object from the file's Git repository.</param>
    /// <param name="trackedFiles">A collection of relative paths included in the snapshot.</param>
    /// <param name="trackedDirectories">A collection of directories included in the snapshot.</param>
    public void AddSnapshot(DateTime snapshotTime, Commit commit, IEnumerable<string> trackedFiles, IEnumerable<string> trackedDirectories) {
        Snapshots.Add(new SnapshotDetails(this.Id, snapshotTime, commit, trackedFiles, trackedDirectories));
        LastBackup = snapshotTime;
    }

    /// <summary>
    /// Notifies listeners (UI) that a property value has changed.
    /// </summary>
    /// <param name="propertyName">
    /// The name of the property that changed. If omitted, the <see cref="CallerMemberNameAttribute"/> 
    /// automatically provides the name of the calling method.
    /// </param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Updates the model's theme context and refreshes visual assets.
    /// </summary>
    public void UpdateTheme(bool isDarkMode) {
        if (_isDarkMode != isDarkMode) {
            _isDarkMode = isDarkMode;
            UpdateTypeAndIcon();
        }
    }
}