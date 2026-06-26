using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FileSnapshotUI.Helpers;

public static class GitHelper {
    private static readonly string authorName = Environment.UserName;
    private static readonly string authorEmail = Environment.UserDomainName;
    private static readonly string committerName = "File Snapshot App";
    private static readonly string committerEmail = "@filesnapshot";
    
    public static Commit StageAndCommit(Repository repo, DateTime time, IEnumerable<string> files) {
        //Commands.Stage(repo, "*");
        foreach(var file in files) {
            repo.Index.Add(file);
        }
        repo.Index.Write();

        Signature author = new(authorName, authorEmail, time);
        Signature committer = new(committerName, committerEmail, time);

        Commit commit = repo.Commit($"{time:G}", author, committer);

        return commit;
    }

    public static void ResetToCommit(Repository repo, Commit commit) => repo.Reset(ResetMode.Hard, commit);
}