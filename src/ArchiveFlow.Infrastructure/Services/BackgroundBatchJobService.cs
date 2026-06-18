using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Implements a background worker queue using ConcurrentQueue and a dedicated Task.
/// Ensures heavy operations do not block the main UI thread.
/// </summary>
public class BackgroundBatchJobService : IBatchJobService, IDisposable
{
    private readonly ConcurrentQueue<BatchJobRequest> _jobQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _workerTask;
    private readonly ILogger<BackgroundBatchJobService> _logger;
    private bool _isProcessing;

    public event Action<string>? OnJobStarted;
    public event Action<string, string>? OnJobProgress;
    public event Action<string>? OnJobCompleted;

    public BackgroundBatchJobService(ILogger<BackgroundBatchJobService> logger)
    {
        _logger = logger;
        StartWorker();
    }

    private void StartWorker()
    {
        _workerTask = Task.Run(async () =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (_jobQueue.TryDequeue(out var jobAction))
                {
                    _isProcessing = true;
                    
                    try
                    {
                        OnJobStarted?.Invoke(jobAction.JobName);
                        
                        var progress = new Progress<string>(msg => OnJobProgress?.Invoke(jobAction.JobName, msg));
                        await jobAction.Action(_cts.Token, progress);
                        
                        OnJobCompleted?.Invoke(jobAction.JobName);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Batch job was canceled.");
                        OnJobCompleted?.Invoke($"{jobAction.JobName} (Canceled)");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Batch job failed.");
                        OnJobCompleted?.Invoke($"{jobAction.JobName} (Failed: {ex.Message})");
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                }
                else
                {
                    // Prevent busy waiting
                    await Task.Delay(100, _cts.Token);
                }
            }
        }, _cts.Token);
    }

    public void EnqueueJob(string jobName, Func<CancellationToken, IProgress<string>, Task> jobAction)
    {
        _jobQueue.Enqueue(new BatchJobRequest(jobName, jobAction));
        _logger.LogInformation("Enqueued batch job: {JobName}. Processing active: {IsProcessing}", jobName, _isProcessing);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _workerTask?.Wait();
        _cts.Dispose();
    }

    private sealed record BatchJobRequest(
        string JobName,
        Func<CancellationToken, IProgress<string>, Task> Action);
}
