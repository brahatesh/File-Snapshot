using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace FileSnapshotUI.Services {
    public class BackgroundTaskQueue {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue =
            Channel.CreateUnbounded<Func<CancellationToken, ValueTask>>();

        public void EnqueueTask(Func<CancellationToken, ValueTask> workItem) {
            ArgumentNullException.ThrowIfNull(workItem);
            _queue.Writer.TryWrite(workItem);
        }

        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken) {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}