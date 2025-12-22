using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;

using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace InventarioApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TareasController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public TareasController(InventarioDbContext context)
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
    public async Task<ActionResult<IEnumerable<Tarea>>> GetTareas()
    {
        var userId = await GetCurrentUserId();
        return await _context.Tareas
            .Where(t => t.UsuarioId == userId)
            .OrderByDescending(t => t.FechaCreacion)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<Tarea>> PostTarea(Tarea tarea)
    {
        var userId = await GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        tarea.UsuarioId = userId;
        _context.Tareas.Add(tarea);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetTareas), new { id = tarea.Id }, tarea);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTarea(int id, Tarea tarea)
    {
        if (id != tarea.Id) return BadRequest();
        
        var userId = await GetCurrentUserId();
        var existingTask = await _context.Tareas.FindAsync(id);
        
        if (existingTask == null) return NotFound();
        if (existingTask.UsuarioId != userId) return Forbid();

        // Preserve ownership and creation date logic if needed, or just update fields
        // Safer to just update allowed fields or attach. 
        // For now, simpler: check ownership, then update.
        // But since we are replacing the object in EF (Entry.State = Modified), we must ensure UsuarioId isn't overwritten or check it.
        
        // Better pattern for update:
        existingTask.Titulo = tarea.Titulo;
        existingTask.Descripcion = tarea.Descripcion;
        existingTask.Estado = tarea.Estado;
        // Do not update UsuarioId or FechaCreacion usually
        
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTarea(int id)
    {
        var userId = await GetCurrentUserId();
        var tarea = await _context.Tareas.FindAsync(id);
        
        if (tarea == null) return NotFound();
        if (tarea.UsuarioId != userId) return Forbid();

        _context.Tareas.Remove(tarea);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
