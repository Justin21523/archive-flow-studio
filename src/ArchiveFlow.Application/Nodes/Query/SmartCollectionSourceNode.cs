using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Source node that loads files based on a saved Smart Collection rule.
/// </summary>
public class SmartCollectionSourceNode : IArchiveNode
{
    private readonly ISmartCollectionRepository _repository;
    private readonly IFileRepository _fileRepository;
    public string CollectionName { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Smart Collection: {CollectionName}";
    public double X { get; set; }
    public double Y { get; set; }

    public SmartCollectionSourceNode(ISmartCollectionRepository repository, IFileRepository fileRepository, string name)
    {
        _repository = repository;
        _fileRepository = fileRepository;
        CollectionName = name;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var collection = await _repository.GetByNameAsync(CollectionName);
        if (collection == null) return;

        var allFiles = await _fileRepository.GetAllAsync(cancellationToken);
        
        // Simple rule parser for MVP (expects JSON like {"Field":"extension", "Value":".pdf"})
        try
        {
            var rule = JsonSerializer.Deserialize<FilterRule>(collection.FilterRuleJson);
            if (rule != null)
            {
                var filtered = allFiles.Where(f => EvaluateRule(f, rule)).ToList();
                context.SetFileSet(filtered);
            }
        }
        catch (Exception)
        {
            // If rule parsing fails, return all files or empty set
            context.SetFileSet(allFiles);
        }
    }

    private bool EvaluateRule(Domain.Entities.FileRecord file, FilterRule rule)
    {
        if (rule.Field.Equals("extension", StringComparison.OrdinalIgnoreCase))
        {
            return file.FileExtension.Equals(rule.Value, StringComparison.OrdinalIgnoreCase);
        }
        if (rule.Field.Equals("size", StringComparison.OrdinalIgnoreCase))
        {
            if (long.TryParse(rule.Value, out long size))
            {
                return file.FileSize > size;
            }
        }
        return true;
    }
}

// Helper DTO for rule parsing
public class FilterRule
{
    public string Field { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}