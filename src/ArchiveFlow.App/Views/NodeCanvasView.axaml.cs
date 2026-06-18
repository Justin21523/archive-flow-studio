using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using ArchiveFlow.App.ViewModels;
using ArchiveFlow.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Threading;

namespace ArchiveFlow.App.Views;

public partial class NodeCanvasView : UserControl
{
    private NodeViewModel? _draggedNode;
    private Point _dragOffset;
    private NodeCanvasViewModel? _viewModel;
    private readonly Dictionary<NodeViewModel, NodeView> _nodeViews = new();
    private readonly Dictionary<EdgeViewModel, Path> _edgeViews = new();
    // Performance optimization: Viewport tracking
    private Rect _currentViewport;
    private bool _isUpdatingNodes;

    public NodeCanvasView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public NodeCanvasViewModel? ViewModel => _viewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.Nodes.CollectionChanged -= OnNodesCollectionChanged;
            _viewModel.Edges.CollectionChanged -= OnEdgesCollectionChanged;
            foreach (var node in _viewModel.Nodes)
            {
                node.PropertyChanged -= OnNodePropertyChanged;
            }
            foreach (var edge in _viewModel.Edges)
            {
                edge.PropertyChanged -= OnEdgePropertyChanged;
            }
        }

        _viewModel = DataContext as NodeCanvasViewModel;
        EdgeLayer.Children.Clear();
        NodeLayer.Children.Clear();
        _edgeViews.Clear();
        _nodeViews.Clear();

        if (_viewModel != null)
        {
            _viewModel.Nodes.CollectionChanged += OnNodesCollectionChanged;
            _viewModel.Edges.CollectionChanged += OnEdgesCollectionChanged;
            UpdateCanvasViewportCenter();
            foreach (var edge in _viewModel.Edges)
            {
                AddEdgeView(edge);
            }
            foreach (var node in _viewModel.Nodes)
            {
                AddNodeView(node);
            }
        }
    }

    private void CanvasViewport_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            _currentViewport = new Rect(new Point(sv.Offset.X, sv.Offset.Y), sv.Viewport);
            UpdateVisibleNodes();
        }
    }

    private void CanvasViewport_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            _currentViewport = new Rect(new Point(sv.Offset.X, sv.Offset.Y), sv.Viewport);
            
            // Throttle updates for performance
            if (!_isUpdatingNodes)
            {
                _isUpdatingNodes = true;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    UpdateVisibleNodes();
                    _isUpdatingNodes = false;
                }, DispatcherPriority.Background);
            }
        }
    }
    /// <summary>
    /// Optimized node rendering - only render visible nodes
    /// </summary>
    private void UpdateVisibleNodes()
    {
        if (_viewModel == null || MainCanvas == null) return;

        // Expand viewport slightly for smoother scrolling
        var expandedViewport = _currentViewport.Inflate(200);

        foreach (var nodeVm in _viewModel.Nodes)
        {
            var nodeView = FindNodeView(nodeVm);
            if (nodeView != null)
            {
                var nodeBounds = new Rect(nodeVm.X, nodeVm.Y, 200, 100);
                nodeView.IsVisible = expandedViewport.Intersects(nodeBounds);
            }
        }
    }

    private NodeView? FindNodeView(NodeViewModel node)
    {
        return _nodeViews.TryGetValue(node, out var nodeView) ? nodeView : null;
    }
    private void Canvas_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        // Deselect when clicking on empty canvas
        if (sender is Canvas && e.Source is Canvas)
        {
            _viewModel?.SelectNode(null);
        }
    }

    private void UpdateCanvasViewportCenter()
    {
        if (ViewModel == null) return;

        var viewportWidth = CanvasViewport.Viewport.Width;
        var viewportHeight = CanvasViewport.Viewport.Height;

        if (double.IsNaN(viewportWidth) || viewportWidth <= 0)
        {
            viewportWidth = CanvasViewport.Bounds.Width;
        }

        if (double.IsNaN(viewportHeight) || viewportHeight <= 0)
        {
            viewportHeight = CanvasViewport.Bounds.Height;
        }

        var centerX = CanvasViewport.Offset.X + viewportWidth / 2;
        var centerY = CanvasViewport.Offset.Y + viewportHeight / 2;

        centerX = Math.Clamp(centerX, 0, MainCanvas.Bounds.Width > 0 ? MainCanvas.Bounds.Width : MainCanvas.Width);
        centerY = Math.Clamp(centerY, 0, MainCanvas.Bounds.Height > 0 ? MainCanvas.Bounds.Height : MainCanvas.Height);

        ViewModel.UpdateCanvasViewportCenter(centerX, centerY);
    }

    private void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (NodeViewModel node in e.OldItems)
            {
                RemoveNodeView(node);
            }
        }

        if (e.NewItems != null)
        {
            foreach (NodeViewModel node in e.NewItems)
            {
                AddNodeView(node);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var node in _nodeViews.Keys.ToList())
            {
                RemoveNodeView(node);
            }

            if (_viewModel != null)
            {
                foreach (var node in _viewModel.Nodes)
                {
                    AddNodeView(node);
                }
            }
        }
    }

    private void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (EdgeViewModel edge in e.OldItems)
            {
                RemoveEdgeView(edge);
            }
        }

        if (e.NewItems != null)
        {
            foreach (EdgeViewModel edge in e.NewItems)
            {
                AddEdgeView(edge);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var edge in _edgeViews.Keys.ToList())
            {
                RemoveEdgeView(edge);
            }

            if (_viewModel != null)
            {
                foreach (var edge in _viewModel.Edges)
                {
                    AddEdgeView(edge);
                }
            }
        }
    }

    private void AddEdgeView(EdgeViewModel edge)
    {
        if (_edgeViews.ContainsKey(edge)) return;

        var path = new Path
        {
            Stroke = Brushes.DodgerBlue,
            StrokeThickness = 3,
            Fill = Brushes.Transparent,
            IsHitTestVisible = false
        };

        _edgeViews[edge] = path;
        EdgeLayer.Children.Add(path);
        UpdateEdgeView(edge);
        edge.PropertyChanged += OnEdgePropertyChanged;
    }

    private void RemoveEdgeView(EdgeViewModel edge)
    {
        edge.PropertyChanged -= OnEdgePropertyChanged;

        if (_edgeViews.Remove(edge, out var path))
        {
            EdgeLayer.Children.Remove(path);
        }
    }

    private void OnEdgePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is EdgeViewModel edge && e.PropertyName == nameof(EdgeViewModel.PathData))
        {
            UpdateEdgeView(edge);
        }
    }

    private void UpdateEdgeView(EdgeViewModel edge)
    {
        if (_edgeViews.TryGetValue(edge, out var path))
        {
            path.Data = string.IsNullOrWhiteSpace(edge.PathData) ? null : Geometry.Parse(edge.PathData);
        }
    }

    private void AddNodeView(NodeViewModel node)
    {
        if (_nodeViews.ContainsKey(node)) return;

        var nodeView = new NodeView
        {
            DataContext = node
        };

        _nodeViews[node] = nodeView;
        NodeLayer.Children.Add(nodeView);
        UpdateNodeViewPosition(node);
        node.PropertyChanged += OnNodePropertyChanged;
    }

    private void RemoveNodeView(NodeViewModel node)
    {
        node.PropertyChanged -= OnNodePropertyChanged;

        if (_nodeViews.Remove(node, out var nodeView))
        {
            NodeLayer.Children.Remove(nodeView);
        }
    }

    private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is NodeViewModel node && (e.PropertyName == nameof(NodeViewModel.X) || e.PropertyName == nameof(NodeViewModel.Y)))
        {
            UpdateNodeViewPosition(node);
        }
    }

    private void UpdateNodeViewPosition(NodeViewModel node)
    {
        if (_nodeViews.TryGetValue(node, out var nodeView))
        {
            Canvas.SetLeft(nodeView, node.X);
            Canvas.SetTop(nodeView, node.Y);
        }
    }

    public Point GetCanvasPosition(PointerEventArgs e)
    {
        return e.GetPosition(MainCanvas);
    }

    public void StartNodeDrag(NodeViewModel node, Point canvasPosition, IPointer pointer)
    {
        _draggedNode = node;
        _dragOffset = new Point(canvasPosition.X - node.X, canvasPosition.Y - node.Y);
        pointer.Capture(MainCanvas);
    }

    public void StartConnection(PortViewModel port, Point canvasPosition, IPointer pointer)
    {
        ViewModel?.StartConnection(port);
        ViewModel?.UpdateTempConnection(canvasPosition.X, canvasPosition.Y);
        pointer.Capture(MainCanvas);
    }

    public void FinishConnection(PortViewModel port)
    {
        ViewModel?.FinishConnection(port);
    }

    private void Canvas_PointerMoved(object sender, PointerEventArgs e)
    {
        var pos = e.GetPosition(MainCanvas);

        if (_draggedNode != null)
        {
            ViewModel?.UpdateNodePosition(_draggedNode, pos.X - _dragOffset.X, pos.Y - _dragOffset.Y);
            UpdateNodeViewPosition(_draggedNode);
        }
        else if (ViewModel?.IsConnecting == true)
        {
            ViewModel.UpdateTempConnection(pos.X, pos.Y);
        }
    }

    private void Canvas_PointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (_draggedNode != null)
        {
            _draggedNode = null;
            e.Pointer.Capture(null);
            return;
        }

        if (ViewModel?.IsConnecting == true)
        {
            var pos = e.GetPosition(MainCanvas);
            if (!ViewModel.TryFinishConnectionAt(pos.X, pos.Y))
            {
                ViewModel.CancelConnection();
            }

            e.Pointer.Capture(null);
        }
    }

    private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is FileRecord file)
        {
            ViewModel?.SelectFile(file);
        }
    }
    private void UserControl_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete || e.Key == Key.Back)
        {
            _viewModel?.DeleteSelectedNodeCommand.Execute(null);
            e.Handled = true;
        }
    }

    // 確保在 OnDataContextChanged 或初始化時，讓 UserControl 獲得焦點，以便接收鍵盤事件
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateCanvasViewportCenter();
        this.Focus();
    }

    // 新增方法：處理 TreeView 節點點擊
    private void NodeLibraryItem_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border && border.Tag is string nodeType && !string.IsNullOrEmpty(nodeType))
        {
            var command = nodeType switch
            {
                "AllFiles" => _viewModel?.AddAllFilesCommand,
                "FilterTxt" => _viewModel?.AddFilterTxtCommand,
                "FilterMd" => _viewModel?.AddFilterMdCommand,
                "AddTagAI" => _viewModel?.AddTagAICommand,
                "Result" => _viewModel?.AddResultTableCommand,
                _ => null
            };
            
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
            }
        }
    }

}
