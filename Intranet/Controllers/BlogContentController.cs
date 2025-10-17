using Intranet.Dtos;
using Intranet.Models;
using Intranet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
                            .Where(b => b.PageType == pageType)
                            .ToListAsync();

            if(content == null)
            {
                return BadRequest("Lista vacia");
            }

            return Ok(content);
        }

        [HttpGet]
        [Route("GetBlogContentById/{id}")]
        public async Task<IActionResult> GetBlogContentById(int id, string pageType)
        {
            var IdBlog = await _context.BlogContent
                                .AsNoTracking()
                                .Where(b => b.PageType == pageType)
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

            string fileUrl = null!;

            if (blogContentDto.Img != null && blogContentDto.Img.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                    ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm"
                };

                try
                {
                    fileUrl = await _azureStorageService.UploadFile(_container, blogContentDto.Img, allowedExtensions);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            var newBlogContent = new BlogContent
            {
                Title = blogContentDto.Title,
                SubTitle = blogContentDto.SubTitle,
                Description = blogContentDto.Description,
                Content = blogContentDto.Content,
                Template = blogContentDto.Template,
                PageType = blogContentDto.PageType,
                Img = fileUrl!,
                IdUser = userId
            };

            _context.BlogContent.Add(newBlogContent);
            await _context.SaveChangesAsync();

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

            string fileUrl = existingBlogContent.Img;

            if (blogContentDto.Img != null && blogContentDto.Img.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                    ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm"
                };

                try
                {
                    fileUrl = await _azureStorageService.UploadFile(_container, blogContentDto.Img, allowedExtensions);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            existingBlogContent.Title = blogContentDto.Title;
            existingBlogContent.SubTitle = blogContentDto.SubTitle;
            existingBlogContent.Description = blogContentDto.Description;
            existingBlogContent.Content = blogContentDto.Content;
            existingBlogContent.Template = blogContentDto.Template;
            existingBlogContent.PageType = blogContentDto.PageType;
            existingBlogContent.Img = fileUrl!;

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