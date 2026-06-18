using System.Collections.Generic;
using System.Threading.Tasks;
using ArchiveFlow.Domain.Entities;

namespace ArchiveFlow.Application.Interfaces;

/// <summary>
/// Repository for managing file relationships (knowledge graph edges).
/// </summary>
public interface IRelationshipRepository
{
    Task CreateRelationshipAsync(string sourceId, string targetId, string relationType);
    Task<IEnumerable<FileRelationship>> GetRelationshipsByFileIdAsync(string fileId);
    Task<IEnumerable<FileRelationship>> GetAllRelationshipsAsync();
}