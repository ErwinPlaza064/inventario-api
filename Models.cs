namespace InventarioApi;

public enum TaskStatus
{
    Pendiente,
    EnProceso,
    Completada
}

public class Tarea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TaskStatus Estado { get; set; } = TaskStatus.Pendiente;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
}

public class Nota
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
}

public class Credencial
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty; // Ej: "Router Planta Baja"
    public string Valor { get; set; } = string.Empty;  // Ej: "admin123"
    public string? Usuario { get; set; }              // Ej: "admin" (Opcional)
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
}

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "User"; // Admin o User
}
