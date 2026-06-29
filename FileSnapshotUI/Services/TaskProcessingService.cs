using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    /// <summary>
    /// A background worker service that continuously monitors the <see cref="BackgroundTaskQueue"/> 
    /// and executes queued work items.
    /// </summary>
    /// <remarks>
    /// This service runs as a <see cref="BackgroundService"/>, providing a long-running 
    /// execution loop that processes tasks until the application shuts down.
    /// </remarks>
    public partial class TaskProcessingService(BackgroundTaskQueue taskQueue) : BackgroundService {
        private readonly BackgroundTaskQueue _taskQueue = taskQueue;

        /// <summary>
        /// Starts the background execution loop to process tasks from the queue.
        /// </summary>
        /// <param name="stoppingToken">A <see cref="CancellationToken"/> to signal service shutdown.</param>
        /// <returns>A task representing the background execution loop.</returns>
        /// <remarks>
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
                    System.Diagnostics.Debug.WriteLine($"Task failed: {ex.Message}");
                }
            }
        }
    }
}