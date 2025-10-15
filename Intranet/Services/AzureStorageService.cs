using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Intranet.Services
{
    public class AzureStorageService
    {
        private readonly string _connectionString;

        public AzureStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection")!;
        }

        public async Task<string> UploadFile(string container, IFormFile file, string[] allowedExtensions)
        {
            if (file == null || file.Length == 0)
            {
                throw new ApplicationException("El archivo adjunto no puede estar vacío");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new ApplicationException($"Tipo de archivo no permitido. Extensiones permitidas: {string.Join(", ", allowedExtensions)}");
            }

            var client = new BlobContainerClient(_connectionString, container);
            await client.CreateIfNotExistsAsync();

            var fileName = file.FileName;
            var blob = client.GetBlobClient(fileName);

            var contentType = GetContentType(extension);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
            };

            await blob.UploadAsync(file.OpenReadStream(), blobHttpHeaders);

            return blob.Uri.ToString();
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",

                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".flv" => "video/x-flv",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",

                _ => "application/octet-stream"
            };
        }
    }
}