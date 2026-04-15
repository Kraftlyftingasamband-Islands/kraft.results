namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed class RecordComputationWorker(
    RecordComputationChannel channel,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RecordComputationWorker> logger) : BackgroundService
{
    private readonly RecordComputationChannel _channel = channel;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly ILogger<RecordComputationWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Record computation worker started");

        await foreach (int attemptId in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation(
                    "Processing record computation for attempt {AttemptId}",
                    attemptId);

                using IServiceScope scope = _serviceScopeFactory.CreateScope();
                RecordComputationService service =
                    scope.ServiceProvider.GetRequiredService<RecordComputationService>();

                await service.ComputeRecordsAsync(attemptId, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // Record computation failure must not crash the worker
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to compute records for attempt {AttemptId}",
                    attemptId);
            }
#pragma warning restore CA1031
            finally
            {
                _channel.SignalItemCompleted();
            }
        }

        _logger.LogInformation("Record computation worker stopped");
    }
}