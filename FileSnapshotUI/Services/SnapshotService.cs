using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Compression;
using LibGit2Sharp;

namespace FileSnapshotUI.Services;

public class SnapshotService {
    private readonly NotificationService _notifications;

    public SnapshotService(NotificationService notifications) {
        _notifications = notifications;
    }

    public async Task PerformSnapshotAsync(FileItem file, CancellationToken token) {
        var workingDir = AppEnvironment.GetTempFolder();
        Directory.CreateDirectory(workingDir);
        string backupDir = file.BackupPath;
        string sourcePath = file.FullPath;
        var fileType = file.FileType;
        DateTime snapshotTime = DateTime.Now;

        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = true);

        try {
            if (!Repository.IsValid(backupDir)) throw new InvalidOperationException("Git repo not found. Backup location invalid.");

            await FileOperationsHelper.DeleteDirectoryASync(backupDir, token, false);

            switch (fileType) {
                case FileItem.FileTypeEnum.Excel:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, workingDir, token);
                    var tempPath = Path.Combine(workingDir, Path.GetFileName(sourcePath));
                    FileOperationsHelper.SanitizeExcelFile(tempPath, token);

                    var extractPath = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(sourcePath));
                    ZipFile.ExtractToDirectory(tempPath, extractPath);
                    await FileOperationsHelper.CopyDirectoryAsync(extractPath, backupDir, token);
                    break;

                case FileItem.FileTypeEnum.Word:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, workingDir, token);
                    tempPath = Path.Combine(workingDir, Path.GetFileName(sourcePath));
                    FileOperationsHelper.SanitizeWordFile(tempPath, token);

                    extractPath = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(sourcePath));
                    ZipFile.ExtractToDirectory(tempPath, extractPath);
                    await FileOperationsHelper.CopyDirectoryAsync(extractPath, backupDir, token);
                    break;

                case FileItem.FileTypeEnum.Powerpoint:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, workingDir, token);
                    tempPath = Path.Combine(workingDir, Path.GetFileName(sourcePath));
                    FileOperationsHelper.SanitizePowerPointFile(tempPath, token);

                    extractPath = Path.Combine(workingDir, Path.GetFileNameWithoutExtension(sourcePath));
                    ZipFile.ExtractToDirectory(tempPath, extractPath);
                    await FileOperationsHelper.CopyDirectoryAsync(extractPath, backupDir, token);
                    break;

                default:
                    await FileOperationsHelper.CopyFileAsync(sourcePath, backupDir, token);
                    break;
            }

            using var repo = new Repository(backupDir);
            Commands.Stage(repo, "*");

            Signature author = new Signature(Environment.UserName, Environment.UserDomainName, snapshotTime);
            Signature committer = new Signature("File Snapshot App", "@filesnapshot", snapshotTime);

            Commit commit = repo.Commit($"{snapshotTime:G}", author, committer);

            App.MainDispatcher.TryEnqueue(() => file.AddSnapshot(snapshotTime, commit));
        }
        catch(Exception ex) {
            App.MainDispatcher.TryEnqueue(() => _notifications.AddNotification(file, $"Snapshot failed: {ex.Message}"));
        }

        App.MainDispatcher.TryEnqueue(() => file.IsProcessing = false);
        Directory.Delete(workingDir, true);
    }
}