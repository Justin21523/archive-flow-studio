using System.Threading.Tasks;
using ArchiveFlow.Application.DTOs;

namespace ArchiveFlow.Application.Interfaces;

public interface IWorkflowStorageService
{
    Task SaveWorkflowAsync(string fileName, WorkflowDto workflow);
    Task<WorkflowDto?> LoadWorkflowAsync(string fileName);
    Task<IEnumerable<string>> ListWorkflowsAsync();
}