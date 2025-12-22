using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace InventarioApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CredencialesController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public CredencialesController(InventarioDbContext context)
    {
        _context = context;
    }

    private async Task<int> GetCurrentUserId()
    {
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        if (string.IsNullOrEmpty(username)) return 0;
        
        var user = await _context.Usuarios.FirstOrDefaultAsync(u => u.Username == username);
        return user?.Id ?? 0;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Credencial>>> GetCredenciales()
    {
        var userId = await GetCurrentUserId();
        // Ordenadas por fecha descendente
        return await _context.Credenciales
            .Where(c => c.UsuarioId == userId)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Credencial>> PostCredencial(Credencial credencial)
    {
        var userId = await GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        credencial.UsuarioId = userId;
        _context.Credenciales.Add(credencial);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCredenciales), new { id = credencial.Id }, credencial);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCredencial(int id)
    {
        var userId = await GetCurrentUserId();
        var credencial = await _context.Credenciales.FindAsync(id);
        
        if (credencial == null) return NotFound();
        if (credencial.UsuarioId != userId) return Forbid();

        _context.Credenciales.Remove(credencial);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
