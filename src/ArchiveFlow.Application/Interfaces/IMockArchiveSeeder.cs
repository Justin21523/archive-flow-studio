using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Development-only service for resetting and generating realistic mock archive data.
/// </summary>
public interface IMockArchiveSeeder
{
    Task<MockArchiveSeedResult> ResetAndGenerateAsync(
        int fileCount = 420,
        CancellationToken cancellationToken = default);
}