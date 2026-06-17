using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ArchiveFlow.Application.Nodes;
using ArchiveFlow.Application.Nodes.Query;
using ArchiveFlow.Application.Workflows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly WorkflowEngine _workflowEngine;
    private readonly Application.Interfaces.IFileRepository _fileRepository;
    private readonly ILogger<NodeCanvasViewModel> _logger;

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();

    [ObservableProperty]
    private string _executionLog = "Ready to execute workflow.";

    [ObservableProperty]
    private bool _isExecuting;

    public NodeCanvasViewModel(
        WorkflowEngine workflowEngine, 
        Application.Interfaces.IFileRepository fileRepository,
        ILogger<NodeCanvasViewModel> logger)
    {
        _workflowEngine = workflowEngine;
        _fileRepository = fileRepository;
        _logger = logger;

        InitializeDefaultNodes();
    }

    private void InitializeDefaultNodes()
    {
        // Create a simple linear workflow: All Files -> Filter .txt -> Filter .md
        Nodes.Add(new NodeViewModel("node_all", "All Files", 50, 50));
        Nodes.Add(new NodeViewModel("node_filter_txt", "Filter: .txt", 300, 50));
        Nodes.Add(new NodeViewModel("node_filter_md", "Filter: .md", 550, 50));
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        if (IsExecuting) return;

        IsExecuting = true;
        ExecutionLog = "Starting workflow execution...\n";
        
        // Update UI status
        foreach (var node in Nodes) node.Status = "Running";

        try
        {
            // 1. Instantiate the actual backend nodes
            var allFilesNode = new AllFilesNode(_fileRepository) { X = 50, Y = 50 };
            var filterTxtNode = new FileTypeFilterNode(".txt") { X = 300, Y = 50 };
            var filterMdNode = new FileTypeFilterNode(".md") { X = 550, Y = 50 };

            // Note: In a real app, the WorkflowEngine would execute them in sequence.
            // Here we simulate the flow to show how context changes.
            
            var context = new NodeExecutionContext();

            // Execute Node 1
            UpdateNodeStatus("node_all", "Running");
            await allFilesNode.ExecuteAsync(context);
            UpdateNodeStatus("node_all", $"Success ({context.CurrentFileSet.Count} files)");
            ExecutionLog += $"[All Files] Found {context.CurrentFileSet.Count} files.\n";

            // Execute Node 2
            UpdateNodeStatus("node_filter_txt", "Running");
            await filterTxtNode.ExecuteAsync(context);
            UpdateNodeStatus("node_filter_txt", $"Success ({context.CurrentFileSet.Count} files)");
            ExecutionLog += $"[Filter .txt] Filtered to {context.CurrentFileSet.Count} files.\n";

            // Execute Node 3
            UpdateNodeStatus("node_filter_md", "Running");
            await filterMdNode.ExecuteAsync(context);
            UpdateNodeStatus("node_filter_md", $"Success ({context.CurrentFileSet.Count} files)");
            ExecutionLog += $"[Filter .md] Filtered to {context.CurrentFileSet.Count} files.\n";

            ExecutionLog += "\nWorkflow completed successfully!";
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Workflow execution failed.");
            ExecutionLog += $"\nError: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }

    private void UpdateNodeStatus(string nodeId, string status)
    {
        var nodeVm = System.Linq.Enumerable.FirstOrDefault(Nodes, n => n.NodeId == nodeId);
        if (nodeVm != null)
        {
            nodeVm.Status = status;
        }
    }
}