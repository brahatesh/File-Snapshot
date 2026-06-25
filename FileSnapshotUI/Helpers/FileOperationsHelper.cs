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
            if (subDir.FullName == sourceDir) continue;
            string newDestDir = Path.Combine(destDir, subDir.Name);
            await CopyDirectoryAsync(subDir.FullName, newDestDir, token);
        }
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

    public static async Task DeleteDirectoryASync(string dirPath, CancellationToken token) {
        var dir = new DirectoryInfo(dirPath);
        if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

        Directory.Delete(dirPath, true);
    }
}