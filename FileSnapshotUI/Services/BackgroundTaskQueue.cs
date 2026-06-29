using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    /// <summary>
    /// Provides a thread-safe, asynchronous queue for managing background work items.
    /// </summary>
    public class BackgroundTaskQueue {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue =
            Channel.CreateUnbounded<Func<CancellationToken, ValueTask>>();

        /// <summary>
        /// Enqueues a new background task to be processed.
        /// </summary>
        /// <param name="workItem">A function delegate representing the task to be executed.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="workItem"/> is null.</exception>
        public void EnqueueTask(Func<CancellationToken, ValueTask> workItem) {
            ArgumentNullException.ThrowIfNull(workItem);
            _queue.Writer.TryWrite(workItem);
        }

        /// <summary>
        /// Asynchronously retrieves the next available task from the queue.
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A <see cref="ValueTask"/> containing the work item to be executed.</returns>
        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken) {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}