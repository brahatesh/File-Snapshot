using System;
using System.IO;

namespace FileSnapshotUI.Helpers;

public static class AppEnvironment {
    public static string WorkingDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileSnapshot",
            "WorkingDir");

    public static string GetTempFolder() {
        return Path.Combine(WorkingDirectory, Guid.NewGuid().ToString("N"));
    }
}