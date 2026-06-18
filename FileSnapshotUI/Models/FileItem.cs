using Microsoft.UI.Xaml.Media;
using System;
using System.IO;

namespace FileSnapshotUI.Models;

public class FileItem {
    private string _fileName = string.Empty;
    private string _fullPath = string.Empty;
    private Guid _id = Guid.NewGuid();

    public Guid Id {
        get => _id;
    }

    public string FileName { 
        get => _fileName; 
    }

    private string _iconGlyphPath { get; set; } = "/Assets/OtherLogo48x48.png";
    public string IconGlyphPath {
        get => _iconGlyphPath;
    }

    private enum _fileTypeEnum {Excel, Text, Word, Powerpoint, Other};
    private _fileTypeEnum _fileType { get; set; }

    public string FullPath { 
        get => _fullPath; 
        set { 
            _fullPath = value ?? string.Empty;
            _fileName = Path.GetFileName(_fullPath);
            UpdateTypeAndIcon();
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

        _iconGlyphPath = GetIconGlyphPathFromFileType(_fileType);
    }

    private static string GetIconGlyphPathFromFileType(_fileTypeEnum fileType) =>
        fileType switch {
            _fileTypeEnum.Excel => "/Assets/ExcelLogo48x48.png",
            _fileTypeEnum.Word => "/Assets/WordLogo48x48.png",
            _fileTypeEnum.Powerpoint => "/Assets/PowerpointLogo48x48.png",
            _fileTypeEnum.Text => "/Assets/TextLogo48x48.png",
            _ => "/Assets/OtherLogo48x48.png"
        };
        
}