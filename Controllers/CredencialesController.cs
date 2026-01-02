using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioApi.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

using System.Text;
using System.Security.Cryptography;

namespace InventarioApi.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class CredencialesController : ControllerBase
{
    private readonly InventarioDbContext _context;
    
    // Clave de encriptación (Debería estar en ENV en prod, aquí simplificada)
    // 32 chars = 256 bits
    private static readonly string EncryptionKey = "E1F2A3B4C5D6E7F8A1B2C3D4E5F6A7B8"; 

    public CredencialesController(InventarioDbContext context)
    {
        _context = context;
    }

    private string Encrypt(string clearText)
    {
        try {
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    return "ENC:" + Convert.ToBase64String(ms.ToArray());
                }
            }
        } catch { return clearText; }
    }

    private string Decrypt(string cipherText)
    {
        try {
            if (!cipherText.StartsWith("ENC:")) return cipherText; // Return as-is if not encrypted
            cipherText = cipherText.Replace("ENC:", "");
            
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    return Encoding.Unicode.GetString(ms.ToArray());
                }
            }
        } catch { return cipherText; }
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
        var creds = await _context.Credenciales
            .Where(c => c.UsuarioId == userId)
            .OrderByDescending(c => c.FechaCreacion)
            .ToListAsync();
            
        // Decrypt on the fly
        foreach (var c in creds) 
        {
            c.Valor = Decrypt(c.Valor);
        }
        
        return creds;
    }

    [HttpPost]
    public async Task<ActionResult<Credencial>> PostCredencial(Credencial credencial)
    {
        var userId = await GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        credencial.UsuarioId = userId;
        
        // Encrypt before saving
        credencial.Valor = Encrypt(credencial.Valor);
        
        _context.Credenciales.Add(credencial);
        await _context.SaveChangesAsync();
        
        // Registrar actividad
        var actividad = new Actividad
        {
            Tipo = TipoActividad.CredencialCreada,
            Descripcion = $"Credencial creada: {credencial.Titulo}",
            ReferenciaId = credencial.Id,
            ReferenciaInfo = credencial.Categoria,
            UsuarioId = userId
        };
        _context.Actividades.Add(actividad);
        await _context.SaveChangesAsync();
        
        credencial.Valor = Decrypt(credencial.Valor); 
        
        return CreatedAtAction(nameof(GetCredenciales), new { id = credencial.Id }, credencial);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCredencial(int id, Credencial credencial)
    {
        if (id != credencial.Id) return BadRequest();
        
        var userId = await GetCurrentUserId();
        var existingCredencial = await _context.Credenciales.FindAsync(id);
        
        if (existingCredencial == null) return NotFound();
        if (existingCredencial.UsuarioId != userId) return Forbid();
        
        // Update fields
        existingCredencial.Titulo = credencial.Titulo;
        existingCredencial.Valor = Encrypt(credencial.Valor);
        existingCredencial.Usuario = credencial.Usuario;
        existingCredencial.Categoria = credencial.Categoria;
        
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CredencialExists(id))
                return NotFound();
            throw;
        }
        
        return NoContent();
    }

    private bool CredencialExists(int id)
    {
        return _context.Credenciales.Any(e => e.Id == id);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCredencial(int id)
    {
        var userId = await GetCurrentUserId();
        var credencial = await _context.Credenciales.FindAsync(id);
        
        if (credencial == null) return NotFound();
        if (credencial.UsuarioId != userId) return Forbid();

        var titulo = credencial.Titulo;
        var categoria = credencial.Categoria;

        _context.Credenciales.Remove(credencial);
        await _context.SaveChangesAsync();

        // Registrar actividad
        var actividad = new Actividad
        {
            Tipo = TipoActividad.CredencialEliminada,
            Descripcion = $"Credencial eliminada: {titulo}",
            ReferenciaInfo = categoria,
            UsuarioId = userId
        };
        _context.Actividades.Add(actividad);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
