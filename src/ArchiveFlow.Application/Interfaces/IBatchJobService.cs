using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for managing and executing background batch jobs.
/// Prevents UI freezing during heavy file processing tasks.
/// </summary>
public interface IBatchJobService
{
    /// <summary>
    /// Enqueues a new background job for execution.
    /// </summary>
    /// <param name="jobName">Display name of the job.</param>
    /// <param name="jobAction">The asynchronous task to execute.</param>
    void EnqueueJob(string jobName, Func<CancellationToken, IProgress<string>, Task> jobAction);

    /// <summary>
    /// Event triggered when a job starts.
    /// </summary>
    event Action<string>? OnJobStarted;

    /// <summary>
    /// Event triggered when a job reports progress.
    /// </summary>
    event Action<string, string>? OnJobProgress;

    /// <summary>
    /// Event triggered when a job completes.
    /// </summary>
    event Action<string>? OnJobCompleted;
}