using LibGit2Sharp;
using System;
using System.Collections;
using System.Collections.Generic;

namespace FileSnapshotUI.Helpers;

/// <summary>
/// Provides utility methods to interact with Git repositories using <c>LibGit2Sharp</c>
/// </summary>
public static class GitHelper {
    private static readonly string authorName = Environment.UserName;
    private static readonly string authorEmail = Environment.UserDomainName;
    private static readonly string committerName = "File Snapshot App";
    private static readonly string committerEmail = "@filesnapshot";

    /// <summary>
    /// Stages a specific set of files and creates a new commit in the repository.
    /// </summary>
    /// <param name="repo">The <see cref="Repository"/> instance to be updated.</param>
    /// <param name="time">The timestamp to apply to both the author and committer signatures.</param>
    /// <param name="files">An <see cref="IEnumerable{T}"/> of relative file paths to stage.</param>
    /// <returns>The newly created <see cref="Commit"/>.</returns>
    /// <remarks>
    /// Creates a commit with the content author as the user of the computer and
    /// commit author as the application
    /// </remarks>
    public static Commit StageAndCommit(Repository repo, DateTime time, IEnumerable<string> files) {
        // Only add tracked files
        foreach(var file in files) {
            repo.Index.Add(file);
        }
        repo.Index.Write();

        Signature author = new(authorName, authorEmail, time);
        Signature committer = new(committerName, committerEmail, time);

        Commit commit = repo.Commit($"{time:G}", author, committer);

        return commit;
    }

    /// <summary>
    /// Reverts the repository state to a previous commit using a hard reset.
    /// </summary>
    /// <param name="repo">The <see cref="Repository"/> instance to reset.</param>
    /// <param name="commit">The target <see cref="Commit"/> to which the repository should be reset.</param>
    public static void ResetToCommit(Repository repo, Commit commit) => repo.Reset(ResetMode.Hard, commit);
}