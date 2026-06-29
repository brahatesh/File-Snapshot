using System;

namespace FileSnapshotUI.Models;

/// <summary>
/// Represents a notification related to a specific file snapshot event.
/// </summary>
public class Notification {
    private Guid _id = Guid.NewGuid();

    public Guid Id {
        get => _id;
    }
    public Guid FileItemId { get; }
    public FileItem FileItem { get; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; }

    public DateTime CreatedAtLocal {
        get {
            return CreatedAt.ToLocalTime();
        }
    }

    public Notification(FileItem fileItem, string message) {
        FileItem = fileItem ?? throw new ArgumentNullException(nameof(fileItem));
        FileItemId = fileItem.Id;
        Message = message;
        CreatedAt = DateTime.UtcNow;
    }
}