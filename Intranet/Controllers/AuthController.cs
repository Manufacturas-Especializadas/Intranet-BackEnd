using Intranet.Dtos;
using Intranet.Models;
using Intranet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Intranet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuthService _authService;

        public AuthController(AppDbContext context, AuthService service)
        {
            _context = context;
            _authService = service;
        }

        [HttpGet]
        [Route("GetRoles")]
        public async Task<IActionResult> GetRolesAsync()
        {
            var roles = await _context.Roles
                        .AsNoTracking()
                        .ToListAsync();

            if (roles == null) return BadRequest("Lista vacia");


            return Ok(roles);
        }

        [HttpGet]
        [Route("GetDepartments")]
        public async Task<IActionResult> GetDepartmentsAsync()
        {
            var departments = await _context.Departments
                            .AsNoTracking()
                            .ToListAsync();

            if (departments == null) return BadRequest("Lista vacia");

            return Ok(departments);
        }

        [HttpGet]
        [Route("GetUserById/{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if(user == null)
            {
                return NotFound("Usuario no encontrado");
            }

            return Ok(user);
        }

        [HttpPost]
        [Route("Logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Usuario no autenticado" });
            }

            var success = await _authService.LogoutAsync(userId);

            if (!success)
            {
                return NotFound(new { message = "Usuario no encontrado" });
            }

            return Ok(new { message = "Sesión cerrada correctamente" });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var response = await _authService.LoginAsync(request);

            if (response == null)
            {
                return Unauthorized(new { message = "Nombre, número de nómina incorrectos" });
            }

            return Ok(response);
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
        {
            try
            {
                var user = await _authService.RegisterAsync(request);

                if (user == null)
                {
                    return BadRequest(new { message = "Ya existe un usuario con ese nombre y número de nómina" });
                }

                return Ok(new { message = "Usuario registrado exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }        

        [HttpPost]
        [Route("Refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return BadRequest("Refresh token es requerido");
            }

            var response = await _authService.RefreshTokenAsync(refreshToken);

            if (response == null)
            {
                return Unauthorized(new { message = "Refresh token inválido o expirado" });
            }

            return Ok(response);
        }
    }
}