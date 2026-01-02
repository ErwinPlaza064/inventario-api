using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace InventarioApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ActividadesController : ControllerBase
{
    private readonly InventarioDbContext _context;

    public ActividadesController(InventarioDbContext context)
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
    public async Task<ActionResult<IEnumerable<object>>> GetActividades()
    {
        var userId = await GetCurrentUserId();
        
        var actividades = await _context.Actividades
            .Where(a => a.UsuarioId == userId)
            .OrderByDescending(a => a.FechaCreacion)
            .Take(50)
            .Select(a => new {
                a.Id,
                Tipo = a.Tipo.ToString(),
                a.Descripcion,
                a.ReferenciaId,
                a.ReferenciaInfo,
                a.FechaCreacion
            })
            .ToListAsync();

        return Ok(actividades);
    }
}
