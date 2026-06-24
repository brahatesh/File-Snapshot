using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
//using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.Storage;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Runtime.CompilerServices;
using FileSnapshotUI.Helpers;

namespace FileSnapshotUI.Models;

public partial class FileItem: INotifyPropertyChanged {
    private string _fileName = string.Empty;
    private string _fullPath = string.Empty;
    private readonly Guid _id;
    private string _backupPath = string.Empty;
    private DateTime _lastBackupUTC = DateTime.MinValue;
    private readonly string _defaultBackupPathRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileSnapshot");
    private bool _isDarkMode = false;
    private TimeSpan _snapshotIntervalDuration;

    public ObservableCollection<SnapshotDetails> Snapshots { get; } = [];
    public event PropertyChangedEventHandler? PropertyChanged;

    public FileItem(string filePath, string? backupPath = null) {
        _id = Guid.NewGuid();

        var uiSettings = new Windows.UI.ViewManagement.UISettings();
        var bgColor = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
        _isDarkMode = bgColor == Colors.Black;

        FullPath = filePath;
        if (string.IsNullOrEmpty(backupPath)) {
            _backupPath = Path.Combine(_defaultBackupPathRoot, _id.ToString());
        }
        else {
            _backupPath = backupPath;
        }
        Directory.CreateDirectory(_backupPath);

        _snapshotIntervalDuration = TimeSpan.FromDays(1);
    }

    public DateTime LastBackup {
        get => _lastBackupUTC.ToLocalTime();
        set {
            _lastBackupUTC = value.ToUniversalTime();
            //OnPropertyChanged(nameof(LastBackup));
            OnPropertyChanged(nameof(LastBackupString));
        }
    }

    public string LastBackupString {
        get => _lastBackupUTC == DateTime.MinValue ? "Never" : LastBackup.ToString("G");
    }

    public TimeSpan SnapshotIntervalDuration {
        get => _snapshotIntervalDuration;
        set => _snapshotIntervalDuration = value;
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

    private string _iconGlyphPath { get; set; } = "/Assets/OtherLogoLightMode48x48.png";
    public string IconGlyphPath {
        get => _iconGlyphPath;
        private set {
            if(_iconGlyphPath != value) {
                _iconGlyphPath = value;
                OnPropertyChanged();
            }
        }
    }

    private enum _fileTypeEnum {Excel, Text, Word, Powerpoint, Other};
    private _fileTypeEnum _fileType { get; set; }

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
    
    private void UpdateTypeAndIcon() {
        var ext = Path.GetExtension(FileName).ToLowerInvariant();
        _fileType = ext switch {
            ".xlsx" or ".xls" => _fileTypeEnum.Excel,
            ".docx" or ".doc" => _fileTypeEnum.Word,
            ".pptx" or ".ppt" => _fileTypeEnum.Powerpoint,
            ".txt" or ".md" => _fileTypeEnum.Text,
            _ => _fileTypeEnum.Other
        };

        IconGlyphPath = GetIconGlyphPathFromFileType(_fileType, _isDarkMode);
    }

    private static string GetIconGlyphPathFromFileType(_fileTypeEnum fileType, bool isDarkMode) {
        string themeSuffix = isDarkMode ? "DarkMode" : "LightMode";
        var glyphPath = fileType switch {
            _fileTypeEnum.Excel => "/Assets/ExcelLogo48x48.png",
            _fileTypeEnum.Word => "/Assets/WordLogo48x48.png",
            _fileTypeEnum.Powerpoint => "/Assets/PowerpointLogo48x48.png",
            _fileTypeEnum.Text => $"/Assets/TextLogo{themeSuffix}48x48.png",
            _ => $"/Assets/OtherLogo{themeSuffix}48x48.png"
        };
        return glyphPath;
    }

    public void AddSnapshot() {
        Snapshots.Add(new SnapshotDetails(this.Id));
        LastBackup = DateTime.Now;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        //System.Diagnostics.Debug.WriteLine($"PropertyChanged: {propertyName}");
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void UpdateTheme(bool isDarkMode) {
        if(_isDarkMode != isDarkMode) {
            _isDarkMode = isDarkMode;
            UpdateTypeAndIcon();
        }
    }
}