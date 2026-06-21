using System.Data;
using System.Text;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using ArchiveFlow.Domain.Enums;
using ArchiveFlow.Infrastructure.Database;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.MockData;

/// <summary>
/// Generates a realistic local mock archive for development and workflow testing.
/// It clears database records and recreates files under Data/mock-files.
/// </summary>
public sealed class MockArchiveSeeder : IMockArchiveSeeder
{
    private readonly IDatabaseConnectionFactory _connectionFactory;
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly IFileHashingService _hashingService;
    private readonly ILogger<MockArchiveSeeder> _logger;

    public MockArchiveSeeder(
        IDatabaseConnectionFactory connectionFactory,
        IFileRepository fileRepository,
        IMetadataRepository metadataRepository,
        IFileHashingService hashingService,
        ILogger<MockArchiveSeeder> logger)
    {
        _connectionFactory = connectionFactory;
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _hashingService = hashingService;
        _logger = logger;
    }

    public async Task<MockArchiveSeedResult> ResetAndGenerateAsync(
        int fileCount = 420,
        CancellationToken cancellationToken = default)
    {
        var result = new MockArchiveSeedResult
        {
            MockRootPath = ResolveMockRootPath()
        };

        _logger.LogInformation("Resetting mock archive data.");

        await ClearArchiveTablesAsync(cancellationToken);
        ResetMockFolder(result.MockRootPath);

        var specs = CreateSpecs();
        var hashCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var index = 1; index <= fileCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var spec = specs[(index - 1) % specs.Count];
            var isDuplicate = index > 30 && index % 29 == 0;
            var duplicateKey = isDuplicate
                ? $"duplicate-group-{index % 7}"
                : $"unique-{index}";

            var project = ResolveProject(index);
            var year = 2020 + index % 7;
            var folder = Path.Combine(result.MockRootPath, spec.Folder, year.ToString(), Sanitize(project));
            Directory.CreateDirectory(folder);

            var fileName = $"{index:0000}-{Sanitize(spec.Subject)}-{Sanitize(spec.Category)}{spec.Extension}";
            var filePath = Path.Combine(folder, fileName);

            await WriteMockFileAsync(filePath, spec, index, project, duplicateKey, cancellationToken);

            var fileInfo = new FileInfo(filePath);
            var hash = await _hashingService.ComputeSha256HashAsync(filePath, cancellationToken);

            var record = FileRecord.Create(
                filePath,
                hash,
                fileInfo.Length,
                spec.MimeType);

            record.UpdateStatus(ResolveStatus(index, isDuplicate));

            await _fileRepository.SaveAsync(record, cancellationToken);

            result.FileCount++;

            if (!result.ExtensionCounts.ContainsKey(spec.Extension))
            {
                result.ExtensionCounts[spec.Extension] = 0;
            }

            result.ExtensionCounts[spec.Extension]++;

            if (!hashCounts.ContainsKey(hash))
            {
                hashCounts[hash] = 0;
            }

            hashCounts[hash]++;

            result.MetadataValueCount += await AddMetadataForFileAsync(
                record,
                spec,
                index,
                project,
                cancellationToken);
        }

        result.DuplicateGroupCount = hashCounts.Count(x => x.Value > 1);

        _logger.LogInformation(
            "Mock archive generated. Files: {FileCount}, Metadata values: {MetadataCount}, Duplicate groups: {DuplicateGroups}",
            result.FileCount,
            result.MetadataValueCount,
            result.DuplicateGroupCount);

        return result;
    }

    private async Task ClearArchiveTablesAsync(CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var existingTables = (await connection.QueryAsync<string>(
                "SELECT name FROM sqlite_master WHERE type = 'table';"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        using var transaction = connection.BeginTransaction();

        if (existingTables.Contains("metadata_values"))
        {
            await connection.ExecuteAsync("DELETE FROM metadata_values;", transaction: transaction);
        }

        if (existingTables.Contains("metadata_fields"))
        {
            await connection.ExecuteAsync("DELETE FROM metadata_fields;", transaction: transaction);
        }

        if (existingTables.Contains("files"))
        {
            await connection.ExecuteAsync("DELETE FROM files;", transaction: transaction);
        }

        transaction.Commit();
    }

    private string ResolveMockRootPath()
    {
        var dataDirectory = Path.GetDirectoryName(_connectionFactory.DatabasePath)
            ?? Path.Combine(Directory.GetCurrentDirectory(), "Data");

        return Path.Combine(dataDirectory, "mock-files");
    }

    private static void ResetMockFolder(string mockRootPath)
    {
        if (Directory.Exists(mockRootPath))
        {
            Directory.Delete(mockRootPath, recursive: true);
        }

        Directory.CreateDirectory(mockRootPath);
    }

    private async Task WriteMockFileAsync(
        string filePath,
        MockFileSpec spec,
        int index,
        string project,
        string duplicateKey,
        CancellationToken cancellationToken)
    {
        var content = BuildMockContent(spec, index, project, duplicateKey);
        var bytes = Encoding.UTF8.GetBytes(content);

        await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
    }

    private static string BuildMockContent(
        MockFileSpec spec,
        int index,
        string project,
        string duplicateKey)
    {
        if (duplicateKey.StartsWith("duplicate-group-", StringComparison.OrdinalIgnoreCase))
        {
            return $"""
            ArchiveFlow duplicate test payload
            DuplicateKey: {duplicateKey}
            This content is intentionally repeated to produce duplicate hashes.
            """;
        }

        return $"""
        ArchiveFlow Mock File
        Index: {index}
        Category: {spec.Category}
        Subject: {spec.Subject}
        Project: {project}
        Extension: {spec.Extension}
        MimeType: {spec.MimeType}
        Tags: {string.Join(", ", spec.Tags)}

        This is realistic mock content for ArchiveFlow Studio.
        It is used to test file sources, metadata filters, keyword search,
        duplicate detection, missing metadata detection, and archive health workflows.

        Research notes:
        - Digital archive
        - Metadata workflow
        - Personal knowledge management
        - Node-based file organization
        - Dublin Core inspired descriptive metadata
        """;
    }

    private async Task<int> AddMetadataForFileAsync(
        FileRecord record,
        MockFileSpec spec,
        int index,
        string project,
        CancellationToken cancellationToken)
    {
        var count = 0;

        count += await AddMetadataAsync(
            record.Id,
            "archive_id",
            "Archive ID",
            "String",
            "Basic",
            record.ArchiveId,
            isRequired: true,
            cancellationToken);

        count += await AddMetadataAsync(
            record.Id,
            "format",
            "Format",
            "String",
            "Technical Metadata",
            spec.Extension,
            isRequired: false,
            cancellationToken);

        count += await AddMetadataAsync(
            record.Id,
            "mime_type",
            "MIME Type",
            "String",
            "Technical Metadata",
            spec.MimeType,
            isRequired: false,
            cancellationToken);

        var intentionallyMissingCoreMetadata = index % 13 == 0;

        if (!intentionallyMissingCoreMetadata)
        {
            count += await AddMetadataAsync(
                record.Id,
                "title",
                "Title",
                "String",
                "Descriptive Metadata",
                $"{spec.Subject} Resource {index:0000}",
                isRequired: true,
                cancellationToken);

            count += await AddMetadataAsync(
                record.Id,
                "subject",
                "Subject",
                "String",
                "Descriptive Metadata",
                spec.Subject,
                isRequired: true,
                cancellationToken);
        }

        if (index % 5 != 0)
        {
            count += await AddMetadataAsync(
                record.Id,
                "description",
                "Description",
                "LongText",
                "Descriptive Metadata",
                $"Mock {spec.Category} file for {spec.Subject} in project {project}.",
                isRequired: false,
                cancellationToken);
        }

        if (index % 7 != 0)
        {
            count += await AddMetadataAsync(
                record.Id,
                "project",
                "Project",
                "String",
                "Personal Knowledge",
                project,
                isRequired: false,
                cancellationToken);
        }

        if (index % 4 != 0)
        {
            count += await AddMetadataAsync(
                record.Id,
                "reading_status",
                "Reading Status",
                "String",
                "Personal Knowledge",
                ResolveReadingStatus(index),
                isRequired: false,
                cancellationToken);
        }

        count += await AddMetadataAsync(
            record.Id,
            "importance",
            "Importance",
            "String",
            "Personal Knowledge",
            ResolveImportance(index),
            isRequired: false,
            cancellationToken);

        foreach (var tag in spec.Tags.Take(index % 3 + 1))
        {
            count += await AddMetadataAsync(
                record.Id,
                "tag",
                "Tag",
                "String",
                "Personal Knowledge",
                tag,
                isRequired: false,
                cancellationToken);
        }

        return count;
    }

    private async Task<int> AddMetadataAsync(
        string fileId,
        string fieldName,
        string displayName,
        string fieldType,
        string category,
        string valueText,
        bool isRequired,
        CancellationToken cancellationToken)
    {
        await _metadataRepository.AddMetadataValueIfMissingAsync(
            fileId,
            fieldName,
            displayName,
            fieldType,
            category,
            valueText,
            isRequired,
            cancellationToken);

        return 1;
    }

    private static FileStatus ResolveStatus(int index, bool isDuplicate)
    {
        if (isDuplicate)
        {
            return FileStatus.Duplicate;
        }

        if (index % 13 == 0)
        {
            return FileStatus.Incomplete;
        }

        if (index % 5 == 0)
        {
            return FileStatus.Archived;
        }

        return FileStatus.Scanned;
    }

    private static string ResolveProject(int index)
    {
        var projects = new[]
        {
            "ArchiveFlow Studio",
            "AI Research Library",
            "Digital Humanities Notes",
            "Campus Game Assets",
            "Personal Knowledge Base",
            "Library Science Archive",
            "Metadata Workflow Lab",
            "Reading Pipeline"
        };

        return projects[index % projects.Length];
    }

    private static string ResolveReadingStatus(int index)
    {
        var values = new[]
        {
            "To Read",
            "Reading",
            "Read",
            "Reference",
            "Not Applicable"
        };

        return values[index % values.Length];
    }

    private static string ResolveImportance(int index)
    {
        var values = new[]
        {
            "Low",
            "Normal",
            "High",
            "Critical"
        };

        return values[index % values.Length];
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray());

        return cleaned
            .Replace(" ", "-")
            .Replace("/", "-")
            .Replace("\\", "-")
            .ToLowerInvariant();
    }

    private static IReadOnlyList<MockFileSpec> CreateSpecs()
    {
        return new List<MockFileSpec>
        {
            new(".txt", "Document", "text/plain", "documents", "Metadata", new[] { "metadata", "archive", "text" }),
            new(".md", "Note", "text/markdown", "notes", "Personal Knowledge", new[] { "note", "markdown", "pkm" }),
            new(".pdf", "Research Paper", "application/pdf", "research", "AI", new[] { "ai", "paper", "research" }),
            new(".docx", "Office Document", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "documents", "Library Science", new[] { "report", "library", "office" }),
            new(".pptx", "Presentation", "application/vnd.openxmlformats-officedocument.presentationml.presentation", "presentations", "Digital Humanities", new[] { "slides", "teaching", "dh" }),
            new(".xlsx", "Spreadsheet", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "datasets", "Statistics", new[] { "spreadsheet", "stats", "data" }),
            new(".csv", "Dataset", "text/csv", "datasets", "Metadata", new[] { "csv", "dataset", "metadata" }),
            new(".json", "Structured Data", "application/json", "datasets", "Workflow", new[] { "json", "workflow", "data" }),
            new(".xml", "Metadata Export", "application/xml", "exports", "Dublin Core", new[] { "xml", "dublin-core", "export" }),
            new(".html", "Web Page", "text/html", "web", "Web Archive", new[] { "html", "web", "archive" }),
            new(".css", "Stylesheet", "text/css", "code", "Frontend", new[] { "css", "frontend", "code" }),
            new(".js", "JavaScript", "application/javascript", "code", "Programming", new[] { "javascript", "code", "frontend" }),
            new(".ts", "TypeScript", "application/typescript", "code", "Programming", new[] { "typescript", "code", "frontend" }),
            new(".cs", "CSharp Code", "text/x-csharp", "code", "C# Desktop App", new[] { "csharp", "avalonia", "code" }),
            new(".py", "Python Code", "text/x-python", "code", "Data Processing", new[] { "python", "data", "script" }),
            new(".java", "Java Code", "text/x-java", "code", "Programming", new[] { "java", "code", "backend" }),
            new(".cpp", "CPlusPlus Code", "text/x-c++", "code", "Programming", new[] { "cpp", "systems", "code" }),
            new(".h", "Header File", "text/x-c", "code", "Programming", new[] { "header", "systems", "code" }),
            new(".png", "Image", "image/png", "images", "Visual Reference", new[] { "image", "reference", "visual" }),
            new(".jpg", "Image", "image/jpeg", "images", "Visual Reference", new[] { "photo", "image", "visual" }),
            new(".webp", "Image", "image/webp", "images", "Web Asset", new[] { "webp", "asset", "image" }),
            new(".svg", "Vector Image", "image/svg+xml", "images", "Icon Design", new[] { "svg", "icon", "vector" }),
            new(".mp3", "Audio", "audio/mpeg", "audio", "Oral History", new[] { "audio", "oral-history", "media" }),
            new(".wav", "Audio", "audio/wav", "audio", "Sound Archive", new[] { "audio", "sound", "media" }),
            new(".mp4", "Video", "video/mp4", "video", "Lecture Recording", new[] { "video", "lecture", "media" }),
            new(".mov", "Video", "video/quicktime", "video", "Field Recording", new[] { "video", "recording", "media" }),
            new(".blend", "3D Asset", "application/octet-stream", "3d-assets", "Blender Asset", new[] { "3d", "blender", "asset" }),
            new(".fbx", "3D Export", "application/octet-stream", "3d-assets", "Game Asset", new[] { "3d", "fbx", "game" }),
            new(".obj", "3D Model", "application/octet-stream", "3d-assets", "3D Model", new[] { "3d", "obj", "model" }),
            new(".glb", "3D Model", "model/gltf-binary", "3d-assets", "Web 3D", new[] { "3d", "glb", "web" }),
            new(".stl", "3D Print", "model/stl", "3d-assets", "3D Printing", new[] { "3d", "stl", "printing" }),
            new(".zip", "Archive Package", "application/zip", "archives", "Archive Package", new[] { "zip", "package", "backup" }),
            new(".7z", "Archive Package", "application/x-7z-compressed", "archives", "Archive Package", new[] { "7z", "package", "backup" }),
            new(".sqlite", "Database", "application/vnd.sqlite3", "databases", "SQLite", new[] { "sqlite", "database", "local" }),
            new(".db", "Database", "application/octet-stream", "databases", "Database", new[] { "database", "index", "local" }),
            new(".bib", "Bibliography", "text/x-bibtex", "references", "Bibliography", new[] { "bibtex", "citation", "reference" }),
            new(".ris", "Citation Export", "application/x-research-info-systems", "references", "Citation", new[] { "ris", "citation", "reference" }),
            new(".epub", "Ebook", "application/epub+zip", "ebooks", "Reading", new[] { "ebook", "reading", "book" })
        };
    }

    private sealed record MockFileSpec(
        string Extension,
        string Category,
        string MimeType,
        string Folder,
        string Subject,
        string[] Tags);
}