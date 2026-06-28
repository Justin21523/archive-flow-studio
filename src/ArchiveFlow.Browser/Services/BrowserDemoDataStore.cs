using System.Text;
using System.Xml.Linq;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Browser.Services;

public sealed class BrowserDemoDataStore :
    IDataRepository,
    IFileRepository,
    IMetadataRepository,
    IRelationshipRepository,
    IExportJobRepository,
    IImportJobRepository,
    IDemoDataService,
    IImportPipelineService,
    IExportService
{
    private readonly List<FileRecord> _files = [];
    private readonly List<MetadataField> _fields = [];
    private readonly List<MetadataValue> _metadataValues = [];
    private readonly List<FileRelationship> _relationships = [];
    private readonly List<ExportJobRecord> _exportJobs = [];
    private readonly List<ImportJobRecord> _importJobs = [];
    private int _nextFieldId = 1;
    private int _nextMetadataValueId = 1;
    private int _nextRelationshipId = 1;

    public IFileRepository Files => this;
    public IMetadataRepository Metadata => this;
    public IRelationshipRepository Relationships => this;
    public IExportJobRepository ExportJobs => this;
    public IImportJobRepository ImportJobs => this;

    public string LastExportContent { get; private set; } = string.Empty;

    public Task ResetAsync(CancellationToken cancellationToken = default)
    {
        _files.Clear();
        _fields.Clear();
        _metadataValues.Clear();
        _relationships.Clear();
        _exportJobs.Clear();
        _importJobs.Clear();
        _nextFieldId = 1;
        _nextMetadataValueId = 1;
        _nextRelationshipId = 1;

        SeedBaseData();
        return Task.CompletedTask;
    }

    public Task LoadScenarioAsync(string scenarioName, CancellationToken cancellationToken = default)
    {
        ResetAsync(cancellationToken).GetAwaiter().GetResult();

        if (scenarioName.Contains("Metadata", StringComparison.OrdinalIgnoreCase))
        {
            var target = _files.First(file => file.FileExtension == ".pdf");
            _metadataValues.RemoveAll(value => value.FileId == target.Id && value.FieldName == "subject");
        }
        else if (scenarioName.Contains("Design", StringComparison.OrdinalIgnoreCase))
        {
            var image = _files.First(file => file.FileExtension is ".png" or ".svg");
            var model = _files.First(file => file.FileExtension == ".glb");
            TryCreateRelationshipAsync(image.Id, model.Id, "references").GetAwaiter().GetResult();
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        var existingIndex = _files.FindIndex(file => file.Id == record.Id);
        if (existingIndex >= 0)
        {
            _files[existingIndex] = record;
        }
        else if (_files.All(file => !string.Equals(file.FileHash, record.FileHash, StringComparison.OrdinalIgnoreCase)))
        {
            _files.Add(record);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FileRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<FileRecord>>(_files.OrderByDescending(file => file.ImportedAt).ToList());
    }

    public Task<FileRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.FirstOrDefault(file => file.Id == id));
    }

    public Task<FileRecord?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.FirstOrDefault(file => string.Equals(file.FileHash, fileHash, StringComparison.OrdinalIgnoreCase)));
    }

    public Task UpdatePreviewAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        return SaveAsync(record, cancellationToken);
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_files.Count);
    }

    public Task<MetadataField> GetOrCreateFieldAsync(
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        bool isRequired = false,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeFieldName(fieldName);
        var existing = _fields.FirstOrDefault(field => string.Equals(field.FieldName, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var field = new MetadataField
        {
            Id = _nextFieldId++,
            FieldName = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? fieldName : displayName,
            FieldType = string.IsNullOrWhiteSpace(fieldType) ? "String" : fieldType,
            Category = string.IsNullOrWhiteSpace(category) ? "Basic" : category,
            IsRequired = isRequired,
            SortOrder = _fields.Count + 1
        };
        _fields.Add(field);
        return Task.FromResult(field);
    }

    public Task<IReadOnlyList<MetadataField>> GetAllFieldsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MetadataField>>(_fields.OrderBy(field => field.SortOrder).ToList());
    }

    public Task UpdateFieldDefinitionAsync(int fieldId, string displayName, string fieldType, string category, bool isRequired, CancellationToken cancellationToken = default)
    {
        var field = _fields.FirstOrDefault(item => item.Id == fieldId);
        if (field is not null)
        {
            field.DisplayName = displayName;
            field.FieldType = fieldType;
            field.Category = category;
            field.IsRequired = isRequired;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MetadataValue>> GetMetadataByFileIdAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MetadataValue>>(_metadataValues
            .Where(value => value.FileId == fileId)
            .OrderBy(value => value.SortOrder)
            .ThenBy(value => value.ValueText)
            .Select(CloneMetadataValue)
            .ToList());
    }

    public Task<IReadOnlyList<MetadataValue>> GetMetadataValuesByFieldAsync(string fileId, string fieldName, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeFieldName(fieldName);
        return Task.FromResult<IReadOnlyList<MetadataValue>>(_metadataValues
            .Where(value => value.FileId == fileId && string.Equals(value.FieldName, normalized, StringComparison.OrdinalIgnoreCase))
            .Select(CloneMetadataValue)
            .ToList());
    }

    public Task<string?> GetFirstMetadataValueAsync(string fileId, string fieldName, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeFieldName(fieldName);
        var value = _metadataValues.FirstOrDefault(item =>
            item.FileId == fileId &&
            string.Equals(item.FieldName, normalized, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(value?.ValueText);
    }

    public Task<bool> HasMetadataValueAsync(string fileId, string fieldName, string valueText, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeFieldName(fieldName);
        var exists = _metadataValues.Any(value =>
            value.FileId == fileId &&
            string.Equals(value.FieldName, normalized, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(value.ValueText, valueText, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }

    public Task<MetadataValue> AddMetadataValueAsync(string fileId, int fieldId, string valueText, CancellationToken cancellationToken = default)
    {
        var field = _fields.First(item => item.Id == fieldId);
        var value = new MetadataValue
        {
            Id = _nextMetadataValueId++,
            FileId = fileId,
            FieldId = field.Id,
            ValueText = valueText,
            CreatedAt = DateTime.UtcNow,
            FieldName = field.FieldName,
            DisplayName = field.DisplayName,
            FieldType = field.FieldType,
            Category = field.Category,
            IsRequired = field.IsRequired,
            SortOrder = field.SortOrder
        };
        _metadataValues.Add(value);
        return Task.FromResult(CloneMetadataValue(value));
    }

    public Task UpdateMetadataValueAsync(int metadataValueId, string valueText, CancellationToken cancellationToken = default)
    {
        var value = _metadataValues.FirstOrDefault(item => item.Id == metadataValueId);
        if (value is not null)
        {
            value.ValueText = valueText;
            value.UpdatedAt = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task DeleteMetadataValueByIdAsync(int metadataValueId, CancellationToken cancellationToken = default)
    {
        _metadataValues.RemoveAll(value => value.Id == metadataValueId);
        return Task.CompletedTask;
    }

    public async Task SetMetadataValueAsync(string fileId, string fieldName, string displayName, string fieldType, string category, string valueText, bool isRequired = false, CancellationToken cancellationToken = default)
    {
        await DeleteMetadataValueAsync(fileId, fieldName, null, cancellationToken);
        var field = await GetOrCreateFieldAsync(fieldName, displayName, fieldType, category, isRequired, cancellationToken);
        await AddMetadataValueAsync(fileId, field.Id, valueText, cancellationToken);
    }

    public async Task AddMetadataValueIfMissingAsync(string fileId, string fieldName, string displayName, string fieldType, string category, string valueText, bool isRequired = false, CancellationToken cancellationToken = default)
    {
        if (await HasMetadataValueAsync(fileId, fieldName, valueText, cancellationToken))
        {
            return;
        }

        var field = await GetOrCreateFieldAsync(fieldName, displayName, fieldType, category, isRequired, cancellationToken);
        await AddMetadataValueAsync(fileId, field.Id, valueText, cancellationToken);
    }

    public Task DeleteMetadataValueAsync(string fileId, string fieldName, string? valueText = null, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeFieldName(fieldName);
        _metadataValues.RemoveAll(value =>
            value.FileId == fileId &&
            string.Equals(value.FieldName, normalized, StringComparison.OrdinalIgnoreCase) &&
            (valueText is null || string.Equals(value.ValueText, valueText, StringComparison.OrdinalIgnoreCase)));
        return Task.CompletedTask;
    }

    public Task CreateRelationshipAsync(string sourceId, string targetId, string relationType)
    {
        return TryCreateRelationshipAsync(sourceId, targetId, relationType);
    }

    public Task<bool> TryCreateRelationshipAsync(string sourceId, string targetId, string relationType)
    {
        if (string.IsNullOrWhiteSpace(sourceId) ||
            string.IsNullOrWhiteSpace(targetId) ||
            sourceId == targetId ||
            string.IsNullOrWhiteSpace(relationType) ||
            _relationships.Any(item => item.SourceFileId == sourceId && item.TargetFileId == targetId && item.RelationshipType == relationType))
        {
            return Task.FromResult(false);
        }

        var source = _files.FirstOrDefault(file => file.Id == sourceId);
        var target = _files.FirstOrDefault(file => file.Id == targetId);
        if (source is null || target is null)
        {
            return Task.FromResult(false);
        }

        _relationships.Add(new FileRelationship
        {
            Id = _nextRelationshipId++,
            SourceFileId = sourceId,
            TargetFileId = targetId,
            RelationshipType = relationType,
            CreatedAt = DateTime.UtcNow,
            SourceFileName = source.FileName,
            TargetFileName = target.FileName
        });

        return Task.FromResult(true);
    }

    public Task<bool> RelationshipExistsAsync(string sourceId, string targetId, string relationType)
    {
        return Task.FromResult(_relationships.Any(item =>
            item.SourceFileId == sourceId &&
            item.TargetFileId == targetId &&
            item.RelationshipType == relationType));
    }

    public Task<bool> UpdateRelationshipTypeAsync(int id, string relationType)
    {
        var relationship = _relationships.FirstOrDefault(item => item.Id == id);
        if (relationship is null || string.IsNullOrWhiteSpace(relationType))
        {
            return Task.FromResult(false);
        }

        relationship.RelationshipType = relationType.Trim();
        return Task.FromResult(true);
    }

    public Task<bool> DeleteRelationshipAsync(int id)
    {
        return Task.FromResult(_relationships.RemoveAll(item => item.Id == id) > 0);
    }

    public Task<IEnumerable<FileRelationship>> GetRelationshipsByFileIdAsync(string fileId)
    {
        return Task.FromResult<IEnumerable<FileRelationship>>(_relationships
            .Where(item => item.SourceFileId == fileId || item.TargetFileId == fileId)
            .Select(CloneRelationship)
            .ToList());
    }

    public Task<IEnumerable<FileRelationship>> GetAllRelationshipsAsync()
    {
        return Task.FromResult<IEnumerable<FileRelationship>>(_relationships.Select(CloneRelationship).ToList());
    }

    public Task SaveAsync(ExportJobRecord job, CancellationToken cancellationToken = default)
    {
        _exportJobs.Insert(0, job);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExportJobRecord>> GetRecentAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<ExportJobRecord>>(_exportJobs.Take(limit).ToList());
    }

    public Task SaveAsync(ImportJobRecord job, CancellationToken cancellationToken = default)
    {
        _importJobs.Insert(0, job);
        return Task.CompletedTask;
    }

    Task<IReadOnlyList<ImportJobRecord>> IImportJobRepository.GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<ImportJobRecord>>(_importJobs.Take(limit).ToList());
    }

    public string GetPreviewPath(ExportFormat format, string requestedFileName)
    {
        var extension = format switch
        {
            ExportFormat.Csv => ".csv",
            ExportFormat.Json => ".json",
            ExportFormat.DublinCoreXml => ".xml",
            _ => ".txt"
        };
        var fileName = string.IsNullOrWhiteSpace(requestedFileName) ? $"archiveflow-demo-export{extension}" : requestedFileName;
        return $"browser-download://{fileName}";
    }

    public Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken = default)
    {
        LastExportContent = request.Format switch
        {
            ExportFormat.Json => BuildJson(request.Files),
            ExportFormat.DublinCoreXml => BuildDublinCoreXml(request.Files),
            _ => BuildCsv(request.Files)
        };

        var outputPath = GetPreviewPath(request.Format, request.RequestedFileName);
        var job = ExportJobRecord.Create(request.Format.ToString(), request.Files.Count, outputPath, "Completed", "Browser demo export content generated in memory.");
        _exportJobs.Insert(0, job);

        return Task.FromResult(new ExportResult
        {
            JobId = job.Id,
            Success = true,
            Format = request.Format,
            FileCount = request.Files.Count,
            OutputPath = outputPath,
            Message = "Browser demo export generated. Use the preview panel content for portfolio review."
        });
    }

    public Task<ImportPreviewResult> PreviewFolderAsync(string folderPath, bool recursive, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var records = new[]
        {
            FileRecord.CreateVirtual("browser-import://oral-history-transcript.txt", "demo-import-001", 18_400, "text/plain", now.AddMinutes(-6)),
            FileRecord.CreateVirtual("browser-import://catalog-crosswalk.csv", "demo-import-002", 9_260, "text/csv", now.AddMinutes(-5)),
            FileRecord.CreateVirtual("browser-import://visual-reference.png", "demo-import-003", 240_000, "image/png", now.AddMinutes(-4)),
            FileRecord.CreateVirtual("browser-import://duplicate-research-brief.pdf", _files.First().FileHash, 98_000, "application/pdf", now.AddMinutes(-3))
        };

        var items = records.Select(record =>
        {
            var existing = _files.FirstOrDefault(file => string.Equals(file.FileHash, record.FileHash, StringComparison.OrdinalIgnoreCase));
            return new ImportPreviewItem
            {
                FileRecord = existing is null ? record : null,
                FilePath = record.FilePath,
                FileName = record.FileName,
                FileExtension = record.FileExtension,
                FileSize = record.FileSize,
                FileHash = record.FileHash,
                Status = existing is null ? ImportPreviewStatus.New : ImportPreviewStatus.Duplicate,
                ExistingArchiveId = existing?.ArchiveId ?? string.Empty,
                Message = existing is null ? "Ready for demo import." : $"Duplicate of {existing.FileName}."
            };
        }).ToList();

        return Task.FromResult(new ImportPreviewResult
        {
            JobId = Guid.NewGuid().ToString("N"),
            FolderPath = folderPath,
            Recursive = recursive,
            Items = items
        });
    }

    public async Task<ImportApplyResult> ApplyImportAsync(ImportPreviewResult preview, CancellationToken cancellationToken = default)
    {
        var imported = 0;
        foreach (var item in preview.Items.Where(item => item.CanImport))
        {
            await SaveAsync(item.FileRecord!, cancellationToken);
            await SetMetadataValueAsync(item.FileRecord!.Id, "status", "Status", "String", "Basic", "Imported", false, cancellationToken);
            imported++;
        }

        var skipped = preview.Items.Count - imported;
        _importJobs.Insert(0, ImportJobRecord.Create(
            preview.FolderPath,
            preview.Recursive,
            preview.TotalCount,
            preview.NewCount,
            preview.DuplicateCount,
            preview.ExistingCount,
            imported,
            0,
            "Completed",
            $"Browser demo import completed: {imported} imported, {skipped} skipped.",
            finishedAt: DateTime.UtcNow));

        return new ImportApplyResult
        {
            JobId = preview.JobId,
            ImportedCount = imported,
            SkippedCount = skipped
        };
    }

    private void SeedBaseData()
    {
        var now = DateTime.UtcNow;
        var files = new[]
        {
            FileRecord.CreateVirtual("browser-demo://research/ai-research-brief.pdf", "demo-001", 420_000, "application/pdf", now.AddDays(-14)),
            FileRecord.CreateVirtual("browser-demo://notes/metadata-workshop-notes.md", "demo-002", 24_600, "text/markdown", now.AddDays(-10)),
            FileRecord.CreateVirtual("browser-demo://datasets/catalog-crosswalk.csv", "demo-003", 82_100, "text/csv", now.AddDays(-8)),
            FileRecord.CreateVirtual("browser-demo://images/archive-diagram.png", "demo-004", 318_000, "image/png", now.AddDays(-5)),
            FileRecord.CreateVirtual("browser-demo://assets/exhibit-model.glb", "demo-005", 1_280_000, "model/gltf-binary", now.AddDays(-3)),
            FileRecord.CreateVirtual("browser-demo://references/source-bibliography.bib", "demo-006", 17_200, "text/plain", now.AddDays(-2)),
            FileRecord.CreateVirtual("browser-demo://documents/missing-subject-report.docx", "demo-007", 212_000, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", now.AddDays(-1))
        };

        _files.AddRange(files);

        foreach (var file in files)
        {
            file.UpdatePreview($"{file.FileName} sample content for online workflow, full-text search, metadata review, and export demonstration.");
        }

        SetMetadataValueAsync(files[0].Id, "title", "Title", "String", "Descriptive", "AI Research Brief").GetAwaiter().GetResult();
        SetMetadataValueAsync(files[0].Id, "subject", "Subject", "String", "Descriptive", "Artificial Intelligence").GetAwaiter().GetResult();
        AddMetadataValueIfMissingAsync(files[0].Id, "tag", "Tag", "String", "Descriptive", "research").GetAwaiter().GetResult();
        AddMetadataValueIfMissingAsync(files[1].Id, "tag", "Tag", "String", "Descriptive", "metadata").GetAwaiter().GetResult();
        AddMetadataValueIfMissingAsync(files[2].Id, "tag", "Tag", "String", "Descriptive", "dataset").GetAwaiter().GetResult();
        AddMetadataValueIfMissingAsync(files[3].Id, "tag", "Tag", "String", "Descriptive", "visual").GetAwaiter().GetResult();
        AddMetadataValueIfMissingAsync(files[4].Id, "tag", "Tag", "String", "Descriptive", "asset").GetAwaiter().GetResult();
        SetMetadataValueAsync(files[5].Id, "subject", "Subject", "String", "Descriptive", "Bibliography").GetAwaiter().GetResult();
        SetMetadataValueAsync(files[6].Id, "status", "Status", "String", "Basic", "Needs Metadata").GetAwaiter().GetResult();

        TryCreateRelationshipAsync(files[0].Id, files[5].Id, "cites").GetAwaiter().GetResult();
        TryCreateRelationshipAsync(files[1].Id, files[2].Id, "describes").GetAwaiter().GetResult();
    }

    private static string NormalizeFieldName(string fieldName)
    {
        return fieldName.Trim().ToLowerInvariant().Replace(' ', '_');
    }

    private static MetadataValue CloneMetadataValue(MetadataValue value)
    {
        return new MetadataValue
        {
            Id = value.Id,
            FileId = value.FileId,
            FieldId = value.FieldId,
            ValueText = value.ValueText,
            CreatedAt = value.CreatedAt,
            UpdatedAt = value.UpdatedAt,
            FieldName = value.FieldName,
            DisplayName = value.DisplayName,
            FieldType = value.FieldType,
            Category = value.Category,
            IsRequired = value.IsRequired,
            SortOrder = value.SortOrder
        };
    }

    private static FileRelationship CloneRelationship(FileRelationship relationship)
    {
        return new FileRelationship
        {
            Id = relationship.Id,
            SourceFileId = relationship.SourceFileId,
            TargetFileId = relationship.TargetFileId,
            RelationshipType = relationship.RelationshipType,
            CreatedAt = relationship.CreatedAt,
            SourceFileName = relationship.SourceFileName,
            TargetFileName = relationship.TargetFileName
        };
    }

    private static string BuildCsv(IReadOnlyList<FileRecord> files)
    {
        var builder = new StringBuilder();
        builder.AppendLine("archive_id,file_name,extension,size,path");
        foreach (var file in files)
        {
            builder.AppendLine($"{EscapeCsv(file.ArchiveId)},{EscapeCsv(file.FileName)},{EscapeCsv(file.FileExtension)},{file.FileSize},{EscapeCsv(file.FilePath)}");
        }

        return builder.ToString();
    }

    private static string BuildJson(IReadOnlyList<FileRecord> files)
    {
        var builder = new StringBuilder();
        builder.AppendLine("[");
        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            builder.AppendLine("  {");
            builder.AppendLine($"    \"archiveId\": \"{EscapeJson(file.ArchiveId)}\",");
            builder.AppendLine($"    \"fileName\": \"{EscapeJson(file.FileName)}\",");
            builder.AppendLine($"    \"extension\": \"{EscapeJson(file.FileExtension)}\",");
            builder.AppendLine($"    \"size\": {file.FileSize},");
            builder.AppendLine($"    \"path\": \"{EscapeJson(file.FilePath)}\",");
            builder.AppendLine($"    \"preview\": \"{EscapeJson(file.ContentPreview)}\"");
            builder.Append(index == files.Count - 1 ? "  }" : "  },");
            builder.AppendLine();
        }

        builder.AppendLine("]");
        return builder.ToString();
    }

    private static string BuildDublinCoreXml(IReadOnlyList<FileRecord> files)
    {
        var document = new XDocument(
            new XElement("records",
                files.Select(file => new XElement("record",
                    new XElement("identifier", file.ArchiveId),
                    new XElement("title", Path.GetFileNameWithoutExtension(file.FileName)),
                    new XElement("format", file.MimeType),
                    new XElement("source", file.FilePath)))));
        return document.ToString();
    }

    private static string EscapeCsv(string value)
    {
        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
    }
}
