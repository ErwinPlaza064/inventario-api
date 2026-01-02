using Microsoft.EntityFrameworkCore;

namespace InventarioApi.Data;

public class InventarioDbContext : DbContext
{
    public InventarioDbContext(DbContextOptions<InventarioDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tarea> Tareas { get; set; }
    public DbSet<Nota> Notas { get; set; }
    public DbSet<Credencial> Credenciales { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Comentario> Comentarios { get; set; }
    public DbSet<Actividad> Actividades { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.ToTable("Tareas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Nota>(entity =>
        {
            entity.ToTable("Notas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Credencial>(entity =>
        {
            entity.ToTable("Credenciales");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Valor).IsRequired();
        });

        modelBuilder.Entity<Comentario>(entity =>
        {
            entity.ToTable("Comentarios");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Contenido).IsRequired();
        });

        modelBuilder.Entity<Actividad>(entity =>
        {
            entity.ToTable("Actividades");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Descripcion).IsRequired().HasMaxLength(500);
        });
    }
}
