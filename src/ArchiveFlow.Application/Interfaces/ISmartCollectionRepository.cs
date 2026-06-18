using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Repository for managing Smart Collections.
/// </summary>
public interface ISmartCollectionRepository
{
    Task CreateAsync(SmartCollection collection);
    Task<IEnumerable<SmartCollection>> GetAllAsync();
    Task<SmartCollection?> GetByNameAsync(string name);
}