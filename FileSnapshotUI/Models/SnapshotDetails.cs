using System;

namespace FileSnapshotUI.Models;

public class SnapshotDetails(Guid fileId) {
    private readonly Guid _fileId = fileId;
    private readonly DateTime _snapshotTimeUTC = DateTime.UtcNow;

    public Guid FileId {
        get => _fileId;
    }

    public DateTime SnapshotTime {
        get => _snapshotTimeUTC.ToLocalTime();
    }

    public string SnapshotTimeString {
        get => _snapshotTimeUTC.ToLocalTime().ToString("G");
    }
}