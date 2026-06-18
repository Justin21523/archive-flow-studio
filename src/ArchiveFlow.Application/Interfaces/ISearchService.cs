using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

public interface ISearchService
{
    Task IndexFileAsync(FileRecord file, string additionalContent = "");
    Task<IEnumerable<FileRecord>> SearchAsync(string keyword);
}