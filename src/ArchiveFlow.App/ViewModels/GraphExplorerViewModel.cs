using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// ViewModel for the Knowledge Graph Explorer view.
/// </summary>
public partial class GraphExplorerViewModel : ObservableObject
{
    private readonly IFileRepository _fileRepository;
    private readonly IRelationshipRepository _relationshipRepository;
    private readonly ILogger<GraphExplorerViewModel> _logger;

    public ObservableCollection<FileRecord> AvailableFiles { get; } = new();
    public ObservableCollection<GraphNodeViewModel> GraphNodes { get; } = new();
    public ObservableCollection<GraphEdgeViewModel> GraphEdges { get; } = new();

    [ObservableProperty]
    private FileRecord? _selectedFile;

    public GraphExplorerViewModel(
        IFileRepository fileRepository,
        IRelationshipRepository relationshipRepository,
        ILogger<GraphExplorerViewModel> logger)
    {
        _fileRepository = fileRepository;
        _relationshipRepository = relationshipRepository;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        AvailableFiles.Clear();
        var files = await _fileRepository.GetAllAsync();
        foreach (var file in files)
        {
            AvailableFiles.Add(file);
        }
    }

    partial void OnSelectedFileChanged(FileRecord? value)
    {
        if (value != null)
        {
            Task.Run(async () => await BuildGraphAsync(value.Id));
        }
    }

    private async Task BuildGraphAsync(string centerFileId)
    {
        GraphNodes.Clear();
        GraphEdges.Clear();

        var relationships = await _relationshipRepository.GetRelationshipsByFileIdAsync(centerFileId);
        var allFiles = await _fileRepository.GetAllAsync();

        // 1. Add Center Node
        var centerFile = allFiles.FirstOrDefault(f => f.Id == centerFileId);
        if (centerFile == null) return;

        var centerNode = new GraphNodeViewModel(centerFile.Id, centerFile.FileName, 400, 300, true);
        GraphNodes.Add(centerNode);

        // 2. Add Neighbor Nodes (1-hop)
        var relatedIds = relationships.Select(r => r.SourceFileId == centerFileId ? r.TargetFileId : r.SourceFileId).Distinct().ToList();
        var neighborFiles = allFiles.Where(f => relatedIds.Contains(f.Id)).ToList();

        int count = neighborFiles.Count;
        if (count == 0) return;

        double radius = 200;
        for (int i = 0; i < count; i++)
        {
            double angle = 2 * Math.PI * i / count;
            double x = 400 + radius * Math.Cos(angle);
            double y = 300 + radius * Math.Sin(angle);

            var file = neighborFiles[i];
            GraphNodes.Add(new GraphNodeViewModel(file.Id, file.FileName, x, y, false));
        }

        // 3. Add Edges
        foreach (var rel in relationships)
        {
            var sourceNode = GraphNodes.FirstOrDefault(n => n.FileId == rel.SourceFileId);
            var targetNode = GraphNodes.FirstOrDefault(n => n.FileId == rel.TargetFileId);

            if (sourceNode != null && targetNode != null)
            {
                GraphEdges.Add(new GraphEdgeViewModel(sourceNode, targetNode, rel.RelationshipType));
            }
        }
    }
}

public partial class GraphNodeViewModel : ObservableObject
{
    public string FileId { get; }
    
    [ObservableProperty] private string _label = string.Empty;
    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private bool _isCenter;

    public GraphNodeViewModel(string fileId, string label, double x, double y, bool isCenter)
    {
        FileId = fileId;
        Label = label.Length > 15 ? label.Substring(0, 15) + "..." : label;
        X = x;
        Y = y;
        IsCenter = isCenter;
    }
}

public partial class GraphEdgeViewModel : ObservableObject
{
    public GraphNodeViewModel Source { get; }
    public GraphNodeViewModel Target { get; }
    
    [ObservableProperty] private string _relationType = string.Empty;
    [ObservableProperty] private string _pathData = string.Empty; // 改為 PathData

    public GraphEdgeViewModel(GraphNodeViewModel source, GraphNodeViewModel target, string relationType)
    {
        Source = source;
        Target = target;
        RelationType = relationType;
        
        // 計算節點中心點 (假設節點大小 120x40)
        double x1 = source.X + 60;
        double y1 = source.Y + 20;
        double x2 = target.X + 60;
        double y2 = target.Y + 20;

        // 計算貝茲曲線控制點
        double dx = Math.Abs(x2 - x1) * 0.5;
        double cx1 = x1 + dx;
        double cy1 = y1;
        double cx2 = x2 - dx;
        double cy2 = y2;

        PathData = $"M {x1},{y1} C {cx1},{cy1} {cx2},{cy2} {x2},{y2}";
    }
}