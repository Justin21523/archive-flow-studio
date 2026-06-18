using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;
using ArchiveFlow.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ArchiveFlow.Infrastructure.Storage;

public class LocalWorkflowStorageService : IWorkflowStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalWorkflowStorageService> _logger;

    public LocalWorkflowStorageService(ILogger<LocalWorkflowStorageService> logger)
    {
        // 儲存在專案根目錄的 Data/Workflows 資料夾
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Workflows");
        Directory.CreateDirectory(_storagePath);
        _logger = logger;
    }

    public async Task SaveWorkflowAsync(string fileName, WorkflowDto workflow)
    {
        var filePath = Path.Combine(_storagePath, $"{fileName}.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(workflow, options);
        
        await File.WriteAllTextAsync(filePath, json);
        _logger.LogInformation($"Workflow saved to: {filePath}");
    }

    public async Task<WorkflowDto?> LoadWorkflowAsync(string fileName)
    {
        var filePath = Path.Combine(_storagePath, $"{fileName}.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"Workflow file not found: {filePath}");
            return null;
        }

        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<WorkflowDto>(json);
    }

    public Task<IEnumerable<string>> ListWorkflowsAsync()
    {
        var files = Directory.GetFiles(_storagePath, "*.json");
        var names = new List<string>();
        foreach (var file in files)
        {
            names.Add(Path.GetFileNameWithoutExtension(file));
        }
        return Task.FromResult<IEnumerable<string>>(names);
    }
}