using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that creates a relationship between all files in the current context 
/// and a specified target file (defined by parameter).
/// </summary>
public class CreateRelationshipNode : IArchiveNode
{
    private readonly IRelationshipRepository _relationshipRepository;
    public string TargetArchiveId { get; set; } = string.Empty;
    public string RelationType { get; set; } = "References";

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Link to {TargetArchiveId}";
    public double X { get; set; }
    public double Y { get; set; }

    public CreateRelationshipNode(IRelationshipRepository relationshipRepository, string targetArchiveId, string relationType)
    {
        _relationshipRepository = relationshipRepository;
        TargetArchiveId = targetArchiveId;
        RelationType = relationType;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // In a real scenario, we would look up the target file's internal ID using the ArchiveId.
        // For this implementation, we assume the parameter passed is the internal FileRecord.Id (GUID string).
        if (string.IsNullOrWhiteSpace(TargetArchiveId)) return;

        foreach (var file in context.CurrentFileSet.ToList())
        {
            if (file.Id != TargetArchiveId)
            {
                await _relationshipRepository.CreateRelationshipAsync(file.Id, TargetArchiveId, RelationType);
            }
        }
    }
}