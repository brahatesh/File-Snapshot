using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Windows.Foundation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Helpers;

/// <summary>
/// Provides utility methods for performing asynchronous file system operations 
/// and sanitizing Microsoft Office documents.
/// </summary>
public static class FileOperationsHelper {
    /// <summary>
    /// Recursively copies a directory to a destination, respecting cancellation tokens.
    /// </summary>
    /// <param name="sourceDir">The path to the source directory.</param>
    /// <param name="destDir">The path to the destination directory.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task CopyDirectoryAsync(string sourceDir, string destDir, CancellationToken token) {
        var dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        string absoluteDestDir = Path.GetFullPath(destDir);

        Directory.CreateDirectory(destDir);

        // Copy each file in directory
        foreach (FileInfo file in dir.GetFiles()) {
            token.ThrowIfCancellationRequested();
            string targetFilePath = Path.Combine(destDir, file.Name);
            using var sourceStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            using var destStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await sourceStream.CopyToAsync(destStream, CancellationToken.None); // Cancellation Token set to None to not cancel mid copy
        }

        // Copy directory and resurive call to copy contents of directory
        foreach (DirectoryInfo subDir in dir.GetDirectories()) {
            token.ThrowIfCancellationRequested();
            if (string.Equals(subDir.FullName, absoluteDestDir, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }
            string newDestDir = Path.Combine(destDir, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, newDestDir, token);
        }
    }

    /// <summary>
    /// Copies a single file from a source path to a destination directory asynchronously.
    /// </summary>
    /// <param name="sourceFile">The full path of the source file to copy.</param>
    /// <param name="destDir">The destination directory where the file will be copied.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous copy operation.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the destination directory does not exist.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the source file does not exist.</exception>
    public static async Task CopyFileAsync(string sourceFile, string destDir, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        var dir = new DirectoryInfo(destDir);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Destination directory not found: {dir.FullName}");

        if (!File.Exists(sourceFile)) throw new FileNotFoundException($"Source file not found: {sourceFile}");

        string fileName = Path.GetFileName(sourceFile);
        string destFile = Path.Combine(destDir, fileName);

        using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
        using FileStream destStream = new(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);

        await sourceStream.CopyToAsync(destStream, CancellationToken.None); // Cancellation Token set to None to not cancel mid copy
    }

    /// <summary>
    /// Asynchronously copies a specified list of tracked files from a source directory to a destination directory.
    /// </summary>
    /// <param name="sourceDir">The root directory where the tracked files are located.</param>
    /// <param name="destDir">The root directory where the files should be copied.</param>
    /// <param name="trackedFiles">A collection of relative file paths to be copied.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <param name="moveGit">If true, the entire .git directory is copied to the destination.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is built specifically to copy files tracked by the snapshot.
    /// </remarks>
    public static async Task CopyTrackedFilesAsync(string sourceDir, string destDir, IEnumerable<string> trackedFiles, CancellationToken token, bool moveGit = false) {
        // Copies the git repo
        if(moveGit) {
            string sourceGit = Path.Combine(sourceDir, ".git");
            string destGit = Path.Combine(destDir, ".git");
            if (Directory.Exists(sourceGit)) {
                await CopyDirectoryAsync(sourceGit, destGit, token);
            }
        }

        // Copy all tracked files
        foreach (var relPath in trackedFiles) {
            token.ThrowIfCancellationRequested();
            string srcFile = Path.Combine(sourceDir, relPath);
            string destFile = Path.Combine(destDir, relPath);

            if (File.Exists(srcFile)) {
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                using var sourceStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
                using var destStream = new FileStream(destFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
                await sourceStream.CopyToAsync(destStream, CancellationToken.None);
            }
        }
    }

    /// <summary>
    /// Asynchronously deletes tracked files and cleans up the directory structure.
    /// </summary>
    /// <param name="dirPath">The root directory path from which to delete files.</param>
    /// <param name="trackedFiles">A collection of relative paths of files to be deleted.</param>
    /// <param name="trackedDirectories">A collection of relative paths of directories to be pruned.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <param name="deleteGit">If true, the .git directory within <paramref name="dirPath"/> is also removed.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method is built to delete tracked snapshot files. Also, deletes empty tracked directories.
    /// </remarks>
    public static async Task DeleteTrackedFilesAsync(string dirPath, IEnumerable<string> trackedFiles, IEnumerable<string> trackedDirectories, CancellationToken token, bool deleteGit = false) {
        // Delete all tracked files
        foreach (var relPath in trackedFiles) {
            token.ThrowIfCancellationRequested();
            string targetFile = Path.Combine(dirPath, relPath);
            if (File.Exists(targetFile)) {
                File.SetAttributes(targetFile, FileAttributes.Normal);
                File.Delete(targetFile);
            }
        }

        // Check all tracked directories (from deepest to shallowest) then delete if empty
        var orderedDirs = trackedDirectories.OrderByDescending(d => d.Length);
        foreach (var relDir in orderedDirs) {
            string targetDir = Path.Combine(dirPath, relDir);
            if (Directory.Exists(targetDir) && !Directory.EnumerateFileSystemEntries(targetDir).Any()) {
                Directory.Delete(targetDir, false);
            }
        }

        // Deletes git repo is requested
        if (deleteGit) {
            string gitDir = Path.Combine(dirPath, ".git");
            if (Directory.Exists(gitDir)) {
                var dir = new DirectoryInfo(gitDir);
                RemoveReadOnlyAttributes(dir);
                Directory.Delete(gitDir, true);
            }
        }

        // Deletes dirPath directory is empty
        if (Directory.Exists(dirPath) && !Directory.EnumerateFileSystemEntries(dirPath).Any()) {
            Directory.Delete(dirPath);
        }
    }

    /// <summary>
    /// Verifies whether the application has permission to read entries from the specified directory.
    /// </summary>
    /// <param name="dirPath">The full path of the directory to check.</param>
    /// <returns>
    /// <c>true</c> if the directory exists and is accessible; 
    /// <c>false</c> if the path is invalid, does not exist, or an access exception occurs.
    /// </returns>
    public static bool CanReadFromDir(string dirPath) {
        if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return false;

        try {
            // Test if app can fetch entries in dir. Any() is needed because
            // EnumerateFileSystemEntries might not fail even with no permission.
            var testRead = Directory.EnumerateFileSystemEntries(dirPath).Any();
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    /// <summary>
    /// Verifies whether the application has permission to create files within the specified directory.
    /// </summary>
    /// <param name="dirPath">The full path of the directory to check.</param>
    /// <returns>
    /// <c>true</c> if the application can successfully create and write 
    /// in the directory; otherwise, <c>false</c>.
    /// </returns>
    public static bool CanWriteToDir(string dirPath) {
        if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return false;

        try {
            // Write zero bytes to a temp file to test write permission
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

    /// <summary>
    /// Recursively removes the <see cref="FileAttributes.ReadOnly"/> attribute from a 
    /// directory and all its contents.
    /// </summary>
    /// <param name="directory">The <see cref="DirectoryInfo"/> representing the root of the structure to modify.</param>
    /// <remarks>
    /// This method is primarily to remove ReadOnly attribute from files stored in .git repo.
    /// </remarks>
    private static void RemoveReadOnlyAttributes(DirectoryInfo directory) {
        // Set attribute for all files
        foreach (FileInfo file in directory.GetFiles("*", SearchOption.AllDirectories)) {
            file.Attributes = FileAttributes.Normal;
        }

        // Set attribute for all dirs
        foreach (DirectoryInfo dir in directory.GetDirectories("*", SearchOption.AllDirectories)) {
            dir.Attributes = FileAttributes.Normal;
        }

        directory.Attributes = FileAttributes.Normal;
    }

    /// <summary>
    /// Sanitizes an Excel workbook by removing metadata, printer settings, 
    /// and internal tracking parts unnecessary for snapshotting.
    /// </summary>
    /// <param name="filePath">The absolute path to the Excel (.xlsx) file.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <remarks>
    /// This method performs the following cleanup operations:
    /// <list type="bullet">
    /// <item>
    /// <description>Removes all <see cref="SpreadsheetPrinterSettingsPart"/> parts from worksheets.</description>
    /// </item>
    /// <item>
    /// <description>Deletes the <see cref="CalculationChainPart"/> to prevent issues with cross-session calculations.</description>
    /// </item>
    /// <item>
    /// <description>Removes <see cref="WorkbookRevisionHeaderPart"/> to strip revision history.</description>
    /// </item>
    /// <item>
    /// <description>Deletes <see cref="CustomXmlMappingsPart"/> to remove custom XML metadata.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public static void SanitizeExcelFile(string filePath, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        using SpreadsheetDocument document = SpreadsheetDocument.Open(filePath, true);

        var workbookPart = document.WorkbookPart;
        if (workbookPart == null) return;

        foreach (var worksheetPart in workbookPart.WorksheetParts) {
            if (worksheetPart.SpreadsheetPrinterSettingsParts.Any()) {
                worksheetPart.DeleteParts(worksheetPart.SpreadsheetPrinterSettingsParts);
            }
        }

        if (workbookPart.CalculationChainPart != null) {
            workbookPart.DeletePart(workbookPart.CalculationChainPart);
        }

        if (workbookPart.WorkbookRevisionHeaderPart != null) {
            workbookPart.DeletePart(workbookPart.WorkbookRevisionHeaderPart);
        }

        if (workbookPart.CustomXmlMappingsPart != null) {
            workbookPart.DeletePart(workbookPart.CustomXmlMappingsPart);
        }
    }

    /// <summary>
    /// Sanitizes a PowerPoint presentation by removing comments, author metadata, 
    /// and custom XML parts.
    /// </summary>
    /// <param name="filePath">The absolute path to the PowerPoint (.pptx) file.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <remarks>
    /// This method performs the following cleanup operations:
    /// <list type="bullet">
    /// <item>
    /// <description>Removes all <see cref="CustomXmlPart"/> objects from the presentation.</description>
    /// </item>
    /// <item>
    /// <description>Deletes the <see cref="CommentAuthorsPart"/> to strip comment author metadata.</description>
    /// </item>
    /// <item>
    /// <description>Removes <see cref="SlideCommentsPart"/> from every slide within the presentation.</description>
    /// </item>
    /// </list>
    /// </remarks>
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

    /// <summary>
    /// Sanitizes a Word document by removing sensitive metadata, comments, 
    /// printer settings, and web settings.
    /// </summary>
    /// <param name="filePath">The absolute path to the Word (.docx) file.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <remarks>
    /// This method performs the following cleanup operations:
    /// <list type="bullet">
    /// <item>
    /// <description>Removes all <see cref="CustomXmlPart"/> objects.</description>
    /// </item>
    /// <item>
    /// <description>Deletes the <see cref="WebSettingsPart"/> to remove web-related formatting data.</description>
    /// </item>
    /// <item>
    /// <description>Removes all <see cref="WordprocessingPrinterSettingsPart"/> parts.</description>
    /// </item>
    /// <item>
    /// <description>Deletes the <see cref="WordprocessingCommentsPart"/> to strip document comments.</description>
    /// </item>
    /// </list>
    /// </remarks>
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