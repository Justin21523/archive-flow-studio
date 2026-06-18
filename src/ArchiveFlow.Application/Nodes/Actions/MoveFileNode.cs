using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that moves files to a specified target directory.
/// Supports Preview mode.
/// </summary>
public class MoveFileNode : IArchiveNode
{
    private readonly IFileOperationService _fileOperationService;
    public string TargetDirectory { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Move to: {TargetDirectory}";
    public double X { get; set; }
    public double Y { get; set; }

    public MoveFileNode(IFileOperationService fileOperationService, string targetDirectory)
    {
        _fileOperationService = fileOperationService;
        TargetDirectory = targetDirectory;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (context.IsPreviewMode)
        {
            var previews = await _fileOperationService.PreviewMoveAsync(context.CurrentFileSet, TargetDirectory);
            foreach (var p in previews)
            {
                if (p.IsValid)
                    context.PreviewMessages.Add($"[Preview Move] {p.FileName} -> {p.NewPath}");
                else
                    context.PreviewMessages.Add($"[Preview Move] {p.FileName} -> ERROR: {p.ErrorMessage}");
            }
        }
        else
        {
            await _fileOperationService.ExecuteMoveAsync(context.CurrentFileSet, TargetDirectory);
            context.PreviewMessages.Add($"[Applied] Moved {context.CurrentFileSet.Count} files to {TargetDirectory}.");
        }
    }
}