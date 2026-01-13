using Intranet.Dtos;
using Intranet.Models;
using Intranet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;
using System.Diagnostics;
using System.Security.Claims;

namespace Intranet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogContentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureStorageService _azureStorageService;
        private readonly EmailService _emailService;
        private readonly string _container = "blogcontent";

        public BlogContentController(AppDbContext context, 
            AzureStorageService azureStorageService,
            EmailService emailService)
        {
            _context = context;
            _azureStorageService = azureStorageService;
            _emailService = emailService;
        }

        [HttpGet]
        [Route("GetBlogContent")]
        public async Task<IActionResult> GetBlogContent([FromQuery] string pageType)
        {
            if (string.IsNullOrWhiteSpace(pageType))
            {
                return BadRequest("El parámetro es requerido");
            }

            var content = await _context.BlogContent
                            .AsNoTracking()
                            .Include(b => b.BlogMedia)
                            .Where(b => b.PageType == pageType)
                            .OrderByDescending(b => b.CreatedAt)
                            .ToListAsync();

            if(content == null)
            {
                return BadRequest("Lista vacia");
            }

            return Ok(content);
        }

        [HttpGet]
        [Route("GetAllBlogContent")]
        public async Task<IActionResult> GetAllBlogContent()
        {
            try
            {
                var content = await _context.BlogContent
                                        .AsNoTracking()
                                        .Include(b => b.BlogMedia)
                                        .OrderByDescending(b => b.CreatedAt)
                                        .ToListAsync();

                if(content == null || !content.Any())
                {
                    return Ok(new List<BlogContent>());
                }

                return Ok(content);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("GetBlogContentById/{id}")]
        public async Task<IActionResult> GetBlogContentById(int id, string pageType)
        {
            var IdBlog = await _context.BlogContent
                                .AsNoTracking()
                                .Include(b => b.BlogMedia)
                                .Where(b => b.PageType == pageType)
                                .OrderByDescending(b => b.CreatedAt)
                                .FirstOrDefaultAsync(b => b.Id == id);

            if(IdBlog == null)
            {
                return NotFound("Id no encontrado");
            }

            return Ok(IdBlog);
        }

        [Authorize]
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromForm] BlogContentDto blogContentDto)
        {
            if (blogContentDto == null) return BadRequest("Campos vacíos");

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null || !int.TryParse(userClaim.Value, out int userId))
                return Unauthorized(new { message = "Usuario no autenticado" });

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return Unauthorized(new { message = "Usuario no válido" });

            var newBlogContent = new BlogContent
            {
                Title = blogContentDto.Title,
                Content = blogContentDto.Content,
                Template = blogContentDto.Template,
                PageType = blogContentDto.PageType,
                IdUser = userId,
            };

            _context.BlogContent.Add(newBlogContent);
            await _context.SaveChangesAsync();

            if (blogContentDto.MediaFiles != null && blogContentDto.MediaFiles.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".webm" };
                foreach (var file in blogContentDto.MediaFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (allowedExtensions.Contains(ext))
                    {
                        try
                        {
                            string fileUrl = await _azureStorageService.UploadFile(_container, file, allowedExtensions);
                            string type = (ext == ".mp4" || ext == ".mov" || ext == ".webm") ? "video" : "image";

                            _context.BlogMedia.Add(new BlogMedia
                            {
                                Url = fileUrl,
                                MediaType = type,
                                BlogContentId = newBlogContent.Id
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error subiendo archivo: {ex.Message}");
                        }
                    }
                }
                await _context.SaveChangesAsync();
            }

            _ = Task.Run(() => SendNewPostNotificationAsync(newBlogContent));

            return Ok(new
            {
                success = true,
                message = "Registro creado exitosamente",
                blogId = newBlogContent.Id
            });
        }

        [Authorize]
        [HttpPut]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, [FromForm] BlogContentDto blogContentDto)
        {
            if (blogContentDto == null)
            {
                return BadRequest("Campos vacíos");
            }

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null || !int.TryParse(userClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(new { message = "Usuario no válido" });
            }

            var isAdmin = User.IsInRole("Admin");

            var existingBlogContent = await _context.BlogContent
                .FirstOrDefaultAsync(b => b.Id == id && (b.IdUser == userId || isAdmin));

            if (existingBlogContent == null)
            {
                return NotFound(new { message = "Contenido no encontrado o no autorizado para editarlo" });
            }

            existingBlogContent.Title = blogContentDto.Title;
            existingBlogContent.Content = blogContentDto.Content;
            existingBlogContent.PageType = blogContentDto.PageType;

            if(blogContentDto.MediaIdsToDelete != null && blogContentDto.MediaIdsToDelete.Any())
            {
                var imagesToDelete = await _context.BlogMedia
                                    .Where(x => x.BlogContentId == id && blogContentDto.MediaIdsToDelete.Contains(x.Id))
                                    .ToListAsync();
                if(imagesToDelete.Any())
                {
                    _context.BlogMedia.RemoveRange(imagesToDelete);
                }
            }

            if (blogContentDto.MediaFiles != null && blogContentDto.MediaFiles.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".webm" };

                foreach (var file in blogContentDto.MediaFiles)
                {
                    var ext = Path.GetExtension(file.FileName).ToLower();
                    if (allowedExtensions.Contains(ext))
                    {
                        try
                        {
                            string fileUrl = await _azureStorageService.UploadFile(_container, file, allowedExtensions);

                            string type = (ext == ".mp4" || ext == ".mov" || ext == ".webm") ? "video" : "image";

                            var mediaItem = new BlogMedia
                            {
                                Url = fileUrl,
                                MediaType = type,
                                BlogContentId = existingBlogContent.Id
                            };

                            _context.BlogMedia.Add(mediaItem);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al subir archivo en edición: {ex.Message}");
                        }
                    }
                }
            }

            _context.BlogContent.Update(existingBlogContent);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Contenido actualizado exitosamente",
                blogId = existingBlogContent.Id
            });
        }

        [Authorize]
        [HttpDelete]
        [Route("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var IdBlog = await _context.BlogContent.FindAsync(id);

            if(IdBlog == null)
            {
                return NotFound("Id no encontrado");
            }

            _context.BlogContent.Remove(IdBlog);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Registro eliminado"
            });
        }

        private async Task SendNewPostNotificationAsync(BlogContent post)
        {
            try
            {
                //var recipients = new List<string> { "jose.lugo@mesa.ms" };

                string subject = $"Nueva Noticia MESA: {post.Title}";
                string link = "https://orange-wave-0308f6610.2.azurestaticapps.net";

                string htmlMessage = $@"
                    <div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
                            
                            <div style='background-color: #007bff; padding: 20px; text-align: center;'>
                                <h1 style='color: #ffffff; margin: 0; font-size: 24px;'>Novedades MESA</h1>
                            </div>

                            <div style='padding: 30px;'>
                                <span style='background-color: #e3f2fd; color: #0d47a1; padding: 4px 8px; border-radius: 4px; font-size: 12px; font-weight: bold; text-transform: uppercase;'>
                                    {post.PageType}
                                </span>
                                
                                <h2 style='color: #333333; margin-top: 15px;'>{post.Title}</h2>
                                
                                <p style='color: #555555; line-height: 1.6; font-size: 16px;'>
                                    {(post.Content.Length > 200 ? post.Content.Substring(0, 200) + "..." : post.Content)}
                                </p>

                                <div style='text-align: center; margin-top: 30px;'>
                                    <a href='{link}' style='background-color: #007bff; color: #ffffff; text-decoration: none; padding: 12px 25px; border-radius: 5px; font-weight: bold; display: inline-block;'>
                                        Leer noticia completa
                                    </a>
                                </div>
                            </div>

                            <div style='background-color: #eeeeee; padding: 15px; text-align: center; font-size: 12px; color: #888888;'>
                                <p>Has recibido este correo porque formas parte del equipo MESA.</p>
                            </div>
                        </div>
                    </div>";

               
                await _emailService.SendEmailForAPersonAsync("jose.lugo@mesa.ms", subject, htmlMessage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FONDO - ERROR CORREO]: {ex.Message}");
            }
        }
    }
}