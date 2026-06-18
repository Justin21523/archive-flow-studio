using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;

namespace ArchiveFlow.Application.Nodes.Actions;

/// <summary>
/// Action node that exports the current file set into a standard Dublin Core XML file.
/// </summary>
public class ExportDublinCoreNode : IArchiveNode
{
    private readonly IDublinCoreExportService _exportService;
    public string OutputFileName { get; set; } = "dublin_core_export.xml";

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Export DC XML: {OutputFileName}";
    public double X { get; set; }
    public double Y { get; set; }

    public ExportDublinCoreNode(IDublinCoreExportService exportService, string outputFileName)
    {
        _exportService = exportService;
        OutputFileName = string.IsNullOrWhiteSpace(outputFileName) ? "dublin_core_export.xml" : outputFileName;
    }

    public async Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // Save to the project's Data/Exports directory
        var exportDir = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Exports");
        Directory.CreateDirectory(exportDir);
        
        var fullPath = Path.Combine(exportDir, OutputFileName);
        
        await _exportService.ExportToDublinCoreXmlAsync(context.CurrentFileSet, fullPath);
        
        // Action node doesn't change the file set flow, it just produces a side-effect (the XML file)
    }
}