using Microsoft.Extensions.Configuration;
using Serilog;
using StatRecovery.Core.Interfaces;
using StatRecovery.Core.Models;
using StatRecovery.Core.Services;
using StatRecovery.Infrastructure.Interfaces;
using StatRecovery.Infrastructure.Services;

class Program
{
    static async Task Main(string[] args)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Log.Information("Starting processing...");

            IS3StorageService s3StorageService = new S3StorageService(configuration);
            ICsvParserService csvParserService = new CsvParserService();
            IZipService zipService = new ZipService(csvParserService);
            IMetadataService metadataService = new MetadataService(s3StorageService);

            var metadata = await metadataService.LoadMetadataAsync();

            Log.Information("Getting zip files from S3...");
            var zipFiles = await s3StorageService.ListZipFilesAsync();

            if (zipFiles.Count == 0)
            {
                Log.Warning("No zip file founded in S3.");
                return;
            }

            foreach (var zipFile in zipFiles)
            {
                if (metadataService.IsZipFullyProcessed(zipFile, metadata))
                {
                    Log.Information("The file {ZipFile} has already been processed. Skipping...", zipFile);
                    continue;
                }

                await using var zipStream = await s3StorageService.GetZipFileStreamAsync(zipFile);
                Log.Information("Extracting files from ZIP: {ZipFile}", zipFile);

                var extractedPdfs = zipService.ExtractZipFile(zipStream);

                var processedZipFile = metadata.ProcessedFiles
                    .FirstOrDefault(z => z.ZipFileName == zipFile) ?? new ProcessedZipFile
                {
                    ZipFileName = zipFile,
                    ProcessedDate = DateTime.UtcNow,
                    ExtractedPdfs = []
                };

                foreach (var pdf in extractedPdfs)
                {
                    if (metadataService.IsPdfProcessed(zipFile, pdf.PdfFileName, metadata))
                    {
                        Log.Information("The PDF {PdfFileName} has already been processed. Skipping...", pdf.PdfFileName);
                        continue;
                    }

                    try
                    {
                        await s3StorageService.UploadPdfAsync(pdf.FileContent, pdf.PoNumber, pdf.PdfFileName);
                        pdf.UploadSuccess = true;
                        pdf.FileSize = pdf.FileContent.Length;
                        Log.Information("Successful upload: {PdfFileName} to PO {PoNumber}", pdf.PdfFileName, pdf.PoNumber);
                    }
                    catch (Exception ex)
                    {
                        pdf.UploadSuccess = false;
                        Log.Error(ex, "Error uploading file {PdfFileName}", pdf.PdfFileName);
                    }

                    processedZipFile.ExtractedPdfs.Add(pdf);
                }

                processedZipFile.IsFullyProcessed = processedZipFile.ExtractedPdfs.All(p => p.UploadSuccess);

                if (metadata.ProcessedFiles.All(z => z.ZipFileName != zipFile))
                {
                    metadata.ProcessedFiles.Add(processedZipFile);
                }

                await metadataService.SaveMetadataAsync(metadata);
                Log.Information("{ZipFile} file processing complete!", zipFile);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An unexpected error occurred.");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
