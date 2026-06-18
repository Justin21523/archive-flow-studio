using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for exporting file records and their metadata 
/// into standard Dublin Core XML format.
/// </summary>
public interface IDublinCoreExportService
{
    /// <summary>
    /// Exports a collection of files into a single Dublin Core XML file.
    /// </summary>
    /// <param name="files">The files to export.</param>
    /// <param name="outputFilePath">The full path where the XML file will be saved.</param>
    Task ExportToDublinCoreXmlAsync(IEnumerable<FileRecord> files, string outputFilePath);
}