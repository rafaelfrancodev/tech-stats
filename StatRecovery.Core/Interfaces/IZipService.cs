using StatRecovery.Core.Models;

namespace StatRecovery.Core.Interfaces
{
    public interface IZipService
    {
        List<ExtractedPdfFile> ExtractZipFile(Stream zipStream);
    }
}
