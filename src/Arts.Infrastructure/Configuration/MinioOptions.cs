namespace Arts.Infrastructure.Configuration;

public class MinioOptions
{
    public const string SectionName = "Minio";

    public string EndpointUrl { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}