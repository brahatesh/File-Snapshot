using FileSnapshotUI.Models;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services;

public class SnapshotService : BackgroundService {
    private readonly FileItem file;

    public SnapshotService(FileItem file) {
        this.file = file;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        throw new System.NotImplementedException();
    }
}