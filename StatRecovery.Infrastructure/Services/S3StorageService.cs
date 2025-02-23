using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using StatRecovery.Infrastructure.Interfaces;

namespace StatRecovery.Infrastructure.Services
{
    public class S3StorageService : IS3StorageService
    {
        private readonly string _bucketName;
        private readonly AmazonS3Client _s3Client;

        public S3StorageService(IConfiguration configuration)
        {
            _bucketName = configuration["AWS:BucketName"] ?? throw new Exception("BucketName not configured.");
            var accessKey = configuration["AWS:AccessKeyId"] ?? throw new Exception("AccessKeyId not configured.");
            var secretKey = configuration["AWS:SecretAccessKey"] ?? throw new Exception("SecretAccessKey not configured.");
            var region = configuration["AWS:Region"] ?? throw new Exception("Region not configured.");
            var awsRegion = RegionEndpoint.GetBySystemName(region);
            _s3Client = new AmazonS3Client(accessKey, secretKey, awsRegion);
        }

        public async Task<List<string>> ListZipFilesAsync()
        {
            var request = new ListObjectsV2Request
            {
                BucketName = _bucketName
            };

            var response = await _s3Client.ListObjectsV2Async(request);

            return (from obj in response.S3Objects where obj.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) select obj.Key).ToList();
        }

        public async Task<Stream> GetZipFileStreamAsync(string s3Key)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key
            };

            var response = await _s3Client.GetObjectAsync(request);
            return response.ResponseStream;
        }

        public async Task UploadPdfAsync(byte[] pdfContent, string poNumber, string pdfFileName)
        {
            var s3Key = $"by-po/{poNumber}/{pdfFileName}";
            using var memoryStream = new MemoryStream(pdfContent);
            
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = memoryStream
            };

            await _s3Client.PutObjectAsync(request);
            Console.WriteLine($"PDF uploaded to S3: {s3Key}");
        }

        public async Task<Stream?> DownloadMetadataAsync(string metadataFileName)
        {
            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = metadataFileName
                };

                var response = await _s3Client.GetObjectAsync(request);
                return response.ResponseStream;
            }
            catch (AmazonS3Exception ex)
            {
                if (ex.StatusCode != System.Net.HttpStatusCode.NotFound) throw;
                Console.WriteLine("Metadata file not found in S3.");
                return null;
            }
        }

        public async Task UploadMetadataAsync(Stream metadataStream, string metadataFileName)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = metadataFileName,
                InputStream = metadataStream
            };

            await _s3Client.PutObjectAsync(request);
            Console.WriteLine("Metadata sent to S3.");
        }
    }
}
