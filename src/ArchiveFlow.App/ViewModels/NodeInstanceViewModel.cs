using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ArchiveFlow.Application.Nodes.Definitions;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one node instance placed on the canvas.
/// It is driven by a NodeDefinition and contains visual state only.
/// </summary>
public partial class NodeInstanceViewModel : ObservableObject
{
    private const double NodeWidth = 230;
    private const double NodePortCenterY = 59;

    public string InstanceId { get; } = Guid.NewGuid().ToString("N");
    public NodeDefinition Definition { get; }

    public string Title => Definition.DisplayName;
    public string NodeType => Definition.NodeType;
    public string Category => Definition.Category.ToString();
    public string Subcategory => Definition.Subcategory;
    public string Description => Definition.Description;
    public string BadgeText => Definition.IsActionNode ? "ACTION" : "PREVIEW";
    public string ModeText => Definition.IsPreviewOnly ? "Preview only" : "Requires preview and apply";

    public int InputCount => Definition.InputPorts.Count;
    public int OutputCount => Definition.OutputPorts.Count;

    public bool HasInputPort => InputPort != null;
    public bool HasOutputPort => OutputPort != null;

    public IBrush AccentBrush { get; }
    public IBrush BadgeBrush { get; }
    public IBrush BorderBrush => IsSelected ? Brushes.White : AccentBrush;

    public PortInstanceViewModel? InputPort { get; }
    public PortInstanceViewModel? OutputPort { get; }

    public ObservableCollection<NodeParameterInstanceViewModel> Parameters { get; } = new();

    public string ParameterSummary => GetParameterSummary();
    public string PreviewSummary => BuildPreviewSummary();
    public string OperationPreview => BuildOperationPreview();
    public string WarningSummary => BuildWarningSummary();

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private int _lastInputFileCount;

    [ObservableProperty]
    private int _lastOutputFileCount;

    public NodeInstanceViewModel(NodeDefinition definition, double x, double y)
    {
        Definition = definition;
        X = x;
        Y = y;

        AccentBrush = Brush.Parse(definition.AccentColor);
        BadgeBrush = definition.IsActionNode
            ? Brush.Parse("#FF9800")
            : Brush.Parse("#4CAF50");

        foreach (var parameterDefinition in definition.Parameters)
        {
            var parameter = new NodeParameterInstanceViewModel(parameterDefinition);
            parameter.ValueChanged += (_, _) => RefreshPreview();

            Parameters.Add(parameter);
        }

        var inputDefinition = definition.InputPorts.FirstOrDefault();
        if (inputDefinition != null)
        {
            InputPort = new PortInstanceViewModel(
                this,
                isInput: true,
                inputDefinition.DataType,
                relativeX: 0,
                relativeY: NodePortCenterY);
        }

        var outputDefinition = definition.OutputPorts.FirstOrDefault();
        if (outputDefinition != null)
        {
            OutputPort = new PortInstanceViewModel(
                this,
                isInput: false,
                outputDefinition.DataType,
                relativeX: NodeWidth,
                relativeY: NodePortCenterY);
        }

        RefreshPreview();
    }

    public string GetParameterValue(string key, string defaultValue = "")
    {
        return Parameters.FirstOrDefault(x => x.Key == key)?.Value ?? defaultValue;
    }

    public void SetRunStats(int inputCount, int outputCount)
    {
        LastInputFileCount = inputCount;
        LastOutputFileCount = outputCount;

        RefreshPreview();
    }

    public void RefreshPreview()
    {
        OnPropertyChanged(nameof(ParameterSummary));
        OnPropertyChanged(nameof(PreviewSummary));
        OnPropertyChanged(nameof(OperationPreview));
        OnPropertyChanged(nameof(WarningSummary));
    }

    private string GetParameterSummary()
    {
        if (Parameters.Count == 0)
        {
            return "No parameters";
        }

        return string.Join(", ", Parameters.Take(3).Select(x => $"{x.DisplayName}: {x.Value}"));
    }

    private string BuildPreviewSummary()
    {
        if (Definition.IsActionNode)
        {
            var targetCount = LastInputFileCount > 0 ? LastInputFileCount : LastOutputFileCount;

            return targetCount > 0
                ? $"Will modify {targetCount} files.\nRequires Apply.\nNo changes are written during preview."
                : "Action preview is ready.\nRun the workflow to calculate affected files.\nRequires Apply.";
        }

        var excluded = Math.Max(0, LastInputFileCount - LastOutputFileCount);

        return NodeType.StartsWith("source.", StringComparison.OrdinalIgnoreCase)
            ? $"Source node.\nOutput Count: {LastOutputFileCount}"
            : $"Input Count: {LastInputFileCount}\nOutput Count: {LastOutputFileCount}\nExcluded Count: {excluded}";
    }

    private string BuildOperationPreview()
    {
        return NodeType switch
        {
            "source.all_files" =>
                "SELECT * FROM files ORDER BY imported_at DESC;",

            "source.folder_source" =>
                $"SELECT * FROM files WHERE file_path LIKE '%{EscapeSqlLike(GetParameterValue("folderPath"))}%';",

            "source.recent_imports" =>
                "SELECT * FROM files ORDER BY imported_at DESC LIMIT 100;",

            "source.unorganized_files" =>
                "SELECT * FROM files WHERE status = 0 OR status IS NULL;",

            "source.missing_metadata" =>
                "SELECT files.* FROM files LEFT JOIN metadata_values ON files.id = metadata_values.file_id WHERE metadata_values.id IS NULL;",

            "source.duplicate_files" =>
                "SELECT * FROM files WHERE file_hash IN (SELECT file_hash FROM files GROUP BY file_hash HAVING COUNT(*) > 1);",

            "filter.extension" =>
                $"WHERE file_extension = '{EscapeSqlLiteral(GetParameterValue("extension", ".pdf"))}'",

            "filter.file_type" =>
                $"WHERE file_extension IN ({BuildFileTypeExpression(GetParameterValue("fileType", "Document"))})",

            "filter.path_contains" =>
                $"WHERE file_path LIKE '%{EscapeSqlLike(GetParameterValue("pathText"))}%'",

            "filter.status" =>
                $"WHERE status = '{EscapeSqlLiteral(GetParameterValue("status", "New"))}'",

            "filter.size" =>
                $"WHERE file_size BETWEEN {GetParameterValue("minSizeMb", "0")}MB AND {GetParameterValue("maxSizeMb", "100")}MB",

            "filter.date_range" =>
                $"WHERE imported_at BETWEEN '{EscapeSqlLiteral(GetParameterValue("startDate"))}' AND '{EscapeSqlLiteral(GetParameterValue("endDate"))}'",

            "filter.metadata_field" =>
                $"WHERE metadata.{EscapeSqlLiteral(GetParameterValue("fieldName", "subject"))} {GetParameterValue("operator", "contains")} '{EscapeSqlLiteral(GetParameterValue("value"))}'",

            "search.keyword" =>
                $"WHERE file_name LIKE '%{EscapeSqlLike(GetParameterValue("query"))}%' OR content_preview LIKE '%{EscapeSqlLike(GetParameterValue("query"))}%'",

            "search.filename" =>
                $"WHERE file_name LIKE '%{EscapeSqlLike(GetParameterValue("filename"))}%'",

            "search.full_text" =>
                $"MATCH files_fts AGAINST '{EscapeSqlLiteral(GetParameterValue("query"))}'",

            "search.boolean" =>
                $"BOOLEAN SEARCH: {GetParameterValue("expression", "AI AND metadata")}",

            "logic.limit" =>
                $"LIMIT {GetParameterValue("count", "100")}",

            "logic.sort_by" =>
                $"ORDER BY {GetParameterValue("field", "Imported Date")} {GetParameterValue("direction", "Descending")}",

            "logic.group_by" =>
                $"GROUP BY {GetParameterValue("field", "Extension")}",

            "logic.and" =>
                "INTERSECT connected file sets.",

            "logic.or" =>
                "UNION connected file sets.",

            "logic.not" =>
                "EXCLUDE files from the connected input set.",

            "metadata.add_tag" =>
                $"Preview metadata update: add tag '{GetParameterValue("tag", "AI")}'.",

            "metadata.remove_tag" =>
                $"Preview metadata update: remove tag '{GetParameterValue("tag")}'.",

            "metadata.set_subject" =>
                $"Preview metadata update: set subject to '{GetParameterValue("subject")}'.",

            "metadata.set_project" =>
                $"Preview metadata update: set project to '{GetParameterValue("project")}'.",

            "metadata.set_status" =>
                $"Preview metadata update: set status to '{GetParameterValue("status", "Archived")}'.",

            "metadata.set_reading_status" =>
                $"Preview metadata update: set reading status to '{GetParameterValue("readingStatus", "To Read")}'.",

            "metadata.set_importance" =>
                $"Preview metadata update: set importance to '{GetParameterValue("importance", "Normal")}'.",

            "metadata.generate_archive_id" =>
                $"Preview metadata update: generate archive IDs using pattern '{GetParameterValue("pattern", "AF-{yyyyMMdd}-{sequence}")}'.",

            "metadata.validate_metadata" =>
                "Preview validation: check required metadata fields and calculate completeness.",

            "file.copy_to_archive" =>
                $"Preview file-system action: copy files to '{GetParameterValue("targetFolder")}'.",

            "file.move_file" =>
                $"Preview file-system action: move files to '{GetParameterValue("targetFolder")}'.",

            "file.rename_file" =>
                $"Preview file-system action: rename files using pattern '{GetParameterValue("pattern")}'.",

            "output.result_table" =>
                "Render current FileSet as Result Table.",

            "output.export_csv" =>
                $"Preview export: write CSV to '{GetParameterValue("fileName", "archiveflow-results.csv")}'.",

            "output.export_json" =>
                $"Preview export: write JSON to '{GetParameterValue("fileName", "archiveflow-results.json")}'.",

            "output.export_dublin_core" =>
                "Preview export: write Dublin Core metadata package.",

            _ =>
                Definition.IsActionNode
                    ? "Preview action operation. Apply is required before writing changes."
                    : "Preview query operation."
        };
    }

    private string BuildWarningSummary()
    {
        var warnings = new List<string>();

        foreach (var parameter in Parameters)
        {
            if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.Value))
            {
                warnings.Add($"Missing required parameter: {parameter.DisplayName}");
            }
        }

        if (Definition.IsActionNode)
        {
            warnings.Add("Action node requires Apply. Execute Workflow only previews changes.");
        }

        if (NodeType.StartsWith("file.", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("File-system action. Physical files must not be changed without explicit Apply confirmation.");
        }

        return warnings.Count == 0
            ? "No warnings."
            : string.Join("\n", warnings);
    }

    private static string BuildFileTypeExpression(string fileType)
    {
        return fileType.ToLowerInvariant() switch
        {
            "document" => "'.pdf', '.doc', '.docx', '.txt', '.md', '.rtf'",
            "image" => "'.png', '.jpg', '.jpeg', '.gif', '.webp', '.tiff', '.bmp'",
            "video" => "'.mp4', '.mov', '.mkv', '.avi', '.webm'",
            "audio" => "'.mp3', '.wav', '.flac', '.ogg', '.m4a'",
            "code" => "'.cs', '.js', '.ts', '.py', '.java', '.cpp', '.html', '.css'",
            "3d model" => "'.blend', '.fbx', '.obj', '.glb', '.gltf', '.stl'",
            "archive" => "'.zip', '.7z', '.rar', '.tar', '.gz'",
            _ => "'*'"
        };
    }

    private static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }

    private static string EscapeSqlLike(string value)
    {
        return EscapeSqlLiteral(value)
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    partial void OnIsSelectedChanged(bool value)
    {
        OnPropertyChanged(nameof(BorderBrush));
    }
}