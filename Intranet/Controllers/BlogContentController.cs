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
        [Route("GetBlogContentById/{id:int}")]
        public async Task<IActionResult> GetBlogContentById(int id)
        {
            var IdBlog = await _context.BlogContent.FirstOrDefaultAsync(b => b.Id == id);

            if (IdBlog == null)
            {
                return NotFound("Id no encontrado");
            }

            return Ok(IdBlog);
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

        [Authorize]
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromForm] BlogContentDto blogContentDto)
        {
            if(blogContentDto == null)
            {
                return BadRequest("Campos vacios");
            }

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null || !int.TryParse(userClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticas" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(new { message = "Usuario no valido" });
            }

            string imageUrl = null!;

            if (blogContentDto.Img != null && blogContentDto.Img.Length > 0)
            {
                try
                {
                    imageUrl = await _azureStorageService.StoragePhotos(_container, blogContentDto.Img);
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
                Img = imageUrl,
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