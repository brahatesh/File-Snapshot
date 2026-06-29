using System;
using System.IO;

namespace FileSnapshotUI.Helpers;

/// <summary>
/// Provides static access to application-wide environmental settings, 
/// including directory paths and temporary file management.
/// </summary>
public static class AppEnvironment {
    /// <summary>
    /// Gets the base working directory path located in the user's LocalApplicationData folder.
    /// </summary>
    /// <remarks>
    /// The path is structured as: %LocalAppData%\FileSnapshot\WorkingDir
    /// </remarks>
    public static string WorkingDirectory { get; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileSnapshot",
            "WorkingDir");

    /// <summary>
    /// Generates a unique temporary folder path within the <see cref="WorkingDirectory"/>.
    /// </summary>
    /// <returns>A string representing the full path to a new, unique temporary directory.</returns>
    public static string GetTempFolder() {
        return Path.Combine(WorkingDirectory, Guid.NewGuid().ToString("N"));
    }
}