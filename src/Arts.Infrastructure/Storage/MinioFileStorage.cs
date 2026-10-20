using Amazon.S3;
using Amazon.S3.Model;
using Arts.Application.Interfaces;
using Arts.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arts.Infrastructure.Storage;

public class MinioFileStorage(
    IAmazonS3 s3Client,
    IOptions<MinioOptions> options,
    ILogger<MinioFileStorage> logger) : IFileStorage
{
    private readonly MinioOptions _options = options.Value;
    
    public async Task<string> UploadFileAsync(IFormFile file, string prefix, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Файл не передан или пустой", nameof(file));

        var fileName = $"{prefix}{Guid.NewGuid()}_{file.FileName}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = fileName,
            InputStream = memoryStream,
            ContentType = file.ContentType
        };

        try
        {
            await s3Client.PutObjectAsync(putRequest, cancellationToken);
            logger.LogInformation("Файл {FileName} успешно загружен в MinIO (Бакет: {BucketName})", 
                fileName, _options.BucketName);
        }
        catch (AmazonS3Exception ex)
        {
            logger.LogError(ex, "Сбой загрузки файла {FileName} в MinIO. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}", 
                fileName, ex.StatusCode, ex.ErrorCode);
            throw;
        }

        return $"{_options.EndpointUrl}/{_options.BucketName}/{fileName}";
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        var key = fileUrl.Substring(fileUrl.IndexOf(_options.BucketName, StringComparison.Ordinal) + _options.BucketName.Length + 1);

        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key
        };
        await s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);
    }
}