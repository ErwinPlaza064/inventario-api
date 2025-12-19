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
    public DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarea>(entity =>
        {
            entity.ToTable("tareas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(255);
        });

        modelBuilder.Entity<Nota>(entity =>
        {
            entity.ToTable("notas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Titulo).IsRequired().HasMaxLength(255);
        });
    }
}
