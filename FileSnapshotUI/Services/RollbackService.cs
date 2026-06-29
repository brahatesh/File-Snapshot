using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using LibGit2Sharp;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services;

/// <summary>
/// Provides services to roll back a tracked file to the state captured in a 
/// specific historical snapshot.
/// </summary>
public class RollbackService() {
    /// <summary>
    /// Performs a rollback of the tracked file to the state defined in the provided <see cref="SnapshotDetails"/>.
    /// </summary>
    /// <param name="file">The <see cref="FileItem"/> to be rolled back.</param>
    /// <param name="workingDir">The working directory where the file should be restored.</param>
    /// <param name="snapshot">The <see cref="SnapshotDetails"/> defining the target state.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task representing the asynchronous rollback operation.</returns>
    public static async Task RollbackRepo(FileItem file, string workingDir, SnapshotDetails snapshot, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        var repoPath = file.BackupPath;
        if (!Repository.IsValid(repoPath)) return;

        // Store last commit to go back after retrieve state during snapshot
        var latestCommit = file.Snapshots.Last().Commit;

        // If snapshot is not for the current file, return
        if (!file.Snapshots.Contains(snapshot)) return;
        
        // Roll back to commit
        var repo = new Repository(repoPath);
        GitHelper.ResetToCommit(repo, snapshot.Commit);

        // Copy files to temp folder
        await FileOperationsHelper.CopyTrackedFilesAsync(repoPath, workingDir, snapshot.TrackedFiles, token);

        string tempFilePath = Path.Combine(workingDir, file.FileName);

        // Build file back if Microsoft office file
        switch(file.FileType) {
            case FileItem.FileTypeEnum.Excel:
            case FileItem.FileTypeEnum.Word:
            case FileItem.FileTypeEnum.Powerpoint:
                ZipFile.CreateFromDirectory(workingDir, tempFilePath);
                break;
        }

        // Go back to current commit
        GitHelper.ResetToCommit(repo, latestCommit);
    }
}