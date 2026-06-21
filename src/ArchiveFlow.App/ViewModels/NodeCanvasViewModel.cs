using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Application.Services;
using ArchiveFlow.Domain.Entities;
using ArchiveFlow.Domain.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFileRepository = ArchiveFlow.Application.Interfaces.IFileRepository;
using IMetadataRepository = ArchiveFlow.Application.Interfaces.IMetadataRepository;
using Avalonia.Controls;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// ViewModel for the node workspace.
/// Phase 0.3 adds stable dragging, port-based Bezier connections,
/// edge deletion, node deletion, and connection-order workflow execution.
/// </summary>
public partial class NodeCanvasViewModel : ObservableObject
{
    private readonly NodeRegistry _nodeRegistry;
    private readonly IFileRepository _fileRepository;
    private PortInstanceViewModel? _connectingSourcePort;
    private readonly IMetadataRepository _metadataRepository;
    public ObservableCollection<PendingChangeViewModel> PendingChanges { get; } = new();
    public bool HasPendingChanges => PendingChanges.Count > 0;
    public ObservableCollection<NodeLibraryCategoryViewModel> NodeLibraryCategories { get; } = new();
    public ObservableCollection<NodeInstanceViewModel> Nodes { get; } = new();
    public ObservableCollection<EdgeViewModel> Edges { get; } = new();
    public ObservableCollection<NodeParameterInstanceViewModel> InspectorParameters { get; } = new();
    public ObservableCollection<FileRecord> ResultFiles { get; } = new();

    [ObservableProperty]
    private NodeInstanceViewModel? _selectedNode;

    [ObservableProperty]
    private EdgeViewModel? _selectedEdge;

    [ObservableProperty]
    private string _statusMessage = "Node workspace ready.";
    
    [ObservableProperty]
    private string _inspectorPreviewTitle = "Preview";

    [ObservableProperty]
    private string _inspectorPreviewSummary = "-";

    [ObservableProperty]
    private string _inspectorQueryOrOperationTitle = "Query / Operation";

    [ObservableProperty]
    private string _inspectorQueryOrOperationText = "-";

    [ObservableProperty]
    private string _inspectorWarnings = "-";
    [ObservableProperty]
    private string _inspectorTitle = "No node selected";

    [ObservableProperty]
    private string _inspectorType = "-";

    [ObservableProperty]
    private string _inspectorCategory = "-";

    [ObservableProperty]
    private string _inspectorSubcategory = "-";

    [ObservableProperty]
    private string _inspectorDescription = "Select a node on the canvas to inspect its definition and parameters.";

    [ObservableProperty]
    private string _inspectorMode = "-";

    [ObservableProperty]
    private string _inspectorPorts = "-";

    [ObservableProperty]
    private string _inspectorParameterSummary = "-";

    [ObservableProperty]
    private string _executionLog = "Ready.";

    [ObservableProperty]
    private string _tempConnectionPath = "M 0,0 C 0,0 0,0 0,0";

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _isExecuting;

    public bool HasSelectedNode => SelectedNode != null;
    public bool HasSelectedEdge => SelectedEdge != null;

    public NodeCanvasViewModel(
        NodeRegistry nodeRegistry,
        IFileRepository fileRepository,
        IMetadataRepository metadataRepository)
    {
        _nodeRegistry = nodeRegistry;
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;

        PendingChanges.CollectionChanged += OnPendingChangesCollectionChanged;
        
        BuildNodeLibrary();
        AddStarterNodes();
    }
    
    private void OnPendingChangesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasPendingChanges));
    }

    public void AddNodeFromDefinition(NodeDefinition definition)
    {
        var offset = Nodes.Count * 36;
        var node = new NodeInstanceViewModel(definition, 160 + offset, 160 + offset);

        Nodes.Add(node);
        RecalculateLayout();
        SelectNode(node);

        StatusMessage = $"Added node: {definition.DisplayName}";
    }

    public void SelectNode(NodeInstanceViewModel node)
    {
        ClearSelection();

        SelectedNode = node;
        SelectedNode.IsSelected = true;

        RefreshInspector(node);

        OnPropertyChanged(nameof(HasSelectedNode));
        OnPropertyChanged(nameof(HasSelectedEdge));
    }

    public void SelectEdge(EdgeViewModel edge)
    {
        ClearSelection();

        SelectedEdge = edge;
        SelectedEdge.IsSelected = true;

        StatusMessage = $"Selected connection: {edge.SourceNode.Title} → {edge.TargetNode.Title}";

        OnPropertyChanged(nameof(HasSelectedNode));
        OnPropertyChanged(nameof(HasSelectedEdge));
    }

    public void ClearSelection()
    {
        if (SelectedNode != null)
        {
            SelectedNode.IsSelected = false;
        }

        if (SelectedEdge != null)
        {
            SelectedEdge.IsSelected = false;
        }

        SelectedNode = null;
        SelectedEdge = null;

        ClearInspector();

        OnPropertyChanged(nameof(HasSelectedNode));
        OnPropertyChanged(nameof(HasSelectedEdge));
    }

    public void UpdateNodePosition(NodeInstanceViewModel node, double x, double y)
    {
        node.X = Math.Max(0, x);
        node.Y = Math.Max(0, y);

        RecalculateLayout();
    }

    public void StartConnection(PortInstanceViewModel sourcePort)
    {
        if (sourcePort.IsInput)
        {
            return;
        }

        _connectingSourcePort = sourcePort;
        IsConnecting = true;
        TempConnectionPath = BuildBezierPath(
            sourcePort.AbsoluteX,
            sourcePort.AbsoluteY,
            sourcePort.AbsoluteX + 80,
            sourcePort.AbsoluteY);
    }

    public void UpdateTempConnection(double x, double y)
    {
        if (!IsConnecting || _connectingSourcePort == null)
        {
            return;
        }

        TempConnectionPath = BuildBezierPath(
            _connectingSourcePort.AbsoluteX,
            _connectingSourcePort.AbsoluteY,
            x,
            y);
    }

    public bool TryFinishConnectionAt(double x, double y)
    {
        if (!IsConnecting || _connectingSourcePort == null)
        {
            return false;
        }

        var targetPort = FindInputPortAt(x, y);
        if (targetPort == null)
        {
            CancelConnection();
            return false;
        }

        FinishConnection(targetPort);
        return true;
    }

    public void CancelConnection()
    {
        _connectingSourcePort = null;
        IsConnecting = false;
        TempConnectionPath = "M 0,0 C 0,0 0,0 0,0";
    }

    [RelayCommand]
    private void DeleteSelectedNode()
    {
        if (SelectedNode == null)
        {
            return;
        }

        var node = SelectedNode;
        var title = node.Title;

        var connectedEdges = Edges
            .Where(edge => edge.SourceNode == node || edge.TargetNode == node)
            .ToList();

        foreach (var edge in connectedEdges)
        {
            Edges.Remove(edge);
        }

        Nodes.Remove(node);
        ClearSelection();
        RecalculateLayout();

        StatusMessage = $"Deleted node: {title}";
    }

    [RelayCommand]
    private void DeleteSelectedEdge()
    {
        if (SelectedEdge == null)
        {
            return;
        }

        var source = SelectedEdge.SourceNode.Title;
        var target = SelectedEdge.TargetNode.Title;

        Edges.Remove(SelectedEdge);
        ClearSelection();

        StatusMessage = $"Deleted connection: {source} → {target}";
    }

    [RelayCommand]
    private void ClearCanvas()
    {
        Nodes.Clear();
        Edges.Clear();
        ResultFiles.Clear();
        ClearSelection();

        StatusMessage = "Canvas cleared.";
        ExecutionLog = "Canvas cleared.";
    }

    [RelayCommand]
    private async Task ExecuteWorkflowAsync()
    {
        if (IsExecuting)
        {
            return;
        }

        IsExecuting = true;
        ResultFiles.Clear();
        PendingChanges.Clear();
        ExecutionLog = "Executing workflow preview...\n";

        try
        {
            RecalculateLayout();

            var order = GetTopologicalOrder();

            if (order.Count == 0)
            {
                ExecutionLog += "No nodes to execute.\n";
                return;
            }

            var nodeOutputs = new Dictionary<string, List<FileRecord>>();

            foreach (var node in order)
            {
                var inputFiles = GetInputFilesForNode(node, nodeOutputs);

                node.Status = "Running";
                node.SetRunStats(inputFiles.Count, 0);

                List<FileRecord> outputFiles;

                if (node.Definition.IsActionNode)
                {
                    outputFiles = await PreviewActionNodeAsync(node, inputFiles);
                    node.Status = $"Preview ready ({outputFiles.Count})";
                    ExecutionLog += $"[ACTION PREVIEW] {node.Title}: generated pending changes from {inputFiles.Count} files.\n";
                }
                else
                {
                    outputFiles = await ExecuteNodeAsync(node, inputFiles);
                    node.Status = $"Success ({outputFiles.Count})";
                    ExecutionLog += $"[QUERY] {node.Title}: input {inputFiles.Count}, output {outputFiles.Count}.\n";
                }

                node.SetRunStats(inputFiles.Count, outputFiles.Count);
                node.RefreshPreview();

                if (SelectedNode == node)
                {
                    RefreshInspector(node);
                }

                nodeOutputs[node.InstanceId] = outputFiles;
            }

            var resultNode = order.LastOrDefault(x => x.Definition.Category == NodeCategory.Outputs);
            var finalNode = resultNode ?? order.Last();

            if (nodeOutputs.TryGetValue(finalNode.InstanceId, out var resultFiles))
            {
                foreach (var file in resultFiles)
                {
                    ResultFiles.Add(file);
                }
            }

            ExecutionLog += $"Workflow preview completed. Result files: {ResultFiles.Count}. Pending changes: {PendingChanges.Count}.\n";
            StatusMessage = $"Preview completed. Result files: {ResultFiles.Count}, pending changes: {PendingChanges.Count}";
        }
        catch (Exception ex)
        {
            ExecutionLog += $"Error: {ex.Message}\n";
            StatusMessage = $"Workflow preview failed: {ex.Message}";
        }
        finally
        {
            IsExecuting = false;
        }
    }
    [RelayCommand]
    private async Task ApplyPendingChangesAsync()
    {
        if (PendingChanges.Count == 0)
        {
            StatusMessage = "No pending changes to apply.";
            return;
        }

        var applicableChanges = PendingChanges
            .Where(change => change.CanApply && !change.IsApplied)
            .ToList();

        if (applicableChanges.Count == 0)
        {
            StatusMessage = "No applicable pending changes. Preview-only and skipped changes were not applied.";
            ExecutionLog += "No applicable pending changes.\n";
            return;
        }

        var appliedCount = 0;

        foreach (var change in applicableChanges)
        {
            try
            {
                switch (change.ChangeKind)
                {
                    case "MetadataAddValue":
                        await _metadataRepository.AddMetadataValueIfMissingAsync(
                            change.FileRecord.Id,
                            change.FieldName,
                            change.DisplayName,
                            change.FieldType,
                            change.Category,
                            change.NewValue);
                        change.MarkApplied();
                        appliedCount++;
                        break;

                    case "MetadataSetValue":
                        await _metadataRepository.SetMetadataValueAsync(
                            change.FileRecord.Id,
                            change.FieldName,
                            change.DisplayName,
                            change.FieldType,
                            change.Category,
                            change.NewValue);
                        change.MarkApplied();
                        appliedCount++;
                        break;

                    case "MetadataDeleteValue":
                        await _metadataRepository.DeleteMetadataValueAsync(
                            change.FileRecord.Id,
                            change.FieldName,
                            change.OldValue);
                        change.MarkApplied();
                        appliedCount++;
                        break;

                    case "FileStatusUpdate":
                        if (Enum.TryParse<FileStatus>(change.NewValue, ignoreCase: true, out var newStatus))
                        {
                            change.FileRecord.UpdateStatus(newStatus);
                            await _fileRepository.SaveAsync(change.FileRecord);
                            change.MarkApplied();
                            appliedCount++;
                        }
                        else
                        {
                            change.MarkFailed($"Invalid status: {change.NewValue}");
                        }

                        break;

                    default:
                        change.MarkSkipped("No Apply handler");
                        break;
                }
            }
            catch (Exception ex)
            {
                change.MarkFailed(ex.Message);
                ExecutionLog += $"Apply failed for {change.FileName}: {ex.Message}\n";
            }
        }

        StatusMessage = $"Applied {appliedCount} pending changes.";
        ExecutionLog += $"Applied {appliedCount} pending changes.\n";
        OnPropertyChanged(nameof(HasPendingChanges));
    }

    [RelayCommand]
    private void DiscardPendingChanges()
    {
        var count = PendingChanges.Count;

        PendingChanges.Clear();

        StatusMessage = $"Discarded {count} pending changes.";
        ExecutionLog += $"Discarded {count} pending changes.\n";
    }

    private async Task<List<FileRecord>> PreviewActionNodeAsync(
        NodeInstanceViewModel node,
        List<FileRecord> inputFiles)
    {
        switch (node.NodeType)
        {
            case "metadata.add_tag":
                await PreviewAddTagAsync(node, inputFiles);
                return inputFiles;

            case "metadata.remove_tag":
                await PreviewRemoveTagAsync(node, inputFiles);
                return inputFiles;

            case "metadata.set_subject":
                await PreviewSetMetadataAsync(
                    node,
                    inputFiles,
                    fieldName: "subject",
                    displayName: "Subject",
                    category: "Descriptive Metadata",
                    value: node.GetParameterValue("subject"));
                return inputFiles;

            case "metadata.set_project":
                await PreviewSetMetadataAsync(
                    node,
                    inputFiles,
                    fieldName: "project",
                    displayName: "Project",
                    category: "Personal Knowledge",
                    value: node.GetParameterValue("project"));
                return inputFiles;

            case "metadata.set_reading_status":
                await PreviewSetMetadataAsync(
                    node,
                    inputFiles,
                    fieldName: "reading_status",
                    displayName: "Reading Status",
                    category: "Personal Knowledge",
                    value: node.GetParameterValue("readingStatus", "To Read"));
                return inputFiles;

            case "metadata.set_importance":
                await PreviewSetMetadataAsync(
                    node,
                    inputFiles,
                    fieldName: "importance",
                    displayName: "Importance",
                    category: "Personal Knowledge",
                    value: node.GetParameterValue("importance", "Normal"));
                return inputFiles;

            case "metadata.set_status":
                PreviewSetFileStatus(node, inputFiles);
                return inputFiles;

            case "metadata.generate_archive_id":
                PreviewBlockedAction(
                    node,
                    inputFiles,
                    "Archive ID generation is preview-only in Phase 0.5. Apply will be added after archive ID rules are centralized.");
                return inputFiles;

            case "metadata.validate_metadata":
                PreviewBlockedAction(
                    node,
                    inputFiles,
                    "Metadata validation is analysis-only. It does not write changes.");
                return inputFiles;

            case "file.copy_to_archive":
            case "file.move_file":
            case "file.rename_file":
            case "file.open_file":
            case "file.reveal":
                PreviewBlockedAction(
                    node,
                    inputFiles,
                    "File-system actions are blocked from Apply in Phase 0.5. Physical files will not be modified.");
                return inputFiles;

            case "output.export_csv":
            case "output.export_json":
            case "output.export_dublin_core":
            case "output.smart_collection":
                PreviewBlockedAction(
                    node,
                    inputFiles,
                    "Output actions are preview-only in Phase 0.5. Export Apply will be implemented later.");
                return inputFiles;

            default:
                PreviewBlockedAction(
                    node,
                    inputFiles,
                    "This action node does not have an Apply handler yet.");
                return inputFiles;
        }
    }

    private async Task PreviewAddTagAsync(
        NodeInstanceViewModel node,
        IReadOnlyList<FileRecord> files)
    {
        var tag = node.GetParameterValue("tag", "AI").Trim();

        if (string.IsNullOrWhiteSpace(tag))
        {
            PreviewBlockedAction(node, files, "Tag value is empty.");
            return;
        }

        foreach (var file in files)
        {
            var alreadyExists = await _metadataRepository.HasMetadataValueAsync(
                file.Id,
                "tag",
                tag);

            PendingChanges.Add(new PendingChangeViewModel(
                fileRecord: file,
                sourceNodeTitle: node.Title,
                changeKind: "MetadataAddValue",
                fieldName: "tag",
                displayName: "Tag",
                fieldType: "String",
                category: "Personal Knowledge",
                oldValue: alreadyExists ? tag : string.Empty,
                newValue: tag,
                safetyLevel: "Metadata only",
                canApply: !alreadyExists));

            if (alreadyExists)
            {
                PendingChanges.Last().MarkSkipped("Already has value");
            }
        }
    }

    private async Task PreviewRemoveTagAsync(
        NodeInstanceViewModel node,
        IReadOnlyList<FileRecord> files)
    {
        var tag = node.GetParameterValue("tag").Trim();

        if (string.IsNullOrWhiteSpace(tag))
        {
            PreviewBlockedAction(node, files, "Tag value is empty.");
            return;
        }

        foreach (var file in files)
        {
            var exists = await _metadataRepository.HasMetadataValueAsync(
                file.Id,
                "tag",
                tag);

            PendingChanges.Add(new PendingChangeViewModel(
                fileRecord: file,
                sourceNodeTitle: node.Title,
                changeKind: "MetadataDeleteValue",
                fieldName: "tag",
                displayName: "Tag",
                fieldType: "String",
                category: "Personal Knowledge",
                oldValue: exists ? tag : string.Empty,
                newValue: string.Empty,
                safetyLevel: "Metadata only",
                canApply: exists));

            if (!exists)
            {
                PendingChanges.Last().MarkSkipped("Value not found");
            }
        }
    }

    private async Task PreviewSetMetadataAsync(
        NodeInstanceViewModel node,
        IReadOnlyList<FileRecord> files,
        string fieldName,
        string displayName,
        string category,
        string value)
    {
        value = value.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            PreviewBlockedAction(node, files, $"{displayName} value is empty.");
            return;
        }

        foreach (var file in files)
        {
            var oldValue = await _metadataRepository.GetFirstMetadataValueAsync(
                file.Id,
                fieldName);

            var alreadySame = string.Equals(
                oldValue ?? string.Empty,
                value,
                StringComparison.OrdinalIgnoreCase);

            PendingChanges.Add(new PendingChangeViewModel(
                fileRecord: file,
                sourceNodeTitle: node.Title,
                changeKind: "MetadataSetValue",
                fieldName: fieldName,
                displayName: displayName,
                fieldType: "String",
                category: category,
                oldValue: oldValue ?? string.Empty,
                newValue: value,
                safetyLevel: "Metadata only",
                canApply: !alreadySame));

            if (alreadySame)
            {
                PendingChanges.Last().MarkSkipped("Already has value");
            }
        }
    }

    private void PreviewSetFileStatus(
        NodeInstanceViewModel node,
        IReadOnlyList<FileRecord> files)
    {
        var statusText = node.GetParameterValue("status", "Archived");

        foreach (var file in files)
        {
            var oldStatus = file.GetStatus().ToString();
            var alreadySame = string.Equals(
                oldStatus,
                statusText,
                StringComparison.OrdinalIgnoreCase);

            PendingChanges.Add(new PendingChangeViewModel(
                fileRecord: file,
                sourceNodeTitle: node.Title,
                changeKind: "FileStatusUpdate",
                fieldName: "status",
                displayName: "Status",
                fieldType: "Enum",
                category: "Basic",
                oldValue: oldStatus,
                newValue: statusText,
                safetyLevel: "Database record only",
                canApply: !alreadySame));

            if (alreadySame)
            {
                PendingChanges.Last().MarkSkipped("Already has status");
            }
        }
    }

    private void PreviewBlockedAction(
        NodeInstanceViewModel node,
        IReadOnlyList<FileRecord> files,
        string reason)
    {
        foreach (var file in files)
        {
            PendingChanges.Add(new PendingChangeViewModel(
                fileRecord: file,
                sourceNodeTitle: node.Title,
                changeKind: "BlockedPreviewOnly",
                fieldName: "-",
                displayName: "-",
                fieldType: "-",
                category: "-",
                oldValue: string.Empty,
                newValue: reason,
                safetyLevel: "Blocked",
                canApply: false));

            PendingChanges.Last().MarkSkipped("Preview only");
        }
    }

    public void RecalculateLayout()
    {
        foreach (var node in Nodes)
        {
            if (node.InputPort != null)
            {
                node.InputPort.AbsoluteX = node.X + node.InputPort.RelativeX;
                node.InputPort.AbsoluteY = node.Y + node.InputPort.RelativeY;
            }

            if (node.OutputPort != null)
            {
                node.OutputPort.AbsoluteX = node.X + node.OutputPort.RelativeX;
                node.OutputPort.AbsoluteY = node.Y + node.OutputPort.RelativeY;
            }
        }

        foreach (var edge in Edges)
        {
            edge.UpdatePath();
        }
    }

    private void BuildNodeLibrary()
    {
        NodeLibraryCategories.Clear();

        var categoryGroups = _nodeRegistry.GetAll()
            .GroupBy(x => x.Category)
            .OrderBy(x => x.Key.ToString());

        foreach (var categoryGroup in categoryGroups)
        {
            NodeLibraryCategories.Add(new NodeLibraryCategoryViewModel
            {
                Name = FormatCategoryName(categoryGroup.Key),
                Subcategories = categoryGroup
                    .GroupBy(x => x.Subcategory)
                    .OrderBy(x => x.Key)
                    .Select(subcategory => new NodeLibrarySubcategoryViewModel
                    {
                        Name = subcategory.Key,
                        Nodes = subcategory
                            .OrderBy(x => x.DisplayName)
                            .Select(x => new NodeLibraryItemViewModel(x))
                            .ToList()
                    })
                    .ToList()
            });
        }
    }

    private void AddStarterNodes()
    {
        var allFiles = _nodeRegistry.FindByType("source.all_files");
        var resultTable = _nodeRegistry.FindByType("output.result_table");

        if (allFiles == null || resultTable == null)
        {
            StatusMessage = "Starter workflow could not be created because core definitions are missing.";
            return;
        }

        var sourceNode = new NodeInstanceViewModel(allFiles, 180, 220);
        var resultNode = new NodeInstanceViewModel(resultTable, 560, 220);

        Nodes.Add(sourceNode);
        Nodes.Add(resultNode);

        RecalculateLayout();

        if (sourceNode.OutputPort != null && resultNode.InputPort != null)
        {
            Edges.Add(new EdgeViewModel(sourceNode.OutputPort, resultNode.InputPort));
        }

        RecalculateLayout();
        SelectNode(sourceNode);
    }

    private void RefreshInspector(NodeInstanceViewModel node)
    {
        InspectorParameters.Clear();

        foreach (var parameter in node.Parameters)
        {
            parameter.ValueChanged -= OnSelectedNodeParameterChanged;
            parameter.ValueChanged += OnSelectedNodeParameterChanged;

            InspectorParameters.Add(parameter);
        }

        InspectorTitle = node.Title;
        InspectorType = node.NodeType;
        InspectorCategory = node.Category;
        InspectorSubcategory = node.Subcategory;
        InspectorDescription = node.Description;
        InspectorMode = node.ModeText;
        InspectorPorts = $"Input ports: {node.InputCount}, Output ports: {node.OutputCount}";
        InspectorParameterSummary = node.ParameterSummary;

        InspectorPreviewTitle = node.Definition.IsActionNode ? "Action Preview" : "Query Preview";
        InspectorPreviewSummary = node.PreviewSummary;

        InspectorQueryOrOperationTitle = node.Definition.IsActionNode ? "Operation" : "SQL-like Expression";
        InspectorQueryOrOperationText = node.OperationPreview;

        InspectorWarnings = BuildInspectorWarnings(node);
    }

    private void OnSelectedNodeParameterChanged(object? sender, EventArgs e)
    {
        if (SelectedNode == null)
        {
            return;
        }

        SelectedNode.RefreshPreview();
        RefreshInspector(SelectedNode);
    }

    private void ClearInspector()
    {
        InspectorParameters.Clear();

        InspectorTitle = "No node selected";
        InspectorType = "-";
        InspectorCategory = "-";
        InspectorSubcategory = "-";
        InspectorDescription = "Select a node on the canvas to inspect its definition and parameters.";
        InspectorMode = "-";
        InspectorPorts = "-";
        InspectorParameterSummary = "-";
        InspectorPreviewTitle = "Preview";
        InspectorPreviewSummary = "-";
        InspectorQueryOrOperationTitle = "Query / Operation";
        InspectorQueryOrOperationText = "-";
        InspectorWarnings = "-";
    }

    private string BuildInspectorWarnings(NodeInstanceViewModel node)
    {
        var warnings = new List<string>();

        if (node.InputPort != null && Edges.All(edge => edge.TargetNode != node))
        {
            warnings.Add("No input connection.");
        }

        if (node.OutputPort != null && Edges.All(edge => edge.SourceNode != node))
        {
            warnings.Add("No output connection.");
        }

        if (!string.IsNullOrWhiteSpace(node.WarningSummary) && node.WarningSummary != "No warnings.")
        {
            warnings.Add(node.WarningSummary);
        }

        if (warnings.Count == 0)
        {
            return "No warnings.";
        }

        return string.Join("\n", warnings.Distinct());
    }

    private PortInstanceViewModel? FindInputPortAt(double x, double y)
    {
        const double hitRadius = 18;

        return Nodes
            .Select(node => node.InputPort)
            .Where(port => port != null)
            .Cast<PortInstanceViewModel>()
            .FirstOrDefault(port =>
            {
                var dx = port.AbsoluteX - x;
                var dy = port.AbsoluteY - y;
                return Math.Sqrt(dx * dx + dy * dy) <= hitRadius;
            });
    }

    private void FinishConnection(PortInstanceViewModel targetPort)
    {
        if (_connectingSourcePort == null)
        {
            CancelConnection();
            return;
        }

        if (!targetPort.IsInput)
        {
            CancelConnection();
            return;
        }

        if (_connectingSourcePort.ParentNode == targetPort.ParentNode)
        {
            StatusMessage = "Cannot connect a node to itself.";
            CancelConnection();
            return;
        }

        if (!ArePortsCompatible(_connectingSourcePort, targetPort))
        {
            StatusMessage = $"Port type mismatch: {_connectingSourcePort.DataType} → {targetPort.DataType}";
            CancelConnection();
            return;
        }

        var duplicate = Edges.Any(edge =>
            edge.SourcePort == _connectingSourcePort &&
            edge.TargetPort == targetPort);

        if (duplicate)
        {
            StatusMessage = "Connection already exists.";
            CancelConnection();
            return;
        }

        var newEdge = new EdgeViewModel(_connectingSourcePort, targetPort);
        Edges.Add(newEdge);
        newEdge.UpdatePath();

        SelectEdge(newEdge);
        CancelConnection();

        StatusMessage = $"Connected: {newEdge.SourceNode.Title} → {newEdge.TargetNode.Title}";
    }

    private static bool ArePortsCompatible(PortInstanceViewModel source, PortInstanceViewModel target)
    {
        return source.DataType == target.DataType ||
               source.DataType == NodePortDataType.Any ||
               target.DataType == NodePortDataType.Any;
    }

    private List<NodeInstanceViewModel> GetTopologicalOrder()
    {
        if (Nodes.Count == 0)
        {
            return new List<NodeInstanceViewModel>();
        }

        if (Edges.Count == 0)
        {
            return Nodes.OrderBy(x => x.X).ThenBy(x => x.Y).ToList();
        }

        var inDegree = Nodes.ToDictionary(node => node.InstanceId, _ => 0);
        var adjacency = Nodes.ToDictionary(node => node.InstanceId, _ => new List<string>());

        foreach (var edge in Edges)
        {
            adjacency[edge.SourceNode.InstanceId].Add(edge.TargetNode.InstanceId);
            inDegree[edge.TargetNode.InstanceId]++;
        }

        var queue = new Queue<string>(
            Nodes
                .Where(node => inDegree[node.InstanceId] == 0)
                .OrderBy(node => node.X)
                .ThenBy(node => node.Y)
                .Select(node => node.InstanceId));

        var sortedIds = new List<string>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            sortedIds.Add(current);

            foreach (var next in adjacency[current])
            {
                inDegree[next]--;

                if (inDegree[next] == 0)
                {
                    queue.Enqueue(next);
                }
            }
        }

        if (sortedIds.Count != Nodes.Count)
        {
            throw new InvalidOperationException("Workflow contains a cycle. Please remove circular connections.");
        }

        return sortedIds
            .Select(id => Nodes.First(node => node.InstanceId == id))
            .ToList();
    }

    private List<FileRecord> GetInputFilesForNode(
        NodeInstanceViewModel node,
        Dictionary<string, List<FileRecord>> nodeOutputs)
    {
        var incomingEdges = Edges
            .Where(edge => edge.TargetNode == node)
            .ToList();

        if (incomingEdges.Count == 0)
        {
            return new List<FileRecord>();
        }

        return incomingEdges
            .SelectMany(edge =>
                nodeOutputs.TryGetValue(edge.SourceNode.InstanceId, out var output)
                    ? output
                    : new List<FileRecord>())
            .GroupBy(file => file.Id)
            .Select(group => group.First())
            .ToList();
    }

    private async Task<List<FileRecord>> ExecuteNodeAsync(
        NodeInstanceViewModel node,
        List<FileRecord> inputFiles)
    {
        var type = node.NodeType;

        if (type == "source.all_files")
        {
            return (await _fileRepository.GetAllAsync()).ToList();
        }

        if (type == "source.recent_imports")
        {
            return (await _fileRepository.GetAllAsync())
                .OrderByDescending(file => file.ImportedAt)
                .Take(100)
                .ToList();
        }

        if (type == "source.folder_source")
        {
            var folderPath = node.GetParameterValue("folderPath");

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return (await _fileRepository.GetAllAsync()).ToList();
            }

            return (await _fileRepository.GetAllAsync())
                .Where(file => file.FilePath.Contains(folderPath, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (type == "filter.extension")
        {
            var extension = NormalizeExtension(node.GetParameterValue("extension", ".pdf"));

            return inputFiles
                .Where(file => NormalizeExtension(file.FileExtension) == extension)
                .ToList();
        }

        if (type == "filter.file_type")
        {
            var fileType = node.GetParameterValue("fileType", "Document");

            return inputFiles
                .Where(file => IsFileTypeMatch(file, fileType))
                .ToList();
        }

        if (type == "filter.path_contains")
        {
            var pathText = node.GetParameterValue("pathText");

            if (string.IsNullOrWhiteSpace(pathText))
            {
                return inputFiles;
            }

            return inputFiles
                .Where(file => file.FilePath.Contains(pathText, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (type == "filter.status")
        {
            var status = node.GetParameterValue("status", "New");

            return inputFiles
                .Where(file => StatusMatches(file, status))
                .ToList();
        }

        if (type == "filter.size")
        {
            var minText = node.GetParameterValue("minSizeMb", "0");
            var maxText = node.GetParameterValue("maxSizeMb", "100");

            _ = double.TryParse(minText, out var minMb);
            _ = double.TryParse(maxText, out var maxMb);

            var minBytes = minMb * 1024 * 1024;
            var maxBytes = maxMb * 1024 * 1024;

            return inputFiles
                .Where(file => file.FileSize >= minBytes && file.FileSize <= maxBytes)
                .ToList();
        }

        if (type == "filter.date_range")
        {
            var startText = node.GetParameterValue("startDate");
            var endText = node.GetParameterValue("endDate");

            var hasStart = DateTime.TryParse(startText, out var startDate);
            var hasEnd = DateTime.TryParse(endText, out var endDate);

            return inputFiles
                .Where(file =>
                {
                    var date = file.ImportedAt;

                    if (hasStart && date < startDate)
                    {
                        return false;
                    }

                    if (hasEnd && date > endDate)
                    {
                        return false;
                    }

                    return true;
                })
                .ToList();
        }

        if (type == "search.keyword" || type == "search.filename")
        {
            var query = type == "search.filename"
                ? node.GetParameterValue("filename")
                : node.GetParameterValue("query");

            if (string.IsNullOrWhiteSpace(query))
            {
                return inputFiles;
            }

            return inputFiles
                .Where(file =>
                    file.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.ContentPreview.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (type == "logic.limit")
        {
            var countText = node.GetParameterValue("count", "100");
            var count = int.TryParse(countText, out var parsedCount) ? parsedCount : 100;

            return inputFiles.Take(Math.Max(0, count)).ToList();
        }

        if (type == "logic.sort_by")
        {
            var field = node.GetParameterValue("field", "Imported Date");
            var direction = node.GetParameterValue("direction", "Descending");
            var descending = direction.Equals("Descending", StringComparison.OrdinalIgnoreCase);

            return SortFiles(inputFiles, field, descending);
        }

        if (node.Definition.Category == NodeCategory.Outputs)
        {
            return inputFiles;
        }

        if (node.Definition.IsActionNode)
        {
            return inputFiles;
        }

        return inputFiles;
    }

    private static string BuildBezierPath(double x1, double y1, double x2, double y2)
    {
        var distance = Math.Abs(x2 - x1);
        var controlOffset = Math.Max(80, distance * 0.5);

        var cx1 = x1 + controlOffset;
        var cy1 = y1;
        var cx2 = x2 - controlOffset;
        var cy2 = y2;

        return $"M {x1},{y1} C {cx1},{cy1} {cx2},{cy2} {x2},{y2}";
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        extension = extension.Trim().ToLowerInvariant();

        return extension.StartsWith(".")
            ? extension
            : $".{extension}";
    }

    private static bool IsFileTypeMatch(FileRecord file, string fileType)
    {
        var extension = NormalizeExtension(file.FileExtension);

        return fileType.ToLowerInvariant() switch
        {
            "document" => extension is ".pdf" or ".doc" or ".docx" or ".txt" or ".md" or ".rtf",
            "image" => extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".tiff" or ".bmp",
            "video" => extension is ".mp4" or ".mov" or ".mkv" or ".avi" or ".webm",
            "audio" => extension is ".mp3" or ".wav" or ".flac" or ".ogg" or ".m4a",
            "code" => extension is ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".h" or ".html" or ".css",
            "3d model" => extension is ".blend" or ".fbx" or ".obj" or ".glb" or ".gltf" or ".stl",
            "archive" => extension is ".zip" or ".7z" or ".rar" or ".tar" or ".gz",
            _ => true
        };
    }

    private static bool StatusMatches(FileRecord file, string desiredStatus)
    {
        var actual = file.Status.ToString();

        if (actual.Equals(desiredStatus, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Enum.TryParse<FileStatus>(desiredStatus, ignoreCase: true, out var parsed))
        {
            return actual == ((int)parsed).ToString() ||
                   actual.Equals(parsed.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static List<FileRecord> SortFiles(
        List<FileRecord> files,
        string field,
        bool descending)
    {
        IOrderedEnumerable<FileRecord> ordered = field.ToLowerInvariant() switch
        {
            "filename" => descending
                ? files.OrderByDescending(file => file.FileName)
                : files.OrderBy(file => file.FileName),

            "size" => descending
                ? files.OrderByDescending(file => file.FileSize)
                : files.OrderBy(file => file.FileSize),

            "modified date" => descending
                ? files.OrderByDescending(file => file.ModifiedAt)
                : files.OrderBy(file => file.ModifiedAt),

            "status" => descending
                ? files.OrderByDescending(file => file.Status)
                : files.OrderBy(file => file.Status),

            _ => descending
                ? files.OrderByDescending(file => file.ImportedAt)
                : files.OrderBy(file => file.ImportedAt)
        };

        return ordered.ToList();
    }

    private static string FormatCategoryName(NodeCategory category)
    {
        return category switch
        {
            NodeCategory.Sources => "Sources",
            NodeCategory.QueryFilters => "Query Filters",
            NodeCategory.Search => "Search",
            NodeCategory.LogicAndSetOperations => "Logic & Set Operations",
            NodeCategory.MetadataActions => "Metadata Actions",
            NodeCategory.FileActions => "File Actions",
            NodeCategory.CreateAndTemplate => "Create / Template",
            NodeCategory.Relationships => "Relationships",
            NodeCategory.IndexingAndExtraction => "Indexing & Extraction",
            NodeCategory.Outputs => "Outputs",
            _ => category.ToString()
        };
    }

    partial void OnSelectedNodeChanged(NodeInstanceViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedNode));
    }

    partial void OnSelectedEdgeChanged(EdgeViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedEdge));
    }
}
