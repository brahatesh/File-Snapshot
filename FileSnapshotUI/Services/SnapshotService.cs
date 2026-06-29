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


/// <summary>
/// Enum to determine the mode of snapshot
/// </summary>
public enum SnapshotMode {
    Manual,
    Automatic,
    Silent
}

/// <summary>
/// Orchestrates the process of creating snapshots for tracked files, 
/// including sanitization, Git staging, and commit management.
/// </summary>
public class SnapshotService(NotificationService notifications, IStateService stateService) {
    private readonly NotificationService _notifications = notifications;
    private readonly IStateService _stateService = stateService;

    /// <summary>
    /// Performs an asynchronous snapshot operation on the specified <see cref="FileItem"/>.
    /// </summary>
    /// <param name="file">The <see cref="FileItem"/> to snapshot.</param>
    /// <param name="mode">The <see cref="SnapshotMode"/> (Manual, Automatic, or Silent) governing behavior.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous snapshot operation.</returns>
    public async Task PerformSnapshotAsync(FileItem file, SnapshotMode mode, CancellationToken token) {
        token.ThrowIfCancellationRequested();

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

        // If old snapshots present, get tracked files of latest snapshot
        if (file.Snapshots.Count > 0) {
            oldTrackedFiles = file.Snapshots.Last().TrackedFiles;
            oldTrackedDirectories = file.Snapshots.Last().TrackedDirectories;
        }

        // Lock UI
        bool fileAlreadyLocked = file.IsProcessing;
        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = true);

        try {
            if (!Repository.IsValid(backupDir)) throw new InvalidOperationException("Git repo not found. Backup location invalid.");

            // Delete tracked files to prep for backup
            await FileOperationsHelper.DeleteTrackedFilesAsync(backupDir, oldTrackedFiles, oldTrackedDirectories, CancellationToken.None);

            switch (fileType) {
                case FileItem.FileTypeEnum.Excel:
                case FileItem.FileTypeEnum.Word:
                case FileItem.FileTypeEnum.Powerpoint:
                    // Copy, strip and extract Microsoft Office files
                    await FileOperationsHelper.CopyFileAsync(sourcePath, workingDir, CancellationToken.None);
                    var tempPath = Path.Combine(workingDir, Path.GetFileName(sourcePath));

                    if (fileType == FileItem.FileTypeEnum.Excel) FileOperationsHelper.SanitizeExcelFile(tempPath, CancellationToken.None);
                    else if (fileType == FileItem.FileTypeEnum.Word) FileOperationsHelper.SanitizeWordFile(tempPath, CancellationToken.None);
                    else if (fileType == FileItem.FileTypeEnum.Powerpoint) FileOperationsHelper.SanitizePowerPointFile(tempPath, CancellationToken.None);

                    var extractPath = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(sourcePath));
                    ZipFile.ExtractToDirectory(tempPath, extractPath);
                    await FileOperationsHelper.CopyDirectoryAsync(extractPath, backupDir, CancellationToken.None);

                    // Track files and dirs
                    newTrackedFiles.AddRange(GetRelativeFiles(extractPath));
                    newTrackedDirectories.AddRange(GetRelativeDirectories(extractPath)); 
                    break;

                default:
                    // Just copy files for others
                    await FileOperationsHelper.CopyFileAsync(sourcePath, backupDir, CancellationToken.None);
                    newTrackedFiles.AddRange([$"{Path.GetFileName(sourcePath)}"]);
                    break;
            }

            using var repo = new Repository(backupDir);

            // Commit snapshot to repo
            Commit commit = GitHelper.StageAndCommit(repo, snapshotTime, newTrackedFiles);

            App.MainDispatcher.TryEnqueue(() => file.AddSnapshot(snapshotTime, commit, newTrackedFiles, newTrackedDirectories));

            var snapshotDetails = new SnapshotDetails(file.Id, snapshotTime, commit, newTrackedFiles, newTrackedDirectories);
            await _stateService.AddSnapshotAsync(snapshotDetails);
        }
        catch (EmptyCommitException) {
            // Only give notification for no changes in case if manual snapshot
            if(mode == SnapshotMode.Manual) App.MainDispatcher.TryEnqueue(() => _notifications.AddNotification(file, "No changes found to snapshot."));
        }
        catch (Exception ex) {
            // Silent mode -> No notification, throw back exception
            if (mode == SnapshotMode.Silent) throw;
            // Auto, Manual snapshot -> All remaining errors result in notification
            else App.MainDispatcher.TryEnqueue(() => _notifications.AddNotification(file, $"Snapshot failed: {ex.Message}"));
        }

        // Unlock UI if it was not locked before starting
        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = fileAlreadyLocked);
        Directory.Delete(workingDir, true);
    }

    // For tracking files
    private static IEnumerable<string> GetRelativeFiles(string basePath) {
        return Directory.GetFiles(basePath, "*", SearchOption.AllDirectories)
                        .Select(f => Path.GetRelativePath(basePath, f));
    }

    // For tracking directories
    private static IEnumerable<string> GetRelativeDirectories(string basePath) {
        return Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories)
                        .Select(d => Path.GetRelativePath(basePath, d));
    }
}