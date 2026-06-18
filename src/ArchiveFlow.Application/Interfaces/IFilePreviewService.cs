using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface IFilePreviewService
{
    Task GeneratePreviewAsync(FileRecord file);
}