using Microsoft.Extensions.Configuration;

namespace Sedgegration.Requests;

public class RequestPersistenceWorker(
    IRequestQueue queue,
    IRequestStore store,
    ILogger<RequestPersistenceWorker> logger,
    IConfiguration config)
    : BackgroundService
{
    private readonly int _maxParallelism = Math.Max(1, config.GetValue<int>("RequestPersistence:Parallelism", 4));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Request persistence worker starting (max parallelism={Max})", _maxParallelism);

        var reader = queue.Reader;
        var semaphore = new SemaphoreSlim(_maxParallelism);
        var running = new List<Task>();

        try
        {
            await foreach (var req in reader.ReadAllAsync(stoppingToken))
            {
                await semaphore.WaitAsync(stoppingToken);

                // Start the persist task (fire-and-forget tracked in running list)
                var task = PersistAsync(req, semaphore, stoppingToken);

                lock (running)
                {
                    running.Add(task);
                    // Clean up completed tasks to avoid memory growth
                    running.RemoveAll(t => t.IsCompleted);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error in persistence worker main loop");
        }

        // Wait for outstanding persistence tasks to complete
        Task[] outstanding;
        lock (running) { outstanding = running.ToArray(); }
        if (outstanding.Length > 0)
        {
            logger.LogInformation("Waiting for {Count} outstanding persistence tasks to complete", outstanding.Length);
            try { await Task.WhenAll(outstanding); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while waiting for outstanding persistence tasks");
            }
        }

        logger.LogInformation("Request persistence worker stopping");
    }

    private async Task PersistAsync(PersistedRequest req, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            await store.SaveAsync(req);
            logger.LogDebug("Persisted request {RequestId}", req.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to persist request {RequestId}", req.Id);
        }
        finally
        {
            try { semaphore.Release(); } catch { }
        }
    }
}
