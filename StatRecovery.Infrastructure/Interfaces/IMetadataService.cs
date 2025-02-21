using StatRecovery.Core.Models;

namespace StatRecovery.Infrastructure.Interfaces
{
    public interface IMetadataService
    {
        Task<ProcessingMetadata> LoadMetadataAsync();
        Task SaveMetadataAsync(ProcessingMetadata metadata);
        bool IsPdfProcessed(string zipFileName, string pdfFileName, ProcessingMetadata metadata);
        bool IsZipFullyProcessed(string zipFileName, ProcessingMetadata metadata);
    }
}
