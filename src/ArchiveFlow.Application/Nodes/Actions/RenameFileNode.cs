using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that renames files by adding a prefix and/or suffix.
/// Supports Preview mode to prevent accidental data loss.
/// </summary>
public class RenameFileNode : IArchiveNode
{
    private readonly IFileOperationService _fileOperationService;
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Rename: {Prefix}***{Suffix}";
    public double X { get; set; }
    public double Y { get; set; }

    public RenameFileNode(IFileOperationService fileOperationService, string prefix, string suffix)
    {
        _fileOperationService = fileOperationService;
        Prefix = prefix;
        Suffix = suffix;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.IsPreviewMode)
        {
            var previews = await _fileOperationService.PreviewRenameAsync(context.CurrentFileSet, Prefix, Suffix);
            foreach (var p in previews)
            {
                if (p.IsValid)
                    context.PreviewMessages.Add($"[Preview Rename] {p.FileName} -> {System.IO.Path.GetFileName(p.NewPath)}");
                else
                    context.PreviewMessages.Add($"[Preview Rename] {p.FileName} -> ERROR: {p.ErrorMessage}");
            }
        }
        else
        {
            await _fileOperationService.ExecuteRenameAsync(context.CurrentFileSet, Prefix, Suffix);
            context.PreviewMessages.Add($"[Applied] Renamed {context.CurrentFileSet.Count} files.");
        }
    }
}