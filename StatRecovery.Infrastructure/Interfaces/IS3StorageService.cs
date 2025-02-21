using StatRecovery.Core.Models;

namespace StatRecovery.Infrastructure.Interfaces
{
    public interface IS3StorageService
    {
        Task<List<string>> ListZipFilesAsync();
        Task<Stream> GetZipFileStreamAsync(string s3Key);
        Task UploadPdfAsync(byte[] pdfContent, string poNumber, string pdfFileName);
        Task<Stream?> DownloadMetadataAsync(string metadataFileName);
        Task UploadMetadataAsync(Stream metadataStream, string metadataFileName);
    }
}
