namespace StatRecovery.Core.Models
{
    public class ProcessingMetadata
    {
        public List<ProcessedZipFile> ProcessedFiles { get; set; } = new List<ProcessedZipFile>();
    }

    public class ProcessedZipFile
    {
        public string ZipFileName { get; set; }
        public DateTime ProcessedDate { get; set; }
        public bool IsFullyProcessed { get; set; }
        public List<ExtractedPdfFile> ExtractedPdfs { get; set; } = new List<ExtractedPdfFile>();
    }

    public class ExtractedPdfFile
    {
        public string PdfFileName { get; set; }
        public string PoNumber { get; set; }
        public byte[] FileContent { get; set; }
        public bool UploadSuccess { get; set; }
        public long FileSize { get; set; }
    }
}
