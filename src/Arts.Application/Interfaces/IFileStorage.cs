using Microsoft.AspNetCore.Http;

namespace Arts.Application.Interfaces;

public interface IFileStorage
{
    Task<string> UploadFileAsync(IFormFile file, string prefix, CancellationToken cancellationToken = default);
    
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}