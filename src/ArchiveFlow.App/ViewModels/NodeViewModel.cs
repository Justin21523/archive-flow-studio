using System;
using System.Collections.ObjectModel;
using System.Linq;
using ArchiveFlow.Application.Nodes.Definitions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents an instance of a node on the canvas, driven by a NodeDefinition.
/// </summary>
public partial class NodeViewModel : ObservableObject
{
    public Guid Id { get; }
    
    /// <summary>
    /// The metadata definition this node instance is based on.
    /// </summary>
    public NodeDefinition Definition { get; }

    [ObservableProperty] private double _x;
    [ObservableProperty] private double _y;
    [ObservableProperty] private string _status = "Idle";
    [ObservableProperty] private bool _isSelected;

    // Preview properties for Query/Action nodes
    [ObservableProperty] private string _queryPreview = string.Empty;
    [ObservableProperty] private string _actionPreview = string.Empty;


    // UI-friendly properties derived from Definition
    public string Title => Definition.DisplayName;
    public string NodeType => Definition.NodeType;
    public bool IsPreviewOnly => Definition.IsPreviewOnly;
    public string AccentColor => Definition.AccentColor;
    
    // Dynamic parameters generated from Definition
    public ObservableCollection<NodeParameterViewModel> Parameters { get; } = new();
    public PortViewModel InputPort { get; }
    public PortViewModel OutputPort { get; }
    // Badge properties for UI display
    public string BadgeText => IsPreviewOnly ? "PREVIEW" : "ACTION";
    public string BadgeColor => IsPreviewOnly ? "#4CAF50" : "#FF9800";

    public NodeViewModel(NodeDefinition definition, double x, double y)
        : this(Guid.NewGuid(), definition, x, y, string.Empty)
    {
    }

    public NodeViewModel(string title, string nodeType, double x, double y, string defaultParam = "")
        : this(Guid.NewGuid(), CreateDefinition(title, nodeType), x, y, defaultParam)
    {
    }
    
    public NodeViewModel(Guid id, string title, string nodeType, double x, double y, string parameterValue = "")
        : this(id, CreateDefinition(title, nodeType), x, y, parameterValue)
    {
    }

    private NodeViewModel(Guid id, NodeDefinition definition, double x, double y, string parameterValue)
    {
        Id = id;
        Definition = definition;
        X = x;
        Y = y;

        // Initialize Parameters based on Definition
        foreach (var paramDef in definition.Parameters)
        {
            Parameters.Add(new NodeParameterViewModel(paramDef));
        }

        // Initialize Ports (Simplified: 1 Input, 1 Output for now)
        var inPortDef = definition.Ports.FirstOrDefault(p => p.IsInput);
        var outPortDef = definition.Ports.FirstOrDefault(p => !p.IsInput);

        InputPort = new PortViewModel(this, true, 0, 50, inPortDef != null);
        OutputPort = new PortViewModel(this, false, 200, 50, outPortDef != null);

        if (!string.IsNullOrEmpty(parameterValue) && Parameters.Count == 0)
        {
            AddTextParam("General Parameter", parameterValue);
        }
    }
    // Helper methods for adding parameters
    public void AddTextParam(string label, string defaultValue = "") => 
        Parameters.Add(new NodeParameterViewModel(label, "Text", defaultValue));
    
    public void AddDropdownParam(string label, params string[] options)
    {
        var p = new NodeParameterViewModel(label, "Dropdown");
        foreach(var opt in options) p.Options.Add(opt);
        if(options.Length > 0) p.Value = options[0];
        Parameters.Add(p);
    }

    public string ParameterValue
    {
        get => Parameters.FirstOrDefault()?.Value ?? string.Empty;
        set
        {
            if (Parameters.Count == 0)
            {
                AddTextParam("General Parameter", value);
            }
            else
            {
                Parameters[0].Value = value;
            }
        }
    }

    public string GetParameterValue(string key)
    {
        return Parameters.FirstOrDefault(p => p.Key == key)?.Value ?? string.Empty;
    }
    // Update preview based on node type and parameters
    private void UpdatePreview()
    {
        if (IsPreviewOnly)
        {
            UpdateQueryPreview();
        }
        else
        {
            UpdateActionPreview();
        }
    }
     private void UpdateQueryPreview()
    {
        QueryPreview = NodeType switch
        {
            "AllFiles" => "SELECT * FROM files",
            "FilterTxt" => "SELECT * FROM files WHERE extension = '.txt'",
            "FilterMd" => "SELECT * FROM files WHERE extension = '.md'",
            "FullTextSearch" => $"SELECT * FROM files_fts WHERE content MATCH '{ParameterValue}'",
            "DynamicRule" => $"FILTER: {ParameterValue}",
            "ConditionBranch" => $"WHERE {ParameterValue}",
            _ => "Query logic will be displayed here"
        };
    }   
    private void UpdateActionPreview()
    {
        ActionPreview = NodeType switch
        {
            "AddTagAI" => $"Add tag 'AI' to all files in input set",
            "SetSubjectCS" => $"Set subject to 'Computer Science' for all files",
            "AutoTag" => "Automatically analyze and tag files based on content",
            "CreateRelationship" => $"Create relationship links to target file",
            "ExportCsv" => $"Export to CSV file: {ParameterValue}",
            "ExportJson" => $"Export to JSON file: {ParameterValue}",
            "ExportDcXml" => $"Export to Dublin Core XML: {ParameterValue}",
            _ => "Action will modify metadata or files"
        };
    }

    private static NodeDefinition CreateDefinition(string title, string nodeType)
    {
        var category = nodeType switch
        {
            "AllFiles" or "FolderScanner" => NodeCategory.Source,
            "AddTagAI" or "SetSubjectCS" or "AutoTag" or "CreateRelationship" => NodeCategory.Action,
            "Result" or "ExportCsv" or "ExportJson" or "ExportDcXml" => NodeCategory.Output,
            "FindRelated" => NodeCategory.Relationship,
            _ => NodeCategory.Processor
        };

        var accentColor = category switch
        {
            NodeCategory.Source => "#4CAF50",
            NodeCategory.Processor => "#2196F3",
            NodeCategory.Action => "#FF9800",
            NodeCategory.Output => "#9C27B0",
            NodeCategory.Relationship => "#FF9800",
            _ => "#607D8B"
        };

        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = title,
            Description = nodeType,
            Category = category,
            SubCategory = string.Empty,
            IsPreviewOnly = nodeType is "AllFiles" or "FolderScanner" or "FilterTxt" or "FilterMd" or "DynamicRule" or "FullTextSearch" or "ConditionBranch" or "MergeBranches" or "Result",
            AccentColor = accentColor,
            Ports =
            {
                new PortDefinition { Name = "Input", DataType = PortDataType.FileSet, IsInput = true },
                new PortDefinition { Name = "Output", DataType = PortDataType.FileSet, IsInput = false }
            }
        };
    }
}

/// <summary>
/// Represents a visual port on the node card.
/// </summary>
public partial class PortViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public NodeViewModel ParentNode { get; }
    public bool IsInput { get; }
    public bool IsConnected { get; } // Simplified for UI visibility
    public double RelativeX { get; }
    public double RelativeY { get; }

    [ObservableProperty] private double _absoluteX;
    [ObservableProperty] private double _absoluteY;

    public PortViewModel(NodeViewModel parentNode, bool isInput, double relX, double relY, bool isConnected = true)
    {
        ParentNode = parentNode;
        IsInput = isInput;
        RelativeX = relX;
        RelativeY = relY;
        IsConnected = isConnected;
    }
}
