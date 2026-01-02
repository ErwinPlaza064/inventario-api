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

using (var scope = app.Services.CreateScope()) {
    try {
        var context = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
        // Intentar crear base de datos si no existe
        context.Database.EnsureCreated();
        
        // Ejecutar scripts individualmente para evitar errores en bloque
        var tables = new[] {
            @"CREATE TABLE IF NOT EXISTS Usuarios (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Username VARCHAR(255) NOT NULL,
                PasswordHash VARCHAR(500) NOT NULL,
                Rol VARCHAR(50) DEFAULT 'User'
            );",
            @"CREATE TABLE IF NOT EXISTS Tareas (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Titulo VARCHAR(255) NOT NULL,
                Descripcion TEXT,
                Estado INT NOT NULL,
                FechaCreacion DATETIME NOT NULL,
                UsuarioId INT NOT NULL DEFAULT 0
            );",
            @"CREATE TABLE IF NOT EXISTS Notas (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Titulo VARCHAR(255) NOT NULL,
                Contenido TEXT,
                FechaCreacion DATETIME NOT NULL,
                UsuarioId INT NOT NULL DEFAULT 0
            );",
            @"CREATE TABLE IF NOT EXISTS Credenciales (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Titulo VARCHAR(255) NOT NULL,
                Valor TEXT NOT NULL,
                Usuario VARCHAR(255),
                Categoria VARCHAR(100),
                FechaCreacion DATETIME NOT NULL,
                UsuarioId INT NOT NULL DEFAULT 0
            );",
            @"CREATE TABLE IF NOT EXISTS Actividades (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                Tipo INT NOT NULL,
                Descripcion VARCHAR(500) NOT NULL,
                ReferenciaId INT,
                ReferenciaInfo VARCHAR(255),
                FechaCreacion DATETIME NOT NULL,
                UsuarioId INT NOT NULL
            );",
            @"CREATE TABLE IF NOT EXISTS Comentarios (
                Id INT AUTO_INCREMENT PRIMARY KEY,
                TareaId INT NOT NULL,
                Contenido TEXT NOT NULL,
                FechaCreacion DATETIME NOT NULL,
                UsuarioId INT NOT NULL
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

        // Migración simple: Intentar agregar columna UsuarioId si no existe
        var migrations = new[] {
            "ALTER TABLE Tareas ADD COLUMN UsuarioId INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Notas ADD COLUMN UsuarioId INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Tareas ADD COLUMN Categoria INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Notas ADD COLUMN Categoria INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Credenciales ADD COLUMN Categoria VARCHAR(100);",
            "ALTER TABLE Tareas ADD COLUMN Prioridad INT NOT NULL DEFAULT 1;",
            "ALTER TABLE Notas ADD COLUMN Prioridad INT NOT NULL DEFAULT 1;",
            "ALTER TABLE Tareas ADD COLUMN FechaVencimiento DATETIME;"
        };

        foreach (var sql in migrations)
        {
            try {
                // Esto fallará si la columna ya existe, lo cual es esperado/seguro en este contexto simple
                context.Database.ExecuteSqlRaw(sql);
            } catch (Exception ex) {
                // Ignorar error si la columna ya existe
                 Console.WriteLine($"Migración (probablemente ya aplicada): {ex.Message}");
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

