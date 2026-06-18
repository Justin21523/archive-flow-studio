using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service for performing full-text searches across the archive.
/// Supports searching file names, paths, content, and metadata.
/// </summary>
public interface IFullTextSearchService
{
    /// <summary>
    /// Performs a full-text search and returns matching files.
    /// </summary>
    /// <param name="query">The search query string</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    Task<IEnumerable<FileRecord>> SearchAsync(string query, int maxResults = 100);

    /// <summary>
    /// Performs an advanced search with multiple filters.
    /// </summary>
    Task<IEnumerable<FileRecord>> AdvancedSearchAsync(
        string? keyword = null,
        string? fileType = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int maxResults = 100);

    /// <summary>
    /// Rebuilds the full-text search index from scratch.
    /// </summary>
    Task RebuildIndexAsync();
}