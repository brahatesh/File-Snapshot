using FileSnapshotUI.Models;
using FileSnapshotUI.ViewModels;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    /// <summary>
    /// A background service that periodically checks for files due for an automatic snapshot 
    /// and orchestrates their processing.
    /// </summary>
    /// <remarks>
    /// This service inherits from <see cref="BackgroundService"/> and runs a <see cref="PeriodicTimer"/> 
    /// that triggers every minute to evaluate the state of tracked files.
    /// </remarks>
    public partial class SnapshotTimerService(
        BackgroundTaskQueue queue,
        RootViewModel viewModel,
        SnapshotService snapshotService) : BackgroundService {
        private readonly BackgroundTaskQueue _queue = queue;
        private readonly RootViewModel _viewModel = viewModel;
        private readonly SnapshotService _snapshotService = snapshotService;

        /// <summary>
        /// Executes the background timer loop to monitor snapshot intervals.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> to signal service shutdown.</param>
        /// <returns>A task representing the background execution loop.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            // Timer of 1 min
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                var now = DateTime.Now;
                var tcs = new TaskCompletionSource<List<FileItem>>();

                App.MainDispatcher.TryEnqueue(() => {
                    var dueFiles = new List<FileItem>();
                    foreach (var file in _viewModel.Files) {
                        // Check if file is due
                        if (now - file.LastBackup >= file.SnapshotIntervalDuration && file.IsNotProcessing) {
                            dueFiles.Add(file);
                        }
                    }
                    tcs.SetResult(dueFiles);
                });

                var dueFiles = await tcs.Task;

                foreach (var file in dueFiles) {
                    // Add snapshot task to queue for all files that are due
                    _queue.EnqueueTask(async (token) => {
                        await _snapshotService.PerformSnapshotAsync(file, SnapshotMode.Automatic, token);

                        await default(ValueTask);
                    });
                }
            }
        }
    }
}