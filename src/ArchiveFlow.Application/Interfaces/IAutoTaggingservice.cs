using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for analyzing file content and automatically assigning metadata tags.
/// </summary>
public interface IAutoTaggingService
{
    /// <summary>
    /// Analyzes the file and applies relevant tags to its metadata.
    /// </summary>
    Task ApplyAutoTagsAsync(FileRecord file);
}