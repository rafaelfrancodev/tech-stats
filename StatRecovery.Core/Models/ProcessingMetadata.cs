namespace StatRecovery.Core.Models
{
    public class ProcessingMetadata
    {
        public List<ProcessedZipFile> ProcessedFiles { get; init; } = [];
    }

    public class ProcessedZipFile
    {
        public string ZipFileName { get; init; }
        public DateTime ProcessedDate { get; set; }
        public bool IsFullyProcessed { get; set; }
        public List<ExtractedPdfFile> ExtractedPdfs { get; init; } = [];
    }

    public class ExtractedPdfFile
    {
        public string PdfFileName { get; init; }
        public string PoNumber { get; init; }
        public byte[] FileContent { get; init; }
        public bool UploadSuccess { get; set; }
        public long FileSize { get; set; }
    }
}
