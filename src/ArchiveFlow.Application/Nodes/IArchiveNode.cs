using System;
using System.Threading.Tasks;

namespace ArchiveFlow.Application.Nodes;

public interface IArchiveNode
{
    Guid Id { get; }
    string DisplayName { get; }
    
    // 節點在 Canvas 上的位置 (用於 UI)
    double X { get; set; }
    double Y { get; set; }

    Task ExecuteAsync(NodeExecutionContext context, CancellationToken cancellationToken = default);
}