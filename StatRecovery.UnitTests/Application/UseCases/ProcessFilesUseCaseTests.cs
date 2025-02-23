using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using StatRecovery.Application.UseCases;
using StatRecovery.Core.Interfaces;
using StatRecovery.Core.Models;
using StatRecovery.Infrastructure.Interfaces;

namespace StatRecovery.UnitTests.Application.UseCases;

public class ProcessFilesUseCaseTests
{
    private readonly Mock<IS3StorageService> _mockS3StorageService;
    private readonly Mock<IZipService> _mockZipService;
    private readonly Mock<IMetadataService> _mockMetadataService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<ProcessFilesUseCase>> _mockLogger;
    private readonly ProcessFilesUseCase _useCase;

    public ProcessFilesUseCaseTests()
    {
        _mockS3StorageService = new Mock<IS3StorageService>();
        _mockZipService = new Mock<IZipService>();
        _mockMetadataService = new Mock<IMetadataService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<ProcessFilesUseCase>>();
        _mockConfiguration.Setup(c => c["MaxParallelFiles"]).Returns("5");
        _mockConfiguration.Setup(c => c["MaxParallelUpload"]).Returns("3");
        
        _useCase = new ProcessFilesUseCase(
            _mockLogger.Object,
            _mockS3StorageService.Object,
            _mockZipService.Object,
            _mockMetadataService.Object,
            _mockConfiguration.Object
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessFilesSuccessfully()
    {
        // Arrange
        var metadata = new ProcessingMetadata();
        var zipFiles = new List<string> { "file1.zip", "file2.zip" };
        var extractedPdfs = new List<ExtractedPdfFile>
        {
            new() { PdfFileName = "file1.pdf", PoNumber = "PO123", FileSize = 1024, UploadSuccess = false },
            new() { PdfFileName = "file2.pdf", PoNumber = "PO456", FileSize = 2048, UploadSuccess = false }
        };

        _mockMetadataService.Setup(m => m.LoadMetadataAsync()).ReturnsAsync(metadata);
        _mockS3StorageService.Setup(m => m.ListZipFilesAsync()).ReturnsAsync(zipFiles);
        _mockS3StorageService.Setup(m => m.GetZipFileStreamAsync(It.IsAny<string>())).ReturnsAsync(new MemoryStream());
        _mockZipService.Setup(m => m.ExtractZipFile(It.IsAny<Stream>())).Returns(extractedPdfs);
        _mockS3StorageService
            .Setup(m => m.UploadPdfAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _mockMetadataService.Setup(m => m.SaveMetadataAsync(It.IsAny<ProcessingMetadata>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _mockS3StorageService.Verify(m => m.ListZipFilesAsync(), Times.Once);
        _mockS3StorageService.Verify(m => m.GetZipFileStreamAsync(It.IsAny<string>()), Times.Exactly(zipFiles.Count));
        _mockZipService.Verify(m => m.ExtractZipFile(It.IsAny<Stream>()), Times.Exactly(zipFiles.Count));
        _mockS3StorageService.Verify(m => m.UploadPdfAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(4));
        _mockMetadataService.Verify(m => m.SaveMetadataAsync(It.IsAny<ProcessingMetadata>()), Times.Exactly(2));
    }
}
