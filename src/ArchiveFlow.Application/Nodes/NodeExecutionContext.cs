using System.Collections.Generic;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes;

/// <summary>
/// The context object that flows through the workflow nodes.
/// It holds the current set of files being processed.
/// </summary>
public class NodeExecutionContext
{
    public List<FileRecord> CurrentFileSet { get; private set; } = new();
    public Dictionary<string, object> SharedData { get; private set; } = new();
    public bool IsPreviewMode { get; set; } = false;
    public List<string> PreviewMessages { get; } = new List<string>();
    
    public void SetFileSet(IEnumerable<FileRecord> files)
    {
        CurrentFileSet = new List<FileRecord>(files);
    }

    public void AddFiles(IEnumerable<FileRecord> files)
    {
        CurrentFileSet.AddRange(files);
    }

    public void ClearFileSet()
    {
        CurrentFileSet.Clear();
    }
}