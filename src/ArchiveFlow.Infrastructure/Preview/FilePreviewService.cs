using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Domain.Entities;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ArchiveFlow.Infrastructure.Preview;

public class FilePreviewService : IFilePreviewService
{
    private readonly IFileRepository _fileRepository;
    private readonly ILogger<FilePreviewService> _logger;
    private readonly string _thumbnailDir;

    public FilePreviewService(IFileRepository fileRepository, ILogger<FilePreviewService> logger)
    {
        _fileRepository = fileRepository;
        _logger = logger;
        _thumbnailDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Thumbnails");
        Directory.CreateDirectory(_thumbnailDir);
    }

    public async Task GeneratePreviewAsync(FileRecord file)
    {
        try
        {
            if (IsImage(file.FileExtension))
            {
                await GenerateImageThumbnailAsync(file);
            }
            else if (IsText(file.FileExtension))
            {
                await ExtractTextPreviewAsync(file);
            }
            
            // 更新資料庫
            await _fileRepository.UpdatePreviewAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate preview for {FileName}", file.FileName);
        }
    }

    private async Task GenerateImageThumbnailAsync(FileRecord file)
    {
        if (!File.Exists(file.FilePath)) return;

        var thumbnailPath = Path.Combine(_thumbnailDir, $"{file.Id}.jpg");
        
        // 如果縮圖已存在且檔案未修改，則跳過
        if (File.Exists(thumbnailPath)) 
        {
            file.UpdateThumbnailPath(thumbnailPath);
            return;
        }

        using var image = await Image.LoadAsync(file.FilePath);
        // 縮放到寬度 300px，保持比例
        image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(300, 0), Mode = ResizeMode.Max }));
        await image.SaveAsJpegAsync(thumbnailPath);
        
        file.UpdateThumbnailPath(thumbnailPath);
        _logger.LogInformation("Generated thumbnail for {FileName}", file.FileName);
    }

    private async Task ExtractTextPreviewAsync(FileRecord file)
    {
        if (!File.Exists(file.FilePath)) return;

        // 讀取前 1000 個字元
        using var reader = new StreamReader(file.FilePath);
        var buffer = new char[1000];
        var readCount = await reader.ReadAsync(buffer, 0, buffer.Length);
        
        file.UpdateContentPreview(new string(buffer, 0, readCount));
    }

    private bool IsImage(string ext) => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(ext.ToLowerInvariant());
    private bool IsText(string ext) => new[] { ".txt", ".md", ".csv", ".json", ".xml" }.Contains(ext.ToLowerInvariant());
}
