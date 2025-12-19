using InventarioApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Construir la cadena de conexión usando las variables individuales de Railway
string connectionString;
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");

if (!string.IsNullOrEmpty(mysqlHost))
{
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT");
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
    var mysqlPass = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
    var mysqlDb = Environment.GetEnvironmentVariable("MYSQLDATABASE");
    
    // Simplificamos la cadena para evitar errores de SSL o formato
    connectionString = $"Server={mysqlHost};Port={mysqlPort};User={mysqlUser};Password={mysqlPass};Database={mysqlDb};";
    Console.WriteLine($"Conectando a base de datos en: {mysqlHost}");
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
}

builder.Services.AddDbContext<InventarioDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

builder.Services.AddControllers(); 
builder.Services.AddOpenApi();

builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        if (builder.Environment.IsDevelopment()) {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        } else {
            // URL real de tu frontend en Vercel
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { 
                    "https://inventario-app-pi.vercel.app", 
                    "https://inventario-app-erwinplaza064s-projects.vercel.app",
                    "https://inventario-app-amber.vercel.app" 
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

