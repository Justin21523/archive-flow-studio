using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Nodes.Query;

/// <summary>
/// Advanced DAG node that filters files based on a conditional rule.
/// Rule format examples: "size:>5000", "ext:.pdf", "contains:report", "status:New"
/// </summary>
public class ConditionBranchNode : IArchiveNode
{
    public string ConditionRule { get; set; } = string.Empty;

    public Guid Id { get; } = Guid.NewGuid();
    public string DisplayName => $"Condition: {ConditionRule}";
    public double X { get; set; }
    public double Y { get; set; }

    public ConditionBranchNode(string conditionRule)
    {
        ConditionRule = conditionRule;
    }

    public Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ConditionRule)) return Task.CompletedTask;

        var parts = ConditionRule.Split(':', 2);
        if (parts.Length != 2) return Task.CompletedTask;

        var conditionType = parts[0].Trim().ToLowerInvariant();
        var conditionValue = parts[1].Trim();

        var filteredFiles = context.CurrentFileSet.Where(file => EvaluateCondition(file, conditionType, conditionValue)).ToList();
        context.SetFileSet(filteredFiles);

        return Task.CompletedTask;
    }

    private bool EvaluateCondition(FileRecord file, string type, string value)
    {
        return type switch
        {
            "size" => ParseSizeCondition(file.FileSize, value),
            "ext" or "type" => file.FileExtension.Equals(value, StringComparison.OrdinalIgnoreCase),
            "contains" or "name" => file.FileName.Contains(value, StringComparison.OrdinalIgnoreCase),
            "status" => file.Status.ToString().Equals(value, StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private bool ParseSizeCondition(long fileSize, string condition)
    {
        if (condition.StartsWith('>')) return fileSize > long.Parse(condition.TrimStart('>'));
        if (condition.StartsWith('<')) return fileSize < long.Parse(condition.TrimStart('<'));
        return fileSize == long.Parse(condition);
    }
}