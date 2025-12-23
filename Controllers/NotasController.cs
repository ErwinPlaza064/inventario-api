using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace InventarioApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotasController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public NotasController(InventarioDbContext context)
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
    public async Task<ActionResult<IEnumerable<Nota>>> GetNotas()
    {
        var userId = await GetCurrentUserId();
        return await _context.Notas
            .Where(n => n.UsuarioId == userId)
            .OrderByDescending(n => n.FechaCreacion)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Nota>> PostNota(Nota nota)
    {
        var userId = await GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        nota.UsuarioId = userId;
        _context.Notas.Add(nota);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetNotas), new { id = nota.Id }, nota);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutNota(int id, Nota nota)
    {
        if (id != nota.Id) return BadRequest();

        var userId = await GetCurrentUserId();
        var existingNota = await _context.Notas.FindAsync(id);

        if (existingNota == null) return NotFound();
        if (existingNota.UsuarioId != userId) return Forbid();

        existingNota.Titulo = nota.Titulo;
        existingNota.Contenido = nota.Contenido;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNota(int id)
    {
        var userId = await GetCurrentUserId();
        var nota = await _context.Notas.FindAsync(id);
        
        if (nota == null) return NotFound();
        if (nota.UsuarioId != userId) return Forbid();

        _context.Notas.Remove(nota);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
