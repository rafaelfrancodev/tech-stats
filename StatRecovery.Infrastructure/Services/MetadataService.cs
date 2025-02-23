using System.Text.Json;
using Microsoft.Extensions.Logging;
using StatRecovery.Core.Models;
using StatRecovery.Infrastructure.Interfaces;

namespace StatRecovery.Infrastructure.Services
{
    public class MetadataService(IS3StorageService s3StorageService, ILogger<MetadataService> logger) : IMetadataService
    {
        private const string _metadataFileName = "stat_processing_metadata.json";

        public async Task<ProcessingMetadata> LoadMetadataAsync()
        {
            try
            {
                logger.LogInformation("Loading metadata from S3...");

                await using var metadataStream = await s3StorageService.DownloadMetadataAsync(_metadataFileName);
                if (metadataStream == null)
                {
                    logger.LogWarning("No metadata file found in S3.");
                    return new ProcessingMetadata();
                }

                var metadata = await JsonSerializer.DeserializeAsync<ProcessingMetadata>(metadataStream);
                logger.LogInformation("Metadata successfully loaded. Total ZIPs processed: {TotalProcessed}", metadata?.ProcessedFiles.Count ?? 0);

                return metadata ?? new ProcessingMetadata();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading metadata from S3.");
                return new ProcessingMetadata();
            }
        }

        public async Task SaveMetadataAsync(ProcessingMetadata metadata)
        {
            try
            {
                logger.LogInformation("Saving optimized metadata to S3...");

                var optimizedMetadata = new ProcessingMetadata
                {
                    ProcessedFiles = metadata.ProcessedFiles.Select(zip => new ProcessedZipFile
                    {
                        ZipFileName = zip.ZipFileName,
                        ProcessedDate = zip.ProcessedDate,
                        IsFullyProcessed = zip.IsFullyProcessed,
                        ExtractedPdfs = zip.ExtractedPdfs.Select(pdf => new ExtractedPdfFile
                        {
                            PdfFileName = pdf.PdfFileName,
                            PoNumber = pdf.PoNumber,
                            FileSize = pdf.FileSize,
                            UploadSuccess = pdf.UploadSuccess,
                            FileContent = Array.Empty<byte>() 
                        }).ToList()
                    }).ToList()
                };

                using var memoryStream = new MemoryStream();
                await JsonSerializer.SerializeAsync(memoryStream, optimizedMetadata, new JsonSerializerOptions { WriteIndented = true });
                memoryStream.Position = 0;

                await s3StorageService.UploadMetadataAsync(memoryStream, _metadataFileName);
                logger.LogInformation("Optimized metadata successfully saved. Total ZIPs recorded: {TotalFiles}", optimizedMetadata.ProcessedFiles.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error saving metadata to S3.");
            }
        }

        public bool IsPdfProcessed(string zipFileName, string pdfFileName, ProcessingMetadata metadata)
        {
            var processedZip = metadata.ProcessedFiles
                .FirstOrDefault(z => z.ZipFileName == zipFileName);

            bool isProcessed = processedZip != null && processedZip.ExtractedPdfs.Any(pdf => pdf.PdfFileName == pdfFileName && pdf.UploadSuccess);
            logger.LogDebug("Checked if PDF {PdfFile} from ZIP {ZipFile} is processed: {IsProcessed}", pdfFileName, zipFileName, isProcessed);
            
            return isProcessed;
        }

        public bool IsZipFullyProcessed(string zipFileName, ProcessingMetadata metadata)
        {
            var processedZip = metadata.ProcessedFiles
                .FirstOrDefault(z => z.ZipFileName == zipFileName);

            bool isFullyProcessed = processedZip?.IsFullyProcessed ?? false;
            logger.LogDebug("Checked if ZIP {ZipFile} is fully processed: {IsFullyProcessed}", zipFileName, isFullyProcessed);
            
            return isFullyProcessed;
        }
    }
}