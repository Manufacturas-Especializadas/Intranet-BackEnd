using Intranet.Dtos;
using Intranet.Models;
using Intranet.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using System.Security.Claims;

namespace Intranet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InternalNewController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureStorageService _azureStorageService;
        private readonly string _contenedor = "internalnews";

        public InternalNewController(AppDbContext context, AzureStorageService azureStorageService)
        {
            _context = context;
            _azureStorageService = azureStorageService;
        }

        [HttpGet]
        [Route("GetInternalNewsById/{id:int}")]
        public async Task<IActionResult> GetInternalNewsById(int id)
        {
            var internalNews = await _context.InternalNews
                    .Select(i => new
                    {
                        i.Id,
                        i.Title,
                        i.Description,
                        i.Img,
                        i.CreatedAt
                    })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id);                

            if(internalNews == null)
            {
                return NotFound("Id no encontrado");
            }

            return Ok(internalNews);
        }

        [HttpGet]
        [Route("GetInternalNews")]
        public async Task<IActionResult> GetInternalNews()
        {
            var news = await _context.InternalNews
                            .Select(i => new
                            {
                                i.Id,
                                i.Title,
                                i.Description,
                                i.Img,
                                i.CreatedAt
                            })
                            .AsNoTracking()
                            .ToListAsync();

            if(news == null)
            {
                return BadRequest("Lista vacia");
            }

            return Ok(news);
        }

        [Authorize]
        [HttpPost]        
        [Route("CreateInternalNew")]
        public async Task<IActionResult> CreateInternalNew([FromForm] InternalNewDto dto)
        {
            if(dto == null)
            {
                return BadRequest("Campos vacios");
            }

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null || !int.TryParse(userClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(new { message = "Usuario no valido" });
            }

            string imageUrl = null!;

            if(dto.Img != null && dto.Img.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                    ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm"
                };

                try
                {
                    imageUrl = await _azureStorageService.UploadFile(_contenedor, dto.Img, allowedExtensions);
                }
                catch(Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            var newInternalNew = new InternalNews
            {
                Title = dto.Title,
                Description = dto.Description,
                Img = imageUrl,
                IdUser = userId,
            };

            _context.InternalNews.Add(newInternalNew);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                succes = true,
                message = "Registro exitosamente creado",
                newId = newInternalNew.Id,
                imageUrl = imageUrl,
            });
        }

        [Authorize]
        [HttpPut]
        [Route("UpdateInteranlNew/{id}")]
        public async Task<IActionResult> UpdateInternalNew([FromForm] InternalNewDto dto, int id)
        {
            var news = await _context.InternalNews.FindAsync(id);
            if(news == null)
            {
                return NotFound("Noticia no encontrada");
            }

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userClaim == null || !int.TryParse(userClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(new { message = "Usuario no valido" });
            }

            string imageUrl = null!;

            if (dto.Img != null && dto.Img.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
                    ".mp4", ".avi", ".mov", ".wmv", ".flv", ".mkv", ".webm"
                };

                var oldImageUrl = news.Img;

                try
                {
                    imageUrl = await _azureStorageService.UploadFile(_contenedor, dto.Img, allowedExtensions);
                    news.Img = imageUrl;

                    if (!string.IsNullOrEmpty(oldImageUrl))
                    {
                        await _azureStorageService.DeleteFile(_contenedor, oldImageUrl);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            news.Title = dto.Title;
            news.Description = dto.Description;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Noticia actualizada"
            });
        }

        [HttpDelete]
        [Route("DeleteInternalNew/{id}")]
        public async Task<IActionResult> DeleteInternalNew(int id)
        {
            var newId = await _context.InternalNews.FindAsync(id);

            if(newId == null)
            {
                return NotFound("Noticia no encontrada");
            }

            _context.InternalNews.Remove(newId);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Noticia eliminada"
            });
        }
    }
}