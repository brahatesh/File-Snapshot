using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace FileSnapshotUI.Models;

/// <summary>
/// Represents the details of a specific snapshot, including the associated Git commit 
/// and the set of files/directories captured at that time.
/// </summary>
public class SnapshotDetails {
    private readonly Guid _fileId;
    private readonly DateTime _snapshotTimeUTC;
    private readonly Commit _commit;
    private readonly ReadOnlyCollection<string> _trackedFiles;
    private readonly ReadOnlyCollection<string> _trackedDirectories;

    public SnapshotDetails(Guid fileId, DateTime snapshotTime, Commit commit, IEnumerable<string> trackedFiles, IEnumerable<string> trackedDirectories) {
        _fileId = fileId;
        if (snapshotTime.Kind == DateTimeKind.Utc) throw new InvalidTimeZoneException("Snapshot TimeZone must be UTC");
        _snapshotTimeUTC = snapshotTime;
        _commit = commit;
        _trackedFiles = new ReadOnlyCollection<string>([.. trackedFiles]);
        _trackedDirectories = new ReadOnlyCollection<string>([.. trackedDirectories]);
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
    public ReadOnlyCollection<string> TrackedDirectories => _trackedDirectories;
}