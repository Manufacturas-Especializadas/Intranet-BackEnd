using Intranet.Dtos;
using Intranet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Intranet.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<Users> _passwordHasher;

        public AuthService(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<Users>();
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return false;
            }

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<TokenResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.IdRoleNavigation)
                .FirstOrDefaultAsync(u => u.PayRollNumber == request.PayRollNumber);

            if (user == null) return null;

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed) return null;

            return await CreateTokenResponseAsync(user);
        }

        public async Task<Users?> RegisterAsync(RegisterRequestDto request)
        {
            if (await _context.Users.AnyAsync(u => u.PayRollNumber == request.PayRollNumber))
            {
                return null;
            }

            var role = await _context.Roles
                    .FirstOrDefaultAsync(r => r.Name == request.RoleName);

            var department = await _context.Departments
                            .FirstOrDefaultAsync(d => d.Name == request.Department);

            if (role == null)
            {
                throw new ArgumentException($"El rol {request.RoleName} no existe");
            }

            if(department == null)
            {
                throw new ArgumentException($"El departamento {request.Department} no existe");
            }

            var user = new Users
            {
                Name = request.Name,
                PayRollNumber = request.PayRollNumber,
                IdRole = role.Id,
                IdDepartment = department.Id,
                PasswordHash = _passwordHasher.HashPassword(null!, request.Password)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<TokenResponseDto?> RefreshTokenAsync(string refreshToken)
        {
            var user = await _context.Users
                    .Include(u => u.IdRoleNavigation)
                    .FirstOrDefaultAsync(u =>
                        u.RefreshToken == refreshToken &&
                        u.RefreshTokenExpiryTime > DateTime.UtcNow);

            if (user == null) return null;

            return await CreateTokenResponseAsync(user);
        }

        private async Task<TokenResponseDto> CreateTokenResponseAsync(Users user)
        {
            var accessToken = CreateAccessToken(user);
            var newRefreshToken = GenerateRefreshToke();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7)
            );

            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 60)
                )
            };
        }

        private string CreateAccessToken(Users user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.IdRoleNavigation.Name),
                new Claim("PayRollNumber", user.PayRollNumber.ToString()!)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    _configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 60)
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToke()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
    }
}