using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace FileSnapshotUI.Data;

public class AppDbContext : DbContext {
    public DbSet<FileItemEntity> FileItems { get; set; }
    public DbSet<SnapshotEntity> Snapshots { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        if (!optionsBuilder.IsConfigured) {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dbPath = Path.Combine(appData, "FileSnapshot", "filesnapshots.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        // This explicitly tells EF Core: "The Snapshots list uses the FileId column as its foreign key."
        // It prevents EF from creating the phantom 'FileItemEntityId' column.
        modelBuilder.Entity<FileItemEntity>()
            .HasMany(f => f.Snapshots)
            .WithOne()
            .HasForeignKey(s => s.FileId);

        base.OnModelCreating(modelBuilder);
    }
}