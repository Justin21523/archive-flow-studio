using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Export;

/// <summary>
/// Implements the generation of Simple Dublin Core XML documents.
/// Maps internal EAV metadata fields to the 15 standard DC elements.
/// </summary>
public class DublinCoreExportService : IDublinCoreExportService
{
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<DublinCoreExportService> _logger;

    // XML Namespaces for Dublin Core
    private static readonly XNamespace DcNamespace = "http://purl.org/dc/elements/1.1/";
    private static readonly XNamespace XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    // Mapping dictionary: Internal Field Name -> Dublin Core Element Name (lowercase)
    private static readonly Dictionary<string, string> MetadataToDcMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "title", "title" },
        { "creator", "creator" },
        { "author", "creator" }, // Alias
        { "subject", "subject" },
        { "keyword", "subject" }, // Alias
        { "tag", "subject" },     // Alias
        { "description", "description" },
        { "abstract", "description" }, // Alias
        { "publisher", "publisher" },
        { "contributor", "contributor" },
        { "date", "date" },
        { "created_at", "date" }, // Alias
        { "type", "type" },
        { "format", "format" },
        { "mime_type", "format" }, // Alias
        { "identifier", "identifier" },
        { "archive_id", "identifier" }, // Alias
        { "source", "source" },
        { "file_path", "source" }, // Alias
        { "language", "language" },
        { "relation", "relation" },
        { "coverage", "coverage" },
        { "rights", "rights" },
        { "license", "rights" } // Alias
    };

    public DublinCoreExportService(IMetadataRepository metadataRepository, ILogger<DublinCoreExportService> logger)
    {
        _metadataRepository = metadataRepository;
        _logger = logger;
    }

    public async Task ExportToDublinCoreXmlAsync(IEnumerable<FileRecord> files, string outputFilePath)
    {
        _logger.LogInformation("Starting Dublin Core XML export to {Path}", outputFilePath);

        // Create the root element with necessary namespaces
        var root = new XElement("archive",
            new XAttribute(XNamespace.Xmlns + "dc", DcNamespace.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "xsi", XsiNamespace.NamespaceName)
        );

        foreach (var file in files)
        {
            var recordElement = await BuildDcRecordAsync(file);
            root.Add(recordElement);
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);
        
        // Ensure directory exists
        var dir = Path.GetDirectoryName(outputFilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await Task.Run(() => doc.Save(outputFilePath));
        _logger.LogInformation("Dublin Core XML export completed successfully.");
    }

    /// <summary>
    /// Builds a single <record> element containing Dublin Core metadata for a specific file.
    /// </summary>
    private async Task<XElement> BuildDcRecordAsync(FileRecord file)
    {
        var record = new XElement("record");

        // 1. Map core FileRecord properties to DC elements
        AddDcElement(record, "identifier", file.ArchiveId);
        AddDcElement(record, "format", file.MimeType);
        AddDcElement(record, "type", MapFileTypeToDcType(file.FileExtension));
        AddDcElement(record, "source", file.FilePath);
        AddDcElement(record, "date", file.CreatedAt.ToString("o")); // ISO 8601 format

        // 2. Fetch and map dynamic EAV Metadata
        var metadataValues = await _metadataRepository.GetMetadataByFileIdAsync(file.Id);
        
        // Group by DC element name to handle multiple values for the same DC element (e.g., multiple subjects/tags)
        var groupedMetadata = metadataValues
            .Where(m => MetadataToDcMap.ContainsKey(m.FieldName))
            .GroupBy(m => MetadataToDcMap[m.FieldName]);

        foreach (var group in groupedMetadata)
        {
            foreach (var meta in group)
            {
                if (!string.IsNullOrWhiteSpace(meta.ValueText))
                {
                    AddDcElement(record, group.Key, meta.ValueText);
                }
            }
        }

        return record;
    }

    /// <summary>
    /// Helper to add a <dc:element> to the record.
    /// </summary>
    private void AddDcElement(XElement parent, string elementName, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        parent.Add(new XElement(DcNamespace + elementName, value));
    }

    /// <summary>
    /// Maps file extensions to standard Dublin Core Type vocabulary (DCMI Type Vocabulary).
    /// </summary>
    private string MapFileTypeToDcType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".svg" => "image",
            ".mp4" or ".avi" or ".mov" or ".mkv" => "moving image",
            ".mp3" or ".wav" or ".flac" => "sound",
            ".txt" or ".md" or ".pdf" or ".docx" => "text",
            ".csv" or ".json" or ".xml" => "dataset",
            ".blend" or ".fbx" or ".obj" or ".stl" => "physical object", // or "interactive resource"
            ".exe" or ".dll" or ".py" or ".cs" => "software",
            ".html" or ".htm" => "interactive resource",
            _ => "file"
        };
    }
}