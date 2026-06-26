using FileSnapshotUI.Helpers;
using FileSnapshotUI.Models;
using LibGit2Sharp;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services;

public class RollbackService() {
    public static async Task RollbackRepo(FileItem file, string workingDir, SnapshotDetails snapshot, CancellationToken token) {
        token.ThrowIfCancellationRequested();

        var repoPath = file.BackupPath;
        if (!Repository.IsValid(repoPath)) return;
        var latestCommit = file.Snapshots.Last().Commit;

        if (!file.Snapshots.Contains(snapshot)) return;
        
        var repo = new Repository(repoPath);
        GitHelper.ResetToCommit(repo, snapshot.Commit);

        await FileOperationsHelper.CopyTrackedFilesAsync(repoPath, workingDir, snapshot.TrackedFiles, token);

        string tempFilePath = Path.Combine(workingDir, file.FileName);

        switch(file.FileType) {
            case FileItem.FileTypeEnum.Excel:
            case FileItem.FileTypeEnum.Word:
            case FileItem.FileTypeEnum.Powerpoint:
                ZipFile.CreateFromDirectory(workingDir, tempFilePath);
                break;
        }

        GitHelper.ResetToCommit(repo, latestCommit);
    }
}