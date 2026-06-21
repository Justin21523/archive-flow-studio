using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using ArchiveFlow.Domain.Enums;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Generates a massive amount of realistic mock files and database records 
/// to simulate various archive scenarios (different types, sizes, metadata, content).
/// </summary>
public class MockDataService : IMockDataService
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ISearchService _searchService;
    private readonly ILogger<MockDataService> _logger;
    private readonly string _mockFilesDir;

    // Predefined keywords for auto-tagging simulation
    private static readonly string[] _techKeywords = { "csharp", "python", "javascript", "react", "machine learning", "ai", "llm", "database", "sql", "api" };
    private static readonly string[] _academicKeywords = { "research", "paper", "thesis", "study", "analysis", "experiment", "data" };
    private static readonly string[] _mediaKeywords = { "photo", "video", "audio", "design", "ui", "ux", "graphic" };

    public MockDataService(
        IFileRepository fileRepository,
        IMetadataRepository metadataRepository,
        ISearchService searchService,
        ILogger<MockDataService> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _searchService = searchService;
        _logger = logger;
        _mockFilesDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "MockFiles");
        Directory.CreateDirectory(_mockFilesDir);
    }

    public async Task GenerateMockDataAsync()
    {
        _logger.LogInformation("Starting massive mock data generation...");

        // 1. Initialize Standard Metadata Fields (Dublin Core + Personal)
        await InitializeStandardFieldsAsync();

        // 1. Generate Mock Images (50 files)
        await GenerateMockImagesAsync(50);

        // 2. Generate Mock Text/Code Files (100 files)
        await GenerateMockTextFilesAsync(100);

        // 3. Generate Mock Large Files (10 files, simulated)
        await GenerateMockLargeFilesAsync(10);

        // 4. Generate Mock 3D/Media Assets (20 files)
        await GenerateMockAssetFilesAsync(20);

        _logger.LogInformation("Massive mock data generation completed.");
    }
    
    private async Task InitializeStandardFieldsAsync()
    {
        // Descriptive Metadata (Dublin Core)
        await _metadataRepository.GetOrCreateFieldAsync("title", "Title", "String", "Descriptive", true);
        await _metadataRepository.GetOrCreateFieldAsync("creator", "Creator", "String", "Descriptive", false);
        await _metadataRepository.GetOrCreateFieldAsync("subject", "Subject", "String", "Descriptive", true);
        await _metadataRepository.GetOrCreateFieldAsync("description", "Description", "String", "Descriptive", false);
        
        // Personal Knowledge
        await _metadataRepository.GetOrCreateFieldAsync("tag", "Tag", "String", "Personal", false);
        await _metadataRepository.GetOrCreateFieldAsync("project", "Project", "String", "Personal", false);
        await _metadataRepository.GetOrCreateFieldAsync("reading_status", "Reading Status", "String", "Personal", false);
        
        // Technical
        await _metadataRepository.GetOrCreateFieldAsync("format", "Format", "String", "Technical", false);
        await _metadataRepository.GetOrCreateFieldAsync("identifier", "Identifier", "String", "Technical", true);

        _logger.LogInformation("Standard metadata fields initialized.");
    }

    private async Task GenerateMockImagesAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var fileName = $"photo_{i:D3}.png";
            var filePath = Path.Combine(_mockFilesDir, fileName);
            
            // Generate a small image file
            if (!File.Exists(filePath))
            {
                var color = Rgba32.ParseHex($"#{i * 137 % 256:X2}{i * 97 % 256:X2}{i * 57 % 256:X2}");
                using var image = new Image<Rgba32>(100 + (i % 50), 100 + (i % 50), color);
                await image.SaveAsPngAsync(filePath);
            }

            var record = await CreateFileRecordAsync(filePath, "image/png");
            record.UpdateContentPreview($"Image file {fileName}. Contains visual data.");
            await SaveGeneratedRecordAsync(record);
            
            // Add realistic metadata
            await AddMetadataAsync(record.Id, "tag", i % 2 == 0 ? "Media" : "Archive");
            await AddMetadataAsync(record.Id, "subject", "Visual Assets");
        }
    }

    private async Task GenerateMockTextFilesAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            string fileName;
            string content;
            string mimeType;

            // Vary file types
            if (i % 3 == 0)
            {
                fileName = $"document_{i:D3}.txt";
                mimeType = "text/plain";
                content = GenerateTextContent(i, _academicKeywords);
            }
            else if (i % 3 == 1)
            {
                fileName = $"script_{i:D3}.py";
                mimeType = "text/x-python";
                content = GenerateCodeContent(i, "python");
            }
            else
            {
                fileName = $"code_{i:D3}.cs";
                mimeType = "text/x-csharp";
                content = GenerateCodeContent(i, "csharp");
            }

            var filePath = Path.Combine(_mockFilesDir, fileName);
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, content);
            }

            var record = await CreateFileRecordAsync(filePath, mimeType);
            record.UpdateContentPreview(content.Substring(0, Math.Min(content.Length, 500))); // Store preview
            await SaveGeneratedRecordAsync(record);

            // Add metadata based on content keywords
            var tags = ExtractTags(content);
            foreach (var tag in tags)
            {
                await AddMetadataAsync(record.Id, "tag", tag);
            }
            await AddMetadataAsync(record.Id, "subject", i % 2 == 0 ? "Development" : "Research");
        }
    }

    private async Task GenerateMockLargeFilesAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var fileName = $"dataset_{i:D3}.csv";
            var filePath = Path.Combine(_mockFilesDir, fileName);
            
            // Create a file with simulated large size (we just write some text, but record large size in DB for testing)
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, "id,value,description\n" + string.Join("\n", Enumerable.Range(1, 100).Select(x => $"{x},{x*10},Item {x}")));
            }

            var record = await CreateFileRecordAsync(filePath, "text/csv");
            // Simulate large file size in DB
            record.UpdateFileSize(50_000_000 + (i * 1_000_000)); // 50MB+
            record.UpdateContentPreview("Large dataset file. Preview truncated.");
            await SaveGeneratedRecordAsync(record);
            
            await AddMetadataAsync(record.Id, "tag", "Data");
            await AddMetadataAsync(record.Id, "tag", "LargeFile");
            await AddMetadataAsync(record.Id, "subject", "Analytics");
        }
    }

    private async Task GenerateMockAssetFilesAsync(int count)
    {
        var extensions = new[] { ".blend", ".fbx", ".obj", ".mp4", ".wav" };
        for (int i = 1; i <= count; i++)
        {
            var ext = extensions[i % extensions.Length];
            var fileName = $"asset_{i:D3}{ext}";
            var filePath = Path.Combine(_mockFilesDir, fileName);
            
            // Create a dummy file
            if (!File.Exists(filePath))
            {
                await File.WriteAllTextAsync(filePath, $"Dummy {ext} file content for testing.");
            }

            var mimeType = ext switch
            {
                ".blend" or ".fbx" or ".obj" => "application/octet-stream",
                ".mp4" => "video/mp4",
                ".wav" => "audio/wav",
                _ => "application/octet-stream"
            };

            var record = await CreateFileRecordAsync(filePath, mimeType);
            record.UpdateContentPreview($"3D/Media asset: {fileName}");
            await SaveGeneratedRecordAsync(record);
            
            await AddMetadataAsync(record.Id, "tag", "Asset");
            await AddMetadataAsync(record.Id, "tag", ext.TrimStart('.'));
            await AddMetadataAsync(record.Id, "subject", "3D Models & Media");
        }
    }

    // --- Helper Methods ---

    private async Task<FileRecord> CreateFileRecordAsync(string filePath, string mimeType)
    {
        var fileInfo = new FileInfo(filePath);
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filePath + DateTime.UtcNow.Ticks))).ToLowerInvariant();
        
        var record = FileRecord.Create(filePath, hash, fileInfo.Length, mimeType);
        // Randomize status for testing
        var statuses = new[] { FileStatus.New, FileStatus.Scanning, FileStatus.Scanned, FileStatus.Archived };
        var statusIndex = Math.Abs(record.Id.GetHashCode()) % statuses.Length;
        record.UpdateStatus(statuses[statusIndex]);
        
        await _fileRepository.SaveAsync(record);
        await _searchService.IndexFileAsync(record);
        return record;
    }

    private async Task SaveGeneratedRecordAsync(FileRecord record)
    {
        await _fileRepository.SaveAsync(record);
        await _searchService.IndexFileAsync(record);
    }

    private async Task AddMetadataAsync(string fileId, string fieldName, string value)
    {
        var field = await _metadataRepository.GetOrCreateFieldAsync(
            fieldName,
            fieldName,
            "String",
            fieldName.Equals("tag", StringComparison.OrdinalIgnoreCase) ? "Personal" : "Basic");
        if (field != null)
        {
            await _metadataRepository.AddMetadataValueAsync(fileId, field.Id, value);
        }
    }

    private string GenerateTextContent(int seed, string[] keywords)
    {
        var random = new Random(seed);
        var content = $"Document {seed}. This is a research paper about ";
        content += keywords[random.Next(keywords.Length)] + ". ";
        content += "It contains detailed analysis and experimental data. ";
        content += "The methodology involves comprehensive study of the subject. ";
        content += "Results indicate significant findings in the field. ";
        // Add more text to simulate a real document
        for(int i=0; i<10; i++) content += $"Paragraph {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. ";
        return content;
    }

    private string GenerateCodeContent(int seed, string language)
    {
        var content = $"// {language} code file {seed}\n";
        content += $"using System;\n\n using System.Collections.Generic; \n\n namespace Project{seed}\n{{\n";
        content += $"    public class Class{seed}\n    {{\n";
        content += $"        public void Method{seed}() {{\n";
        content += $"            Console.WriteLine(\"Hello from {language}!\");\n";
        content += $"            var data = new List<string>();\n";
        content += $"            // Processing logic here\n";
        content += $"        }}\n    }}\n}}";
        return content;
    }

    private List<string> ExtractTags(string content)
    {
        var tags = new List<string>();
        var lowerContent = content.ToLowerInvariant();
        foreach (var kw in _techKeywords) if (lowerContent.Contains(kw)) tags.Add(kw.Replace(" ", ""));
        foreach (var kw in _academicKeywords) if (lowerContent.Contains(kw)) tags.Add(kw.Replace(" ", ""));
        foreach (var kw in _mediaKeywords) if (lowerContent.Contains(kw)) tags.Add(kw.Replace(" ", ""));
        return tags.Distinct().ToList();
    }
}
