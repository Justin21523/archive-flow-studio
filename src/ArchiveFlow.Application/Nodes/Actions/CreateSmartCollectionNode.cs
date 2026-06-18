using System;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that saves the current workflow's filter logic as a Smart Collection.
/// Note: For MVP, we assume the ParameterValue contains a simple JSON rule.
/// </summary>
public class CreateSmartCollectionNode : IArchiveNode
{
    private readonly ISmartCollectionRepository _repository;
    public string CollectionName { get; set; } = string.Empty;
    public string RuleJson { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Create Smart Collection: {CollectionName}";
    public double X { get; set; }
    public double Y { get; set; }

    public CreateSmartCollectionNode(ISmartCollectionRepository repository, string name, string ruleJson)
    {
        _repository = repository;
        CollectionName = name;
        RuleJson = ruleJson;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(CollectionName)) return;

        var collection = new SmartCollection
        {
            Name = CollectionName,
            FilterRuleJson = RuleJson,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.CreateAsync(collection);
        
        // Smart Collection creation is a side-effect, it doesn't change the file set flow
    }
}