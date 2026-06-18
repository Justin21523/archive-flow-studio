using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArchiveFlow.Infrastructure.Services;

/// <summary>
/// Generates mock files (images, text) and populates the database with sample records.
/// </summary>
public class MockDataService : IMockDataService
{
    private readonly IFileRepository _fileRepository;
    private readonly IMetadataRepository _metadataRepository;
    private readonly ILogger<MockDataService> _logger;
    private readonly string _mockFilesDir;

    public MockDataService(
        IFileRepository fileRepository,
        IMetadataRepository metadataRepository,
        ILogger<MockDataService> logger)
    {
        _fileRepository = fileRepository;
        _metadataRepository = metadataRepository;
        _logger = logger;
        _mockFilesDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "MockFiles");
        Directory.CreateDirectory(_mockFilesDir);
    }

    public async Task GenerateMockDataAsync()
    {
        // Check if data already exists to avoid duplicates
        var existingFiles = await _fileRepository.GetAllAsync();
        if (existingFiles.Any())
        {
            _logger.LogInformation("Database already contains data. Skipping mock data generation.");
            return;
        }

        _logger.LogInformation("Starting mock data generation...");

        // 1. Generate Mock Images
        var imagePaths = new[] { "photo1.png", "photo2.png", "photo3.png" };
        var colors = new[] { Rgba32.ParseHex("#FF5733"), Rgba32.ParseHex("#33FF57"), Rgba32.ParseHex("#3357FF") };

        for (int i = 0; i < imagePaths.Length; i++)
        {
            var path = Path.Combine(_mockFilesDir, imagePaths[i]);
            if (!File.Exists(path))
            {
                using var image = new Image<Rgba32>(200, 200, colors[i]);
                await image.SaveAsPngAsync(path);
            }
            await CreateFileRecordAsync(path, "image/png");
        }

        // 2. Generate Mock Text Files
        var textPaths = new[] { "note1.txt", "readme.md", "data.csv" };
        var contents = new[] { "This is a mock text file for testing.", "# Mock Readme\nThis is a markdown file.", "id,name,value\n1,test,100" };

        for (int i = 0; i < textPaths.Length; i++)
        {
            var path = Path.Combine(_mockFilesDir, textPaths[i]);
            if (!File.Exists(path))
            {
                await File.WriteAllTextAsync(path, contents[i]);
            }
            var mime = textPaths[i].EndsWith(".md") ? "text/markdown" : (textPaths[i].EndsWith(".csv") ? "text/csv" : "text/plain");
            await CreateFileRecordAsync(path, mime);
        }

        _logger.LogInformation("Mock data generation completed.");
    }

    private async Task CreateFileRecordAsync(string filePath, string mimeType)
    {
        var fileInfo = new FileInfo(filePath);
        // Use a simple hash for mock data
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(filePath))).ToLowerInvariant();
        
        var record = FileRecord.Create(filePath, hash, fileInfo.Length, mimeType);
        await _fileRepository.SaveAsync(record);

        // Add some mock metadata
        var tagField = await _metadataRepository.GetOrCreateFieldAsync("tag", "Tag");
        var subjectField = await _metadataRepository.GetOrCreateFieldAsync("subject", "Subject");

        if (tagField != null) await _metadataRepository.AddMetadataValueAsync(record.Id, tagField.Id, "MockData");
        if (subjectField != null) await _metadataRepository.AddMetadataValueAsync(record.Id, subjectField.Id, "Testing");
    }
}
