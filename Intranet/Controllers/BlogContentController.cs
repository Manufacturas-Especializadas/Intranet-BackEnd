using Intranet.Dtos;
using Intranet.Models;
using Intranet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Writers;
using System.Security.Claims;

namespace Intranet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlogContentController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureStorageService _azureStorageService;
        private readonly string _container = "blogcontent";

        public BlogContentController(AppDbContext context, AzureStorageService azureStorageService)
        {
            _context = context;
            _azureStorageService = azureStorageService;
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

            if(blogContentDto.MediaFiles != null && blogContentDto.MediaFiles.Count > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".mov", ".webm" };
                var mediaList = new List<BlogMedia>();

                foreach(var file in blogContentDto.MediaFiles)
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
                                BlogContentId = newBlogContent.Id
                            };

                            _context.BlogMedia.Add(mediaItem);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine($"Error subiendo archivo: {ex.Message}");
                        }
                    }
                }

                await _context.SaveChangesAsync();
            }

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

        //[Authorize]
        //[HttpPut]
        //[Route("Update/{id:int}")]
        //public async Task<IActionResult> Update([FromForm] BlogContentDto blogContentDto, int id)
        //{
        //    var IdBlog = await _context.BlogContent.FindAsync(id);

        //    if(IdBlog == null)
        //    {
        //        return NotFound("Id no encontrado");
        //    }

            
        //}
    }
}