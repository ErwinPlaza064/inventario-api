using InventarioApi.Data;
using Microsoft.EntityFrameworkCore;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Construir la cadena de conexión usando las variables individuales de Railway
// ... (imports remain the same)

// Construir la cadena de conexión usando las variables individuales de Railway
string connectionString;
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");

if (!string.IsNullOrEmpty(mysqlHost))
{
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT");
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
    var mysqlPass = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
    var mysqlDb = Environment.GetEnvironmentVariable("MYSQLDATABASE");
    
    // Simplificamos la cadena y aseguramos CharSet
    connectionString = $"Server={mysqlHost};Port={mysqlPort};User={mysqlUser};Password={mysqlPass};Database={mysqlDb};CharSet=utf8mb4;";
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

// --- CONFIGURACIÓN JWT ---
var key = Encoding.ASCII.GetBytes("Tu_Clave_Secreta_Super_Larga_De_Seguridad_IT_123!");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build(); 

    try {
        var context = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
        // Intentar crear base de datos si no existe
        context.Database.EnsureCreated();
        
        // Ejecutar scripts individualmente para evitar errores en bloque
        var tables = new[] {
            @"CREATE TABLE IF NOT EXISTS usuarios (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Username VARCHAR(255) NOT NULL,
                PasswordHash VARCHAR(500) NOT NULL,
                Rol VARCHAR(50) DEFAULT 'User'
            );",
            @"CREATE TABLE IF NOT EXISTS tareas (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Titulo VARCHAR(255) NOT NULL,
                Descripcion TEXT,
                Estado INT NOT NULL,
                FechaCreacion DATETIME NOT NULL
            );",
            @"CREATE TABLE IF NOT EXISTS notas (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Titulo VARCHAR(255) NOT NULL,
                Contenido TEXT,
                FechaCreacion DATETIME NOT NULL
            );"
        };

        foreach (var sql in tables)
        {
            try {
                context.Database.ExecuteSqlRaw(sql);
            } catch (Exception ex) {
                Console.WriteLine($"Error creando tabla: {ex.Message}");
            }
        }
        Console.WriteLine("Inicialización de DB completada.");

    } catch (Exception ex) {
        Console.WriteLine($"Error al inicializar la BD: {ex.ToString()}");
    }
}

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors("PermitirFrontend"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

