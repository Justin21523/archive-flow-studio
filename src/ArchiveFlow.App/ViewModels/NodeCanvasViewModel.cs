using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Nodes;
using ArchiveFlow.Application.Nodes.Actions;
using ArchiveFlow.Application.Nodes.Query;
using ArchiveFlow.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly Application.Interfaces.IFileRepository _fileRepository;
    private readonly ILogger<NodeCanvasViewModel> _logger;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<ArchiveFlow.Domain.Entities.FileRecord> ResultFiles { get; } = new();

    [ObservableProperty]
    private string _executionLog = "Ready.";

    [ObservableProperty]
    private string _previewMessage = string.Empty;

    [ObservableProperty]
    private bool _isExecuting;

    // For drag & drop
    [ObservableProperty]
    private NodeViewModel? _draggedNode;

    public NodeCanvasViewModel(
        Application.Interfaces.IFileRepository fileRepository,
        ILogger<NodeCanvasViewModel> logger)
    {
        _fileRepository = fileRepository;
        _logger = logger;
        InitializeDefaultDag();
    }

    private void InitializeDefaultDag()
    {
        var allNode = new NodeViewModel("All Files", 50, 100);
        var filterNode = new NodeViewModel("Filter: .txt", 300, 50);
        var actionNode = new NodeViewModel("Set Status: Archived", 550, 100);

        Nodes.Add(allNode);
        Nodes.Add(filterNode);
        Nodes.Add(actionNode);

        // Create edges
        var edge1 = new EdgeViewModel(allNode, filterNode);
        var edge2 = new EdgeViewModel(filterNode, actionNode);
        
        Edges.Add(edge1);
        Edges.Add(edge2);
        allNode.OutputEdges.Add(edge1);
        filterNode.OutputEdges.Add(edge2);
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        if (IsExecuting) return;
        IsExecuting = true;
        ExecutionLog = "Executing DAG...\n";
        ResultFiles.Clear();

        try
        {
            // 1. Sort nodes by X coordinate to determine execution order (Left to Right)
            var sortedNodes = Nodes.OrderBy(n => n.X).ToList();
            var context = new NodeExecutionContext();

            foreach (var nodeVm in sortedNodes)
            {
                nodeVm.Status = "Running";
                IArchiveNode node = CreateBackendNode(nodeVm);
                
                await node.ExecuteAsync(context);
                
                nodeVm.Status = $"Success ({context.CurrentFileSet.Count})";
                ExecutionLog += $"[{nodeVm.Title}] Processed. Count: {context.CurrentFileSet.Count}\n";
            }

            // 2. Populate Result Table
            foreach (var file in context.CurrentFileSet)
            {
                ResultFiles.Add(file);
            }

            ExecutionLog += "Workflow completed.\n";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workflow failed");
            ExecutionLog += $"Error: {ex.Message}\n";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    [RelayCommand]
    private async Task PreviewActionAsync()
    {
        ExecutionLog = "Generating Preview...\n";
        var context = new NodeExecutionContext();
        
        // Execute query nodes first to get the file set
        foreach (var nodeVm in Nodes.OrderBy(n => n.X).Where(n => !n.Title.StartsWith("Set Status")))
        {
            var node = CreateBackendNode(nodeVm);
            await node.ExecuteAsync(context);
        }

        // Find the action node
        var actionNodeVm = Nodes.FirstOrDefault(n => n.Title.StartsWith("Set Status"));
        if (actionNodeVm != null)
        {
            var actionNode = (IActionNode)CreateBackendNode(actionNodeVm);
            var preview = await actionNode.PreviewAsync(context);
            PreviewMessage = $"PREVIEW: {preview.Description}";
            ExecutionLog += $"Preview generated: {preview.AffectedFileCount} files affected.\n";
        }
    }

    [RelayCommand]
    private async Task ApplyActionAsync()
    {
        ExecutionLog = "Applying changes...\n";
        var context = new NodeExecutionContext();
        
        foreach (var nodeVm in Nodes.OrderBy(n => n.X).Where(n => !n.Title.StartsWith("Set Status")))
        {
            var node = CreateBackendNode(nodeVm);
            await node.ExecuteAsync(context);
        }

        var actionNodeVm = Nodes.FirstOrDefault(n => n.Title.StartsWith("Set Status"));
        if (actionNodeVm != null)
        {
            var actionNode = (IActionNode)CreateBackendNode(actionNodeVm);
            await actionNode.ApplyAsync(context);
            PreviewMessage = "APPLIED: Changes saved to memory.";
            ExecutionLog += "Changes applied successfully.\n";
            
            // Update result table
            ResultFiles.Clear();
            foreach (var file in context.CurrentFileSet) ResultFiles.Add(file);
        }
    }

    private IArchiveNode CreateBackendNode(NodeViewModel vm)
    {
        if (vm.Title == "All Files") return new AllFilesNode(_fileRepository) { X = vm.X, Y = vm.Y };
        if (vm.Title.StartsWith("Filter:")) 
        {
            var ext = vm.Title.Split(':')[1].Trim();
            return new FileTypeFilterNode(ext) { X = vm.X, Y = vm.Y };
        }
        if (vm.Title.StartsWith("Set Status:"))
        {
            return new UpdateStatusNode(FileStatus.Archived) { X = vm.X, Y = vm.Y };
        }
        throw new InvalidOperationException($"Unknown node type: {vm.Title}");
    }

    // Drag & Drop handlers
    public void StartDrag(NodeViewModel node) => DraggedNode = node;
    
    public void MoveDrag(double deltaX, double deltaY)
    {
        if (DraggedNode != null)
        {
            DraggedNode.X += deltaX;
            DraggedNode.Y += deltaY;
        }
    }

    public void EndDrag() => DraggedNode = null;
}