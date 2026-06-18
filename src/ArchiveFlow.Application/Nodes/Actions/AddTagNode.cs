using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

public class AddTagNode : IArchiveNode
{
    private readonly IMetadataRepository _metadataRepo;
    private readonly string _tagName;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Add Tag: {_tagName}";
    public double X { get; set; }
    public double Y { get; set; }

    public AddTagNode(IMetadataRepository metadataRepo, string tagName)
    {
        _metadataRepo = metadataRepo;
        _tagName = tagName;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var field = await _metadataRepo.GetOrCreateFieldAsync("tag", "Tag");
        if (field == null) return;

        foreach (var file in context.CurrentFileSet.ToList())
        {
            // 檢查是否已存在該 Tag (簡化版：直接插入，實際應用應檢查重複)
            await _metadataRepo.AddMetadataValueAsync(file.Id, field.Id, _tagName);
        }
        // Tag Node 不改變文件集合，只產生副作用 (Side Effect)
    }
}