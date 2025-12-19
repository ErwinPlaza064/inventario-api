using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;

using Microsoft.AspNetCore.Authorization;

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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Nota>>> GetNotas()
    {
        return await _context.Notas.OrderByDescending(n => n.FechaCreacion).ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Nota>> PostNota(Nota nota)
    {
        _context.Notas.Add(nota);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetNotas), new { id = nota.Id }, nota);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNota(int id)
    {
        var nota = await _context.Notas.FindAsync(id);
        if (nota == null) return NotFound();
        _context.Notas.Remove(nota);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
