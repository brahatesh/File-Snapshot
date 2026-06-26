using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FileSnapshotUI.Models;

public class SnapshotDetails {
    private readonly Guid _fileId;
    private readonly DateTime _snapshotTimeUTC;
    private readonly Commit _commit;
    private readonly ReadOnlyCollection<string> _trackedFiles;

    public SnapshotDetails(Guid fileId, DateTime snapshotTime, Commit commit, IEnumerable<string> trackedFiles) {
        _fileId = fileId;
        if (snapshotTime.Kind == DateTimeKind.Utc) throw new InvalidTimeZoneException("Snapshot TimeZone must be UTC");
        _snapshotTimeUTC = snapshotTime;
        _commit = commit;
        _trackedFiles = new ReadOnlyCollection<string>([.. trackedFiles]);
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

    public ReadOnlyCollection<string> TrackedFiles => _trackedFiles;
}