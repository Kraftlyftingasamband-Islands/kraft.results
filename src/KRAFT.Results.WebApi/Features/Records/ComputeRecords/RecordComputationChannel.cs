using System.Threading.Channels;

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed class RecordComputationChannel
{
    private const int Capacity = 1000;

    private readonly Channel<int> _channel = Channel.CreateBounded<int>(
        new BoundedChannelOptions(Capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

    private readonly object _lock = new();
    private int _pendingCount;
    private TaskCompletionSource _drainedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    internal async Task WriteAsync(int attemptId, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _pendingCount++;

            if (_drainedTcs.Task.IsCompleted)
            {
                _drainedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        await _channel.Writer.WriteAsync(attemptId, cancellationToken);
    }

    internal IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    internal void SignalItemCompleted()
    {
        lock (_lock)
        {
            if (--_pendingCount == 0)
            {
                _drainedTcs.TrySetResult();
            }
        }
    }

    internal async Task WaitUntilDrainedAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs;

        lock (_lock)
        {
            if (_pendingCount == 0)
            {
                return;
            }

            tcs = _drainedTcs;
        }

        await tcs.Task.WaitAsync(cancellationToken);
    }
}