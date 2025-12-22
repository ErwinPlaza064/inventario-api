using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventarioApi.Data;
using BCrypt.Net;

namespace InventarioApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public AuthController(InventarioDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<Usuario>> Register(UsuarioDto request)
    {
        if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
            return BadRequest("El usuario ya existe.");

        var user = new Usuario
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Rol = "User"
        };

        _context.Usuarios.Add(user);
        await _context.SaveChangesAsync();
        return Ok(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(UsuarioDto request)
    {
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return BadRequest("Usuario o contraseña incorrectos.");

        var token = CreateToken(user);
        return Ok(new { token, username = user.Username });
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<Usuario>> UpdateProfile(UsuarioDto request)
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null) return NotFound("Usuario no encontrado.");

        if (!string.IsNullOrEmpty(request.Username) && request.Username != user.Username)
        {
            if (await _context.Usuarios.AnyAsync(u => u.Username == request.Username))
                return BadRequest("El nombre de usuario ya está en uso.");
            
            user.Username = request.Username;
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();
        return Ok(new { username = user.Username });
    }

    private string CreateToken(Usuario user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Tu_Clave_Secreta_Super_Larga_De_Seguridad_IT_123!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Rol)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class UsuarioDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
