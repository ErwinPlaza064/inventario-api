using InventarioApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Priorizar la variable de entorno de Railway para la conexión
var connectionString = Environment.GetEnvironmentVariable("MYSQL_URL") 
                      ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<InventarioDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers(); 
builder.Services.AddOpenApi();

builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        if (builder.Environment.IsDevelopment()) {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            // URL real de tu frontend en Vercel
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://inventario-app-pi.vercel.app", 
                    "https://inventario-app-erwinplaza064s-projects.vercel.app" // Agregué el formato común de Vercel
                };
            
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build(); 

using (var scope = app.Services.CreateScope()) {
    try {
        var context = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
        context.Database.EnsureCreated();
    } catch (Exception ex) {
        Console.WriteLine($"Error al inicializar la BD: {ex.Message}");
    }
}

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors("PermitirFrontend"); 
app.MapControllers();

app.Run();

