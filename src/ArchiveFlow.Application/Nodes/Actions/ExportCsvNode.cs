using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Actions;

public class ExportCsvNode : IArchiveNode
{
    private readonly string _fileName;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Export CSV: {_fileName}";
    public double X { get; set; }
    public double Y { get; set; }

    public ExportCsvNode(string fileName)
    {
        _fileName = string.IsNullOrWhiteSpace(fileName) ? "output.csv" : fileName;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,ArchiveId,FileName,FilePath,Extension,Size,Status");
        
        foreach (var file in context.CurrentFileSet)
        {
            sb.AppendLine($"{file.Id},{file.ArchiveId},{file.FileName},{file.FilePath},{file.FileExtension},{file.FileSize},{file.Status}");
        }
        
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", _fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, sb.ToString());
        
        return Task.CompletedTask;
    }
}