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

        public async Task<string> StoragePhotos(string container, IFormFile photo)
        {
            try
            {
                if(photo == null || photo.Length == 0)
                {
                    throw new ApplicationException("El archivo adjunto no puede estar vacio");
                }

                var extension = Path.GetExtension(photo.FileName).ToLower();
                var allowedExtension = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };

                if(!allowedExtension.Contains(extension))
                {
                    throw new ApplicationException("Solo se permiten archivos de imagen (.jpg, .jpeg, .png, .gif, .bmp, .webp)");
                }

                var client = new BlobContainerClient(_connectionString, container);
                await client.CreateIfNotExistsAsync();

                var fileName = photo.FileName;
                var blob = client.GetBlobClient(fileName);

                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".bmp" => "image/bmp",
                    _ => "application/octet-stream"
                };

                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                };

                await blob.UploadAsync(photo.OpenReadStream(), blobHttpHeaders);

                return blob.Uri.ToString();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error al guardar el archivo", ex);
            }
        }
    }
}