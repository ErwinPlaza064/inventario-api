namespace InventarioApi;

using System.Text.Json.Serialization;

public enum TaskStatus
{
    Pendiente,
    EnProceso,
    Completada
}

public enum TaskCategory
{
    Hardware,
    Software,
    Redes,
    Documentacion,
    Mantenimiento
}

public enum TaskPriority
{
    Baja,
    Media,
    Alta,
    Urgente
}

public class Tarea
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public TaskStatus Estado { get; set; } = TaskStatus.Pendiente;
    public TaskCategory? Categoria { get; set; } = TaskCategory.Hardware;
    public TaskPriority Prioridad { get; set; } = TaskPriority.Media;
    public DateTime? FechaVencimiento { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    
    [JsonIgnore]
    public Usuario? Usuario { get; set; }
}

public class Nota
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public TaskPriority Prioridad { get; set; } = TaskPriority.Media;
    public TaskCategory Categoria { get; set; } = TaskCategory.Documentacion;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    
    [JsonIgnore]
    public Usuario? Usuario { get; set; }
}

public class Credencial
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty; // Ej: "Router Planta Baja"
    public string Valor { get; set; } = string.Empty;  // Ej: "admin123"
    public string? Usuario { get; set; }              // Ej: "admin" (Opcional)
    public string? Categoria { get; set; }            // Ej: "General", "WiFi", etc.
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    
    [JsonIgnore]
    public Usuario? UsuarioNavegacion { get; set; }  // Renombrado para evitar conflicto con Usuario string
}

public class Usuario
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    
    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "User"; // Admin o User
    
    // Propiedades de navegación - ignoradas en JSON
    [JsonIgnore]
    public ICollection<Tarea> Tareas { get; set; } = new List<Tarea>();
    [JsonIgnore]
    public ICollection<Nota> Notas { get; set; } = new List<Nota>();
    [JsonIgnore]
    public ICollection<Credencial> Credenciales { get; set; } = new List<Credencial>();
    [JsonIgnore]
    public ICollection<Comentario> Comentarios { get; set; } = new List<Comentario>();
}

public class Comentario
{
    public int Id { get; set; }
    public int TareaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
    
    [JsonIgnore]
    public Usuario? Usuario { get; set; }
}

public enum TipoActividad
{
    TareaCreada,
    TareaActualizada,
    TareaCompletada,
    TareaEliminada,
    ComentarioAgregado,
    NotaCreada,
    NotaActualizada,
    NotaEliminada,
    CredencialCreada,
    CredencialEliminada
}

public class Actividad
{
    public int Id { get; set; }
    public TipoActividad Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public int? ReferenciaId { get; set; }  // ID del objeto relacionado (tarea, nota, etc)
    public string? ReferenciaInfo { get; set; }  // Info adicional (título, categoría, etc)
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int UsuarioId { get; set; }
}
