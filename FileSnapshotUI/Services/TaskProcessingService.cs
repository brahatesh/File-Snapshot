using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    public partial class TaskProcessingService(BackgroundTaskQueue taskQueue) : BackgroundService {
        private readonly BackgroundTaskQueue _taskQueue = taskQueue;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    // Wait for a task to be queued
                    var workItem = await _taskQueue.DequeueAsync(stoppingToken);

                    // Execute the function that was passed in
                    await workItem(stoppingToken);
                }
                catch (OperationCanceledException) {
                    // App is shutting down, exit gracefully
                }
                catch (Exception ex) {
                    // TODO: Log the error. 
                    // Catching this ensures one failed task doesn't crash the whole background thread!
                    System.Diagnostics.Debug.WriteLine($"Task failed: {ex.Message}");
                }
            }
        }
    }
}