using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

public partial class NodeViewModel : ObservableObject
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _nodeType = string.Empty; // e.g., "AllFiles", "Filter", "Result"

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Idle";
    
    // 新增：節點的參數 (例如搜尋關鍵字)
    [ObservableProperty] 
    private string _parameterValue = string.Empty;

    public PortViewModel InputPort { get; set;}
    public PortViewModel OutputPort { get; set;}

    // 預設建構子
    public NodeViewModel(string title, string nodeType, double x, double y, string defaultParam = "")
    {
        Id = Guid.NewGuid();
        Initialize(title, nodeType, x, y, defaultParam);
    }

    // 新增：用於 Load Workflow 的建構子
    public NodeViewModel(Guid id, string title, string nodeType, double x, double y, string defaultParam = "")
    {
        Id = id;
        Initialize(title, nodeType, x, y, defaultParam);
    }

    private void Initialize(string title, string nodeType, double x, double y, string defaultParam)
    {
        Title = title;
        NodeType = nodeType;
        X = x; Y = y;
        ParameterValue = defaultParam;
        InputPort = new PortViewModel(this, true, 0, 50);
        OutputPort = new PortViewModel(this, false, 200, 50);
    }
}

public partial class PortViewModel : ObservableObject
{
    public Guid Id { get; } = Guid.NewGuid();
    public NodeViewModel ParentNode { get; }
    public bool IsInput { get; }
    
    // Relative position within the NodeView
    public double RelativeX { get; }
    public double RelativeY { get; }

    // Absolute position on the Canvas (calculated by Canvas)
    [ObservableProperty]
    private double _absoluteX;

    [ObservableProperty]
    private double _absoluteY;

    public PortViewModel(NodeViewModel parentNode, bool isInput, double relX, double relY)
    {
        ParentNode = parentNode;
        IsInput = isInput;
        RelativeX = relX;
        RelativeY = relY;
    }
}
