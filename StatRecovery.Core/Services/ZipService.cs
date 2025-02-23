using System.IO.Compression;
using StatRecovery.Core.Interfaces;
using StatRecovery.Core.Models;

namespace StatRecovery.Core.Services
{
    public class ZipService(ICsvParserService csvParserService) : IZipService
    {
        public List<ExtractedPdfFile> ExtractZipFile(Stream zipStream)
        {
            var extractedPdfs = new List<ExtractedPdfFile>();

            try
            {
                using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
                var pdfToPoMapping = new Dictionary<string, string>();

                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) continue;
                    Console.WriteLine($"Reading CSV: {entry.FullName}");
                    using var stream = entry.Open();
                    pdfToPoMapping = csvParserService.ParseCsv(stream);
                }

                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) continue;
                    var fileName = entry.Name;
                    var poNumber = pdfToPoMapping != null && pdfToPoMapping.TryGetValue(fileName, value: out var value) ? value : "Unknown";

                    using var memoryStream = new MemoryStream();
                    entry.Open().CopyTo(memoryStream);
                    memoryStream.Position = 0;

                    extractedPdfs.Add(new ExtractedPdfFile
                    {
                        PdfFileName = fileName,
                        PoNumber = poNumber,
                        FileContent = memoryStream.ToArray()
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting ZIP: {ex.Message}");
                throw;
            }

            return extractedPdfs;
        }
    }
}
