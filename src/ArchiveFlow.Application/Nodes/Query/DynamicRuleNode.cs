using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// A flexible filter node that applies rules based on its parameter.
/// Parameter format: "type:ext" (e.g., "type:.png") or "name:keyword" (e.g., "name:mock").
/// </summary>
public class DynamicRuleNode : IArchiveNode
{
    public string RuleParameter { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Rule: {RuleParameter}";
    public double X { get; set; }
    public double Y { get; set; }

    public DynamicRuleNode(string ruleParameter)
    {
        RuleParameter = ruleParameter;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(RuleParameter)) return Task.CompletedTask;

        var parts = RuleParameter.Split(':', 2);
        if (parts.Length != 2) return Task.CompletedTask;

        var ruleType = parts[0].Trim().ToLowerInvariant();
        var ruleValue = parts[1].Trim().ToLowerInvariant();

        var filtered = context.CurrentFileSet.AsEnumerable();

        if (ruleType == "type" || ruleType == "ext")
        {
            filtered = filtered.Where(f => f.FileExtension.ToLowerInvariant().Contains(ruleValue));
        }
        else if (ruleType == "name" || ruleType == "keyword")
        {
            filtered = filtered.Where(f => f.FileName.ToLowerInvariant().Contains(ruleValue));
        }

        context.SetFileSet(filtered);
        return Task.CompletedTask;
    }
}