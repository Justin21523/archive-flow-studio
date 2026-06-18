using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Filters files using regular expression matching on filename or content.
/// Parameter format: "field:pattern" e.g., "name:^report_.*\.pdf$"
/// </summary>
public class RegexSearchNode : IArchiveNode
{
    public string RegexRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Regex Search: {RegexRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public RegexSearchNode(string regexRule)
    {
        RegexRule = regexRule;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(RegexRule)) return Task.CompletedTask;

        var parts = RegexRule.Split(':', 2);
        if (parts.Length != 2) return Task.CompletedTask;

        var searchField = parts[0].Trim().ToLowerInvariant();
        var pattern = parts[1].Trim();

        Regex regex;
        try
        {
            regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        catch
        {
            return Task.CompletedTask;
        }

        var filtered = context.CurrentFileSet.Where(file =>
        {
            string textToSearch = searchField switch
            {
                "name" or "filename" => file.FileName,
                "extension" or "ext" => file.FileExtension,
                "path" => file.FilePath,
                "content" => file.ContentPreview ?? string.Empty,
                _ => file.FileName
            };

            return regex.IsMatch(textToSearch);
        }).ToList();

        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}