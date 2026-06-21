using System;
using ArchiveFlow.App.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArchiveFlow.App.Views;

public partial class MetadataEditorWindow : Window
{
    private MetadataEditorViewModel? _viewModel;

    public MetadataEditorWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RequestClose -= OnRequestClose;
        }

        _viewModel = DataContext as MetadataEditorViewModel;

        if (_viewModel != null)
        {
            _viewModel.RequestClose += OnRequestClose;
        }
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        Close();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.RequestClose -= OnRequestClose;
        }
    }

    private void RemoveFieldButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not MetadataEditorFieldViewModel field)
        {
            return;
        }

        if (DataContext is MetadataEditorViewModel viewModel)
        {
            viewModel.RemoveField(field);
        }
    }
}
