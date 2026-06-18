using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// A lightweight, rule-based auto-tagging service that scans file names and content previews.
/// Designed to be easily replaced with a real ML/AI model in the future.
/// </summary>
public class LocalKeywordTaggingService : IAutoTaggingService
{
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<LocalKeywordTaggingService> _logger;

    // Keyword dictionary: maps detected terms to standardized tags
    private static readonly Dictionary<string, string> KeywordTagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "csharp", "CSharp" }, { ".cs", "CSharp" }, { "dotnet", "DotNet" },
        { "python", "Python" }, { ".py", "Python" },
        { "javascript", "JavaScript" }, { ".js", "JavaScript" }, { "react", "React" },
        { "machine learning", "ML" }, { "ai", "ArtificialIntelligence" }, { "llm", "LLM" },
        { "design", "Design" }, { "ui", "UI" }, { "ux", "UX" },
        { "research", "Research" }, { "paper", "Academic" }, { "thesis", "Academic" },
        { "invoice", "Finance" }, { "budget", "Finance" }, { "receipt", "Finance" },
        { "photo", "Media" }, { "video", "Media" }, { "audio", "Media" },
        { "archive", "Archived" }, { "backup", "Backup" }
    };

    public LocalKeywordTaggingService(IMetadataRepository metadataRepository, ILogger<LocalKeywordTaggingService> logger)
    {
        _metadataRepository = metadataRepository;
        _logger = logger;
    }

    public async Task ApplyAutoTagsAsync(FileRecord file)
    {
        try
        {
            // Combine filename and content preview for analysis
            string textToAnalyze = $"{file.FileName} {file.ContentPreview ?? string.Empty}".ToLowerInvariant();
            var detectedTags = new HashSet<string>();

            // Scan text against keyword dictionary
            foreach (var kvp in KeywordTagMap)
            {
                if (textToAnalyze.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    detectedTags.Add(kvp.Value);
                }
            }

            // Apply detected tags to metadata
            if (detectedTags.Count > 0)
            {
                var tagField = await _metadataRepository.GetOrCreateFieldAsync("tag", "Tag");
                if (tagField != null)
                {
                    foreach (var tag in detectedTags)
                    {
                        await _metadataRepository.AddMetadataValueAsync(file.Id, tagField.Id, tag);
                        _logger.LogDebug("Auto-tagged file {FileName} with tag: {Tag}", file.FileName, tag);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-tag file {FileName}", file.FileName);
        }
    }
}