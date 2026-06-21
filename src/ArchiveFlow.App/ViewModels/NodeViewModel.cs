using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ArchiveFlow.Application.Nodes.Definitions;
using ArchiveFlow.Domain.Entities;
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
    public ObservableCollection<NodeParameterInstanceViewModel> Parameters { get; } = new();
    public PortViewModel InputPort { get; }
    public PortViewModel OutputPort { get; }
    public ObservableCollection<FileRecord> OutputFiles { get; } = new();
    
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
            Parameters.Add(new NodeParameterInstanceViewModel(paramDef));
        }

        // Initialize Ports (Simplified: 1 Input, 1 Output for now)
        var inPortDef = definition.InputPorts.FirstOrDefault();
        var outPortDef = definition.OutputPorts.FirstOrDefault();

        InputPort = new PortViewModel(this, true, 0, 50, inPortDef != null);
        OutputPort = new PortViewModel(this, false, 200, 50, outPortDef != null);

        if (!string.IsNullOrEmpty(parameterValue) && Parameters.Count == 0)
        {
            AddTextParam("General Parameter", parameterValue);
        }
    }
    // Helper methods for adding parameters
    public void AddTextParam(string label, string defaultValue = "") => 
        Parameters.Add(CreateParameter(label, NodeParameterControlType.Text, defaultValue));
    
    public void AddDropdownParam(string label, params string[] options)
    {
        Parameters.Add(CreateParameter(
            label,
            NodeParameterControlType.Dropdown,
            options.FirstOrDefault() ?? string.Empty,
            options));
    }

    public void AddNumberParam(string label, string defaultValue = "0")
    {
        Parameters.Add(CreateParameter(label, NodeParameterControlType.Number, defaultValue));
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
            "AllFiles" or "FolderScanner" => NodeCategory.Sources,
            "AddTagAI" or "SetSubjectCS" or "AutoTag" => NodeCategory.MetadataActions,
            "CreateRelationship" or "FindRelated" => NodeCategory.Relationships,
            "Result" or "ExportCsv" or "ExportJson" or "ExportDcXml" => NodeCategory.Outputs,
            _ => NodeCategory.QueryFilters
        };

        var accentColor = category switch
        {
            NodeCategory.Sources => "#4CAF50",
            NodeCategory.QueryFilters => "#2196F3",
            NodeCategory.MetadataActions => "#FF9800",
            NodeCategory.Outputs => "#9C27B0",
            NodeCategory.Relationships => "#FF9800",
            _ => "#607D8B"
        };

        return new NodeDefinition
        {
            NodeType = nodeType,
            DisplayName = title,
            Description = nodeType,
            Category = category,
            Subcategory = string.Empty,
            IsPreviewOnly = nodeType is "AllFiles" or "FolderScanner" or "FilterTxt" or "FilterMd" or "DynamicRule" or "FullTextSearch" or "ConditionBranch" or "MergeBranches" or "Result",
            AccentColor = accentColor,
            InputPorts = new[]
            {
                new NodePortDefinition { Name = "Input", DataType = NodePortDataType.FileSet }
            },
            OutputPorts = new[]
            {
                new NodePortDefinition { Name = "Output", DataType = NodePortDataType.FileSet }
            }
        };
    }

    private static NodeParameterInstanceViewModel CreateParameter(
        string label,
        NodeParameterControlType controlType,
        string defaultValue,
        IReadOnlyList<string>? options = null)
    {
        var key = new string(label
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return new NodeParameterInstanceViewModel(new NodeParameterDefinition
        {
            Key = string.IsNullOrWhiteSpace(key) ? "parameter" : key,
            DisplayName = label,
            ControlType = controlType,
            DefaultValue = defaultValue,
            Options = options ?? Array.Empty<string>()
        });
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
