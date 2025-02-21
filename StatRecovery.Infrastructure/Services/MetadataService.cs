using System.Text.Json;
using StatRecovery.Core.Models;
using StatRecovery.Infrastructure.Interfaces;

namespace StatRecovery.Infrastructure.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IS3StorageService _s3StorageService;
        private readonly string _metadataFileName = "processing_metadata.json";

        public MetadataService(IS3StorageService s3StorageService)
        {
            _s3StorageService = s3StorageService;
        }

        public async Task<ProcessingMetadata> LoadMetadataAsync()
        {
            try
            {
                using (var metadataStream = await _s3StorageService.DownloadMetadataAsync(_metadataFileName))
                {
                    if (metadataStream == null)
                        return new ProcessingMetadata();

                    var metadata = await JsonSerializer.DeserializeAsync<ProcessingMetadata>(metadataStream);
                    return metadata ?? new ProcessingMetadata();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading metadata: {ex.Message}");
                return new ProcessingMetadata();
            }
        }

        public async Task SaveMetadataAsync(ProcessingMetadata metadata)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(memoryStream, metadata, new JsonSerializerOptions { WriteIndented = true });
                    memoryStream.Position = 0;

                    await _s3StorageService.UploadMetadataAsync(memoryStream, _metadataFileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving metadata: {ex.Message}");
            }
        }

        public bool IsPdfProcessed(string zipFileName, string pdfFileName, ProcessingMetadata metadata)
        {
            var processedZip = metadata.ProcessedFiles
                .FirstOrDefault(z => z.ZipFileName == zipFileName);

            if (processedZip == null)
                return false;

            return processedZip.ExtractedPdfs.Any(pdf => pdf.PdfFileName == pdfFileName && pdf.UploadSuccess);
        }

        public bool IsZipFullyProcessed(string zipFileName, ProcessingMetadata metadata)
        {
            var processedZip = metadata.ProcessedFiles
                .FirstOrDefault(z => z.ZipFileName == zipFileName);

            return processedZip?.IsFullyProcessed ?? false;
        }

    }
}
