using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StatRecovery.Application.Interfaces;
using StatRecovery.Core.Interfaces;
using StatRecovery.Core.Models;
using StatRecovery.Infrastructure.Interfaces;

namespace StatRecovery.Application.UseCases;

public class ProcessFilesUseCase(
    ILogger<ProcessFilesUseCase> logger,
    IS3StorageService s3StorageService,
    IZipService zipService,
    IMetadataService metadataService,
    IConfiguration configuration)
    : IProcessFilesUseCase
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
               var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Starting processing at {StartTime}...", startTime);

        var totalZipFiles = 0;
        var totalPdfsProcessed = 0;
        var totalSuccess = 0;
        var totalFailed = 0;

        int maxParallelFiles = int.TryParse(configuration["MaxParallelFiles"], out maxParallelFiles) ? maxParallelFiles : 10;
        int maxParallelUpload = int.TryParse(configuration["MaxParallelUpload"], out maxParallelUpload) ? maxParallelUpload : 5;

        logger.LogInformation("Max parallel ZIP files processing set to: {MaxParallelFiles}", maxParallelFiles);
        logger.LogInformation("Max parallel PDF uploads set to: {MaxParallelUpload}", maxParallelUpload);

        try
        {
            logger.LogInformation("Getting loading metadata...");
            var metadata = await metadataService.LoadMetadataAsync();

            logger.LogInformation("Getting zip files list from S3...");
            var zipFiles = await s3StorageService.ListZipFilesAsync();

            if (zipFiles.Count == 0)
            {
                logger.LogWarning("No zip file found in S3.");
                return;
            }

            totalZipFiles = zipFiles.Count;

            using var zipSemaphore = new SemaphoreSlim(maxParallelFiles);
            using var uploadSemaphore = new SemaphoreSlim(maxParallelUpload);

            var tasks = zipFiles.Select(async zipFile =>
            {
                await zipSemaphore.WaitAsync();
                try
                {
                    if (metadataService.IsZipFullyProcessed(zipFile, metadata))
                    {
                        logger.LogInformation("The file {ZipFile} has already been processed. Skipping...", zipFile);
                        return;
                    }
                    logger.LogInformation("Downloading files from ZIP: {ZipFile}", zipFile);
                    await using var zipStream = await s3StorageService.GetZipFileStreamAsync(zipFile);
                    logger.LogInformation("Extracting files from ZIP: {ZipFile}", zipFile);

                    var extractedPdfs = zipService.ExtractZipFile(zipStream);
                    if (extractedPdfs.Count == 0)
                    {
                        logger.LogWarning("ZIP {ZipFile} was empty or extraction failed.", zipFile);
                        return;
                    }

                    var processedZipFile = metadata.ProcessedFiles
                        .FirstOrDefault(z => z.ZipFileName == zipFile) ?? new ProcessedZipFile
                        {
                            ZipFileName = zipFile,
                            ProcessedDate = DateTime.UtcNow,
                            ExtractedPdfs = []
                        };

                    totalPdfsProcessed += extractedPdfs.Count;

                    var uploadTasks = extractedPdfs
                        .Where(pdf => !metadataService.IsPdfProcessed(zipFile, pdf.PdfFileName, metadata))
                        .Select(async pdf =>
                        {
                            await uploadSemaphore.WaitAsync();
                            try
                            {
                                await s3StorageService.UploadPdfAsync(pdf.FileContent, pdf.PoNumber, pdf.PdfFileName);
                                pdf.UploadSuccess = true;
                                pdf.FileSize = pdf.FileContent.Length;
                                logger.LogInformation("Successful upload: {PdfFileName} from ZIP {ZipFile} to PO {PoNumber}", 
                                    pdf.PdfFileName, zipFile, pdf.PoNumber);

                                totalSuccess++;
                            }
                            catch (Exception ex)
                            {
                                pdf.UploadSuccess = false;
                                logger.LogError(ex, "Error uploading file {PdfFileName} from ZIP {ZipFile} to PO {PoNumber}", 
                                    pdf.PdfFileName, zipFile, pdf.PoNumber);

                                totalFailed++;
                            }
                            finally
                            {
                                uploadSemaphore.Release();
                            }

                            processedZipFile.ExtractedPdfs.Add(pdf);
                        });

                    await Task.WhenAll(uploadTasks);

                    processedZipFile.IsFullyProcessed = processedZipFile.ExtractedPdfs.All(p => p.UploadSuccess);

                    if (metadata.ProcessedFiles.All(z => z.ZipFileName != zipFile))
                    {
                        metadata.ProcessedFiles.Add(processedZipFile);
                    }

                    try
                    {
                        await metadataService.SaveMetadataAsync(metadata);
                        logger.LogInformation("{ZipFile} file processing complete!", zipFile);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to save metadata for ZIP {ZipFile}", zipFile);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while processing ZIP {ZipFile}", zipFile);
                }
                finally
                {
                    zipSemaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "A critical error occurred in the main process.");
        }
        finally
        {
            stopwatch.Stop();
            var endTime = DateTime.UtcNow;
            var totalDuration = stopwatch.Elapsed;
            logger.LogInformation("Processing finished at {EndTime}", endTime);
            logger.LogInformation("Total processing time: {Duration}", totalDuration);
            logger.LogInformation("Total ZIP files processed: {TotalZipFiles}", totalZipFiles);
            logger.LogInformation("Total PDFs processed: {TotalPdfsProcessed}", totalPdfsProcessed);
            logger.LogInformation("Total PDFs uploaded successfully: {TotalSuccess}", totalSuccess);
            logger.LogInformation("Total PDFs failed to upload: {TotalFailed}", totalFailed);
        }
    }
}