using System;

namespace FileSnapshotUI.Models;

public class SnapshotDetails {
    private Guid _fileId;
    private DateTime _snapshotTimeUTC;

    public SnapshotDetails(Guid fileId) {
        _fileId = fileId;
        _snapshotTimeUTC = DateTime.UtcNow;
    }

    public Guid FileId {
        get => _fileId;
    }

    public DateTime snapshotTime {
        get => _snapshotTimeUTC.ToLocalTime();
    }

    public string snapshotTimeString {
        get => _snapshotTimeUTC.ToLocalTime().ToString("G");
    }
}