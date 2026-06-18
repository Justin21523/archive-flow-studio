using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Application.Interfaces;
using ArchiveFlow.Application.Nodes;

namespace ArchiveFlow.SamplePlugin;

/// <summary>
/// A sample node provided by the external plugin.
/// It counts the words in the content preview of each file and stores it in the shared context.
/// </summary>
public class WordCountNode : IArchiveNode
{
    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => "Word Count (Plugin)";
    public double X { get; set; }
    public double Y { get; set; }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        // This is a side-effect node, it doesn't filter the list but adds metadata
        foreach (var file in context.CurrentFileSet.ToList())
        {
            if (!string.IsNullOrWhiteSpace(file.ContentPreview))
            {
                int wordCount = file.ContentPreview.Split(' ', '\n', '\r').Length;
                // In a real app, we would save this to metadata. Here we just log it.
                Console.WriteLine($"[Plugin] File {file.FileName} has approx {wordCount} words.");
            }
        }
        return Task.CompletedTask;
    }
}