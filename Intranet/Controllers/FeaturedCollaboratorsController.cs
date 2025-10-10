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
    public class FeaturedCollaboratorsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AzureStorageService _azureStorageService;
        private readonly string _container = "featuredcollaborators";

        public FeaturedCollaboratorsController(AppDbContext context, AzureStorageService azureStorageService)
        {
            _context = context;
            _azureStorageService = azureStorageService;
        }

        [HttpGet]
        [Route("GetCollaborators")]
        public async Task<IActionResult> GetCollaborators()
        {
            var collaborators = await _context.FeaturedCollaborators
                            .Select(f => new
                            {
                                f.Name,
                                f.Testimonial,
                                f.Photo,
                            })
                            .AsNoTracking()
                            .ToListAsync();

            return Ok(collaborators);
        }

        [Authorize]
        [HttpPost]
        [Route("CreateFeaturedCollaborators")]
        public async Task<IActionResult> CreateFeaturedCollaborators([FromForm] FeaturedCollaboratorsDto dto)
        {
            if(dto == null)
            {
                return BadRequest("Campos vacios");
            }

            var userClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if(userClaim == null || !int.TryParse(userClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticas" });
            }

            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(new { message = "Usuario no valido" });
            }

            string imageUrl = null!;

            if(dto.Photo != null && dto.Photo.Length > 0)
            {
                try
                {
                    imageUrl = await _azureStorageService.StoragePhotos(_container, dto.Photo);
                }
                catch(Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            var newCollaborator = new FeaturedCollaborators
            {
                Name = dto.Name,
                Testimonial = dto.Testimonial,
                Photo = imageUrl,
                IdUser = userId
            };

            _context.FeaturedCollaborators.Add(newCollaborator);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                sucess = true,
                message = "Registro exitosamente creado",
                newId = newCollaborator.Id,
                imageUrl = imageUrl,
            });
        }
    }
}