using FileSnapshotUI.Data;
using FileSnapshotUI.Models;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services;

public interface IStateService {
    Task<IEnumerable<FileItem>> LoadTrackedFilesAsync();
    Task AddFileItemAsync(FileItem file);
    Task AddSnapshotAsync(SnapshotDetails snapshot);
    Task RemoveFileItemAsync(FileItem file);
    Task RemoveSnapshotAsync(SnapshotDetails snapshot);
    Task UpdateFileItemAsync(FileItem file);
}

public class SqliteStateService : IStateService {
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public SqliteStateService(IDbContextFactory<AppDbContext> dbContextFactory) {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IEnumerable<FileItem>> LoadTrackedFilesAsync() {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync(); // Ensure DB exists

        var entities = await context.FileItems
            .Include(f => f.Snapshots)
            .ToListAsync();

        var fileItems = new List<FileItem>();

        foreach (var entity in entities) {
            var fileItem = new FileItem(entity.Id, entity.FullPath, entity.BackupPath) {
                LastBackup = entity.LastBackup,
                SnapshotIntervalDuration = entity.SnapshotIntervalDuration
            };

            using var repo = new Repository(entity.BackupPath);
            foreach (var snapEntity in entity.Snapshots) {
                var commit = repo.Lookup<Commit>(snapEntity.CommitSha);
                var trackedFiles = JsonSerializer.Deserialize<List<string>>(snapEntity.TrackedFilesJson) ?? [];
                var trackedDirs = JsonSerializer.Deserialize<List<string>>(snapEntity.TrackedDirectoriesJson) ?? [];

                fileItem.AddSnapshot(snapEntity.SnapshotTime.ToLocalTime(), commit, trackedFiles, trackedDirs);
            }

            fileItems.Add(fileItem);
        }

        return fileItems;
    }

    public async Task AddFileItemAsync(FileItem file) {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var entity = new FileItemEntity {
            Id = file.Id,
            FullPath = file.FullPath,
            BackupPath = file.BackupPath,
            LastBackup = file.LastBackup.ToUniversalTime(),
            SnapshotIntervalDuration = file.SnapshotIntervalDuration
        };

        context.FileItems.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task AddSnapshotAsync(SnapshotDetails snapshot) {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var entity = new SnapshotEntity {
            FileId = snapshot.FileId,
            SnapshotTime = snapshot.SnapshotTime.ToUniversalTime(),
            CommitSha = snapshot.Commit.Sha,
            TrackedFilesJson = JsonSerializer.Serialize(snapshot.TrackedFiles),
            TrackedDirectoriesJson = JsonSerializer.Serialize(snapshot.TrackedDirectories)
        };

        context.Snapshots.Add(entity);

        // Update the LastBackupUTC on the parent FileItemEntity
        var fileItem = await context.FileItems.FindAsync(snapshot.FileId);
        if (fileItem != null) {
            fileItem.LastBackup = snapshot.SnapshotTime;
        }

        await context.SaveChangesAsync();
    }

    public async Task RemoveFileItemAsync(FileItem file) {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        // Include the snapshots so we can safely delete the children first 
        // to avoid any SQLite foreign key constraint errors.
        var entity = await context.FileItems
            .Include(f => f.Snapshots)
            .FirstOrDefaultAsync(f => f.Id == file.Id);

        if (entity != null) {
            context.Snapshots.RemoveRange(entity.Snapshots);
            context.FileItems.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task RemoveSnapshotAsync(SnapshotDetails snapshot) {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        // Find the specific snapshot using the File ID and Git SHA
        var entity = await context.Snapshots
            .FirstOrDefaultAsync(s => s.FileId == snapshot.FileId && s.CommitSha == snapshot.Commit.Sha);

        if (entity != null) {
            context.Snapshots.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task UpdateFileItemAsync(FileItem file) {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var entity = await context.FileItems.FindAsync(file.Id);

        if (entity != null) {
            // Update the database columns with the new values
            entity.FullPath = file.FullPath;
            entity.BackupPath = file.BackupPath;
            entity.LastBackup = file.LastBackup;
            entity.SnapshotIntervalDuration = file.SnapshotIntervalDuration;

            await context.SaveChangesAsync();
        }
    }
}