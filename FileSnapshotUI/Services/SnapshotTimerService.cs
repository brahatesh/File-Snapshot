using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    public partial class SnapshotTimerService(
        BackgroundTaskQueue queue,
        RootViewModel viewModel,
        SnapshotService snapshotService) : BackgroundService {
        private readonly BackgroundTaskQueue _queue = queue;
        private readonly RootViewModel _viewModel = viewModel;
        private readonly SnapshotService _snapshotService = snapshotService;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                var now = DateTime.Now;
                var tcs = new TaskCompletionSource<List<FileItem>>();

                // Safely read the UI collection
                App.MainDispatcher.TryEnqueue(() => {
                    var dueFiles = new List<FileItem>();
                    foreach (var file in _viewModel.Files) {
                        // Using your newly updated property name
                        if (now - file.LastBackup >= file.SnapshotIntervalDuration && file.IsNotProcessing) {
                            dueFiles.Add(file);
                        }
                    }
                    tcs.SetResult(dueFiles);
                });

                var dueFiles = await tcs.Task;

                foreach (var file in dueFiles) {
                    // Here is the magic: We define the task block inline and queue it
                    _queue.EnqueueTask(async (token) => {
                        // 1. DO HEAVY I/O HERE (Background Thread)
                        // File.Copy(file.FullPath, Path.Combine(file.BackupPath, "snapshot.tmp"));
                        await _snapshotService.PerformSnapshotAsync(file, token);


                        // 2. UPDATE UI HERE (UI Thread)
                        //App.MainDispatcher.TryEnqueue(() => {
                        //    file.AddSnapshot(); 
                        //});

                        await default(ValueTask);
                    });
                }
            }
        }
    }
}