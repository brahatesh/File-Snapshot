using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services;

public class SnapshotService(NotificationService notifications) {
    private readonly NotificationService _notifications = notifications;

    public async Task PerformSnapshotAsync(FileItem file, CancellationToken token, bool silent = false) {
        var workingDir = AppEnvironment.GetTempFolder();
        Directory.CreateDirectory(workingDir);
        string backupDir = file.BackupPath;
        string sourcePath = file.FullPath;
        var fileType = file.FileType;
        DateTime snapshotTime = DateTime.Now;

        List<string> newTrackedFiles = [];
        List<string> newTrackedDirectories = [];

        IEnumerable<string> oldTrackedFiles = [];
        IEnumerable<string> oldTrackedDirectories = [];

        if (file.Snapshots.Count > 0) {
            oldTrackedFiles = file.Snapshots.Last().TrackedFiles;
            oldTrackedDirectories = file.Snapshots.Last().TrackedDirectories;
        }

        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = true);

        try {
            if (!Repository.IsValid(backupDir)) throw new InvalidOperationException("Git repo not found. Backup location invalid.");

            await FileOperationsHelper.DeleteTrackedFilesAsync(backupDir, oldTrackedFiles, oldTrackedDirectories, token);

            switch (fileType) {
                case FileItem.FileTypeEnum.Excel:
                case FileItem.FileTypeEnum.Word:
                case FileItem.FileTypeEnum.Powerpoint:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, workingDir, token);
                    var tempPath = Path.Combine(workingDir, Path.GetFileName(sourcePath));

                    if (fileType == FileItem.FileTypeEnum.Excel) FileOperationsHelper.SanitizeExcelFile(tempPath, token);
                    else if (fileType == FileItem.FileTypeEnum.Word) FileOperationsHelper.SanitizeWordFile(tempPath, token);
                    else if (fileType == FileItem.FileTypeEnum.Powerpoint) FileOperationsHelper.SanitizePowerPointFile(tempPath, token);

                    var extractPath = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(sourcePath));
                    ZipFile.ExtractToDirectory(tempPath, extractPath);
                    await FileOperationsHelper.CopyDirectoryAsync(extractPath, backupDir, token);

                    newTrackedFiles.AddRange(GetRelativeFiles(extractPath));
                    newTrackedDirectories.AddRange(GetRelativeDirectories(extractPath)); 
                    break;

                default:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, backupDir, token);
                    newTrackedFiles.AddRange([$"{Path.GetFileName(sourcePath)}"]);
                    break;
            }

            using var repo = new Repository(backupDir);
            //Commands.Stage(repo, "*");

            //Signature author = new(Environment.UserName, Environment.UserDomainName, snapshotTime);
            //Signature committer = new("File Snapshot App", "@filesnapshot", snapshotTime);

            //Commit commit = repo.Commit($"{snapshotTime:G}", author, committer);
            Commit commit = GitHelper.StageAndCommit(repo, snapshotTime, newTrackedFiles);

            App.MainDispatcher.TryEnqueue(() => file.AddSnapshot(snapshotTime, commit, newTrackedFiles, newTrackedDirectories));
        }
        catch (Exception ex) {
            if (!silent) App.MainDispatcher.TryEnqueue(() => _notifications.AddNotification(file, $"Snapshot failed: {ex.Message}"));
            else if (ex is not EmptyCommitException) throw;
        }

        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = false);
        Directory.Delete(workingDir, true);
    }
    private static IEnumerable<string> GetRelativeFiles(string basePath) {
        return Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
                        .Select(f => Path.GetRelativePath(basePath, f));
    }

    private static IEnumerable<string> GetRelativeDirectories(string basePath) {
        return Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                        .Select(d => Path.GetRelativePath(basePath, d));
    }
}