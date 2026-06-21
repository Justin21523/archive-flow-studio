using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

public class SetSubjectNode : IArchiveNode
{
    private readonly IMetadataRepository _metadataRepo;
    private readonly string _subjectName;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Set Subject: {_subjectName}";
    public double X { get; set; }
    public double Y { get; set; }

    public SetSubjectNode(IMetadataRepository metadataRepo, string subjectName)
    {
        _metadataRepo = metadataRepo;
        _subjectName = subjectName;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var field = await _metadataRepo.GetOrCreateFieldAsync(
            "subject",
            "Subject",
            "String",
            "Descriptive",
            isRequired: true,
            cancellationToken: cancellationToken);
        if (field == null) return;

        foreach (var file in context.CurrentFileSet.ToList())
        {
            // 簡化版：直接插入。實際應用可能需要先刪除舊的 Subject 再插入新的。
            await _metadataRepo.AddMetadataValueAsync(file.Id, field.Id, _subjectName);
        }
    }
}
