namespace ArchiveFlow.Application.Interfaces;

public interface IDataRepository
{
    IFileRepository Files { get; }

    IMetadataRepository Metadata { get; }

    IRelationshipRepository Relationships { get; }

    IExportJobRepository ExportJobs { get; }

    IImportJobRepository ImportJobs { get; }
}
