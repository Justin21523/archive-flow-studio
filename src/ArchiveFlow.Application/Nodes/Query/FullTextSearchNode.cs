using System;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Query node that performs full-text search across the archive.
/// </summary>
public class FullTextSearchNode : IArchiveNode
{
    private readonly IFullTextSearchService _searchService;

    public string SearchQuery { get; set; } = string.Empty;
    public int MaxResults { get; set; } = 100;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Full-Text Search: {SearchQuery}";
    public double X { get; set; }
    public double Y { get; set; }

    public FullTextSearchNode(IFullTextSearchService searchService, string searchQuery, int maxResults = 100)
    {
        _searchService = searchService;
        SearchQuery = searchQuery;
        MaxResults = maxResults;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            context.SetFileSet(Array.Empty<Domain.Entities.FileRecord>());
            return;
        }

        var results = await _searchService.SearchAsync(SearchQuery, MaxResults);
        context.SetFileSet(results);
    }
}
