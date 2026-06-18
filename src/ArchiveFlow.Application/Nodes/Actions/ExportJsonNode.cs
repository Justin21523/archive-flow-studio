using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Actions;

public class ExportJsonNode : IArchiveNode
{
    private readonly string _fileName;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Export JSON: {_fileName}";
    public double X { get; set; }
    public double Y { get; set; }

    public ExportJsonNode(string fileName)
    {
        _fileName = string.IsNullOrWhiteSpace(fileName) ? "output.json" : fileName;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(context.CurrentFileSet, options);
        
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", _fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        
        return Task.CompletedTask;
    }
}