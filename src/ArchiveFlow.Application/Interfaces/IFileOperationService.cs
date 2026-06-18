using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Service responsible for performing physical file system operations 
/// (Rename, Move) and synchronizing changes with the database.
/// </summary>
public interface IFileOperationService
{
    /// <summary>
    /// Previews the rename operation without modifying any files.
    /// </summary>
    Task<IEnumerable<FileOperationPreview>> PreviewRenameAsync(IEnumerable<FileRecord> files, string prefix, string suffix);

    /// <summary>
    /// Executes the rename operation and updates the database.
    /// </summary>
    Task ExecuteRenameAsync(IEnumerable<FileRecord> files, string prefix, string suffix);

    /// <summary>
    /// Previews the move operation without modifying any files.
    /// </summary>
    Task<IEnumerable<FileOperationPreview>> PreviewMoveAsync(IEnumerable<FileRecord> files, string targetDirectory);

    /// <summary>
    /// Executes the move operation and updates the database.
    /// </summary>
    Task ExecuteMoveAsync(IEnumerable<FileRecord> files, string targetDirectory);
}

/// <summary>
/// DTO representing a preview of a file operation.
/// </summary>
public class FileOperationPreview
{
    public string OriginalPath { get; set; } = string.Empty;
    public string NewPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public bool IsValid { get; set; } = true;
    public string ErrorMessage { get; set; } = string.Empty;
}