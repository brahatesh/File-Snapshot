using DocumentFormat.OpenXml.Packaging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Helpers;

public static class FileOperationsHelper {
    public static async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken token) {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        string absoluteDestDir = Path.GetFullPath(destDir);

        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles()) {
            token.ThrowIfCancellationRequested();
            string targetFilePath = Path.Combine(destDir, file.Name);
            using var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            using var destStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(destStream);
        }

        foreach (DirectoryInfo subDir in dir.GetDirectories()) {
            token.ThrowIfCancellationRequested();
            if (string.Equals(subDir.FullName, absoluteDestDir, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }
            string newDestDir = Path.Combine(destDir, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, newDestDir, token);
        }
    }

    public static async Task CopyFileAsync(string sourceFile, string destDir, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        var dir = new DirectoryInfo(destDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Destination directory not found: {dir.FullName}");

        if (!File.Exists(sourceFile)) throw new FileNotFoundException($"Source file not found: {sourceFile}");

        string fileName = Path.GetFileName(sourceFile);
        string destFile = Path.Combine(destDir, fileName);

        using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        using FileStream destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        await sourceStream.CopyToAsync(destStream);
    }

    public static bool CanReadFromDir(string dirPath) {
        if(string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return false;

        try {
            var testRead = Directory.EnumerateFileSystemEntries(dirPath).Any();
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    public static bool CanWriteToDir(string dirPath) {
        if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return false;

        try {
            string tempFile = Path.Combine(dirPath, $"io_test_{Guid.NewGuid():N}.tmp");
            using FileStream fs = new(
                tempFile,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);
            fs.WriteByte(0);

            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    public static bool CanCopyFile(string filePath) {
        if(string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) {
            return false;
        }

        try {
            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); 
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    public static async Task DeleteDirectoryASync(string dirPath, CancellationToken token, bool deleteGit = true) {
        var dir = new DirectoryInfo(dirPath);
        if (!dir.Exists) return;

        if (deleteGit) {
            RemoveReadOnlyAttributes(dir);
            Directory.Delete(dirPath, true);
        }
        else {
            foreach (FileInfo file in dir.GetFiles()) {
                file.Attributes = FileAttributes.Normal;
                File.Delete(file.FullName);
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories()) {
                if (subDir.Name == ".git") continue;
                await DeleteDirectoryASync(subDir.FullName, token);
            }
        }
    }

    private static void RemoveReadOnlyAttributes(DirectoryInfo directory) {
        foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories)) {
            file.Attributes = FileAttributes.Normal;
        }
        
        foreach (DirectoryInfo dir in directory.GetDirectories("*", SearchOption.AllDirectories)) {
            dir.Attributes = FileAttributes.Normal;
        }
        
        directory.Attributes = FileAttributes.Normal;
    }

    public static void SanitizeExcelFile(string filePath, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true);

        var workbookPart = document.WorkbookPart;
        if (workbookPart == null) return;

        foreach(var worksheetPart in workbookPart.WorksheetParts) {
            if(worksheetPart.SpreadsheetPrinterSettingsParts.Any()) {
                worksheetPart.DeleteParts(worksheetPart.SpreadsheetPrinterSettingsParts);
            }
        }

        if (workbookPart.CalculationChainPart != null) {
            workbookPart.DeletePart(workbookPart.CalculationChainPart);
        }

        if (workbookPart.WorkbookRevisionHeaderPart != null) {
            workbookPart.DeletePart(workbookPart.WorkbookRevisionHeaderPart);
        }

        if(workbookPart.CustomXmlMappingsPart != null) {
            workbookPart.DeletePart(workbookPart.CustomXmlMappingsPart);
        }
    }

    public static void SanitizePowerPointFile(string filePath, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        using PresentationDocument document = PresentationDocument.Open(filePath, true);

        var presentationPart = document.PresentationPart;
        if (presentationPart == null) return;

        if (presentationPart.CustomXmlParts.Any()) {
            presentationPart.DeleteParts(presentationPart.CustomXmlParts);
        }

        if (presentationPart.CommentAuthorsPart != null) {
            presentationPart.DeletePart(presentationPart.CommentAuthorsPart);
        }

        foreach (var slidePart in presentationPart.SlideParts) {
            if (slidePart.SlideCommentsPart != null) {
                slidePart.DeletePart(slidePart.SlideCommentsPart);
            }
        }
    }

    public static void SanitizeWordFile(string filePath, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        using WordprocessingDocument document = WordprocessingDocument.Open(filePath, true);

        var mainPart = document.MainDocumentPart;
        if (mainPart == null) return;

        if (mainPart.CustomXmlParts.Any()) {
            mainPart.DeleteParts(mainPart.CustomXmlParts);
        }

        if (mainPart.WebSettingsPart != null) {
            mainPart.DeletePart(mainPart.WebSettingsPart);
        }

        if (mainPart.WordprocessingPrinterSettingsParts.Any()) {
            mainPart.DeleteParts(mainPart.WordprocessingPrinterSettingsParts);
        }

        if (mainPart.WordprocessingCommentsPart != null) {
            mainPart.DeletePart(mainPart.WordprocessingCommentsPart);
        }
    }
}