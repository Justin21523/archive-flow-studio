using System;
using System.Collections.Generic;
using System.Linq;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.App.ViewModels;

/// <summary>
/// Represents one enriched row in the result table.
/// It combines FileRecord properties with frequently used metadata fields.
/// </summary>
public sealed class ResultFileRowViewModel
{
    public FileRecord FileRecord { get; }

    public string Id => FileRecord.Id;

    public string ArchiveId => FileRecord.ArchiveId;

    public string FileName => FileRecord.FileName;

    public string FileExtension => FileRecord.FileExtension;

    public string FileType { get; }

    public long FileSizeBytes => FileRecord.FileSize;

    public string FileSizeText { get; }

    public string StatusText => FileRecord.GetStatus().ToString();

    public string Subject { get; }

    public string Tags { get; }

    public string Project { get; }

    public string ReadingStatus { get; }

    public string ImportedAtText => FileRecord.ImportedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string FilePath => FileRecord.FilePath;

    public double MetadataCompleteness { get; }

    public string MetadataCompletenessText => $"{MetadataCompleteness:F0}%";

    private ResultFileRowViewModel(
        FileRecord fileRecord,
        string fileType,
        string subject,
        string tags,
        string project,
        string readingStatus,
        double metadataCompleteness)
    {
        FileRecord = fileRecord;
        FileType = fileType;
        Subject = subject;
        Tags = tags;
        Project = project;
        ReadingStatus = readingStatus;
        MetadataCompleteness = metadataCompleteness;
        FileSizeText = FormatFileSize(fileRecord.FileSize);
    }

    public static ResultFileRowViewModel Create(
        FileRecord fileRecord,
        IReadOnlyList<MetadataValue> metadata)
    {
        var subject = GetFirst(metadata, "subject");
        var project = GetFirst(metadata, "project");
        var readingStatus = GetFirst(metadata, "reading_status");

        var tags = string.Join(
            ", ",
            metadata
                .Where(x => string.Equals(x.FieldName, "tag", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.ValueText)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase));

        var completeness = CalculateCompleteness(metadata);

        return new ResultFileRowViewModel(
            fileRecord,
            ResolveFileType(fileRecord.FileExtension),
            subject,
            tags,
            project,
            readingStatus,
            completeness);
    }

    private static string GetFirst(IReadOnlyList<MetadataValue> metadata, string fieldName)
    {
        return metadata
            .FirstOrDefault(x => string.Equals(x.FieldName, fieldName, StringComparison.OrdinalIgnoreCase))
            ?.ValueText ?? string.Empty;
    }

    private static double CalculateCompleteness(IReadOnlyList<MetadataValue> metadata)
    {
        var requiredFields = new[]
        {
            "title",
            "subject",
            "description",
            "project",
            "tag"
        };

        var completed = requiredFields.Count(field =>
            metadata.Any(value =>
                string.Equals(value.FieldName, field, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value.ValueText)));

        return completed / (double)requiredFields.Length * 100.0;
    }

    private static string ResolveFileType(string extension)
    {
        extension = extension.Trim().ToLowerInvariant();

        return extension switch
        {
            ".pdf" or ".doc" or ".docx" or ".txt" or ".md" or ".rtf" or ".epub" => "Document",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".tiff" or ".bmp" or ".svg" => "Image",
            ".mp4" or ".mov" or ".mkv" or ".avi" or ".webm" => "Video",
            ".mp3" or ".wav" or ".flac" or ".ogg" or ".m4a" => "Audio",
            ".cs" or ".js" or ".ts" or ".py" or ".java" or ".cpp" or ".h" or ".html" or ".css" => "Code",
            ".blend" or ".fbx" or ".obj" or ".glb" or ".gltf" or ".stl" => "3D Model",
            ".zip" or ".7z" or ".rar" or ".tar" or ".gz" => "Archive",
            ".csv" or ".json" or ".xml" or ".sqlite" or ".db" => "Dataset",
            _ => "Other"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        if (bytes >= 1024L * 1024L * 1024L)
        {
            return $"{bytes / 1024d / 1024d / 1024d:F2} GB";
        }

        if (bytes >= 1024L * 1024L)
        {
            return $"{bytes / 1024d / 1024d:F2} MB";
        }

        if (bytes >= 1024L)
        {
            return $"{bytes / 1024d:F1} KB";
        }

        return $"{bytes} B";
    }
}