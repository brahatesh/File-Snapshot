using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSnapshotUI.Data;

public class FileItemEntity {
    [Key]
    public Guid Id { get; set; }
    public string FullPath { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public DateTime LastBackup { get; set; }
    public TimeSpan SnapshotIntervalDuration { get; set; }

    public List<SnapshotEntity> Snapshots { get; set; } = new();
}

public class SnapshotEntity {
    [Key]
    public int Id { get; set; }

    public Guid FileId { get; set; }

    public DateTime SnapshotTime { get; set; }
    public string CommitSha { get; set; } = string.Empty;
    public string TrackedFilesJson { get; set; } = "[]";
    public string TrackedDirectoriesJson { get; set; } = "[]";
}