using LibGit2Sharp;
using System;

namespace FileSnapshotUI.Models;

public class SnapshotDetails {
    private readonly Guid _fileId;
    private readonly DateTime _snapshotTimeUTC;
    private readonly Commit _commit;

    public SnapshotDetails(Guid fileId, DateTime snapshotTime, Commit commit) {
        _fileId = fileId;
        if (snapshotTime.Kind == DateTimeKind.Utc) throw new InvalidTimeZoneException("Snapshot TimeZone must be UTC");
        _snapshotTimeUTC = snapshotTime;
        _commit = commit;
    }

    public Guid FileId {
        get => _fileId;
    }

    public DateTime SnapshotTime {
        get => _snapshotTimeUTC.ToLocalTime();
    }

    public string SnapshotTimeString {
        get => _snapshotTimeUTC.ToLocalTime().ToString("G");
    }

    public Commit Commit {
        get => _commit;
    }
}