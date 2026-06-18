using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

public class FullTextSearchNode : IArchiveNode
{
    private readonly ISearchService _searchService;
    public string Keyword { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Search: {Keyword}";
    public double X { get; set; }
    public double Y { get; set; }

    public FullTextSearchNode(ISearchService searchService, string keyword)
    {
        _searchService = searchService;
        Keyword = keyword;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var results = await _searchService.SearchAsync(Keyword);
        context.SetFileSet(results);
    }
}