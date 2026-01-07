using InventarioApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DEL PUERTO PARA RENDER ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- 2. CONFIGURACIÓN DE CADENA DE CONEXIÓN ---
string connectionString;
var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");

if (!string.IsNullOrEmpty(mysqlHost))
{
    // Si usas variables separadas (como en Railway)
    var mysqlPort = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
    var mysqlUser = Environment.GetEnvironmentVariable("MYSQLUSER");
    var mysqlPass = Environment.GetEnvironmentVariable("MYSQLPASSWORD");
    var mysqlDb = Environment.GetEnvironmentVariable("MYSQLDATABASE");
    
    connectionString = $"Server={mysqlHost};Port={mysqlPort};User={mysqlUser};Password={mysqlPass};Database={mysqlDb};CharSet=utf8mb4;";
}
else
{
    // Si usas una variable única (Render suele usar DATABASE_URL o la configurada en appsettings)
    connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? "";
}

builder.Services.AddDbContext<InventarioDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 33))));

builder.Services.AddControllers(); 
builder.Services.AddOpenApi();

// --- 3. CONFIGURACIÓN JWT ---
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

// --- 4. CONFIGURACIÓN DE CORS ---
builder.Services.AddCors(options => {
    options.AddPolicy("PermitirFrontend", policy => {
        policy.WithOrigins(
                "https://inventario-app-amber.vercel.app",
                "http://localhost:3000",
                "http://localhost:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build(); 

// --- 5. INICIALIZACIÓN DE BASE DE DATOS (Auto-Migración) ---
using (var scope = app.Services.CreateScope()) {
    try {
        var context = scope.ServiceProvider.GetRequiredService<InventarioDbContext>();
        context.Database.EnsureCreated();
        
        var tables = new[] {
            @"CREATE TABLE IF NOT EXISTS Usuarios (Id INT AUTO_INCREMENT PRIMARY KEY, Username VARCHAR(255) NOT NULL, PasswordHash VARCHAR(500) NOT NULL, Rol VARCHAR(50) DEFAULT 'User');",
            @"CREATE TABLE IF NOT EXISTS Tareas (Id INT AUTO_INCREMENT PRIMARY KEY, Titulo VARCHAR(255) NOT NULL, Descripcion TEXT, Estado INT NOT NULL, FechaCreacion DATETIME NOT NULL, UsuarioId INT NOT NULL DEFAULT 0);",
            @"CREATE TABLE IF NOT EXISTS Notas (Id INT AUTO_INCREMENT PRIMARY KEY, Titulo VARCHAR(255) NOT NULL, Contenido TEXT, FechaCreacion DATETIME NOT NULL, UsuarioId INT NOT NULL DEFAULT 0);",
            @"CREATE TABLE IF NOT EXISTS Credenciales (Id INT AUTO_INCREMENT PRIMARY KEY, Titulo VARCHAR(255) NOT NULL, Valor TEXT NOT NULL, Usuario VARCHAR(255), Categoria VARCHAR(100), FechaCreacion DATETIME NOT NULL, UsuarioId INT NOT NULL DEFAULT 0);",
            @"CREATE TABLE IF NOT EXISTS Actividades (Id INT AUTO_INCREMENT PRIMARY KEY, Tipo INT NOT NULL, Descripcion VARCHAR(500) NOT NULL, ReferenciaId INT, ReferenciaInfo VARCHAR(255), FechaCreacion DATETIME NOT NULL, UsuarioId INT NOT NULL);",
            @"CREATE TABLE IF NOT EXISTS Comentarios (Id INT AUTO_INCREMENT PRIMARY KEY, TareaId INT NOT NULL, Contenido TEXT NOT NULL, FechaCreacion DATETIME NOT NULL, UsuarioId INT NOT NULL);"
        };

        foreach (var sql in tables)
        {
            try { context.Database.ExecuteSqlRaw(sql); } catch { /* Ignorar si existe */ }
        }

        var migrations = new[] {
            "ALTER TABLE Tareas ADD COLUMN IF NOT EXISTS UsuarioId INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Notas ADD COLUMN IF NOT EXISTS UsuarioId INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Tareas ADD COLUMN IF NOT EXISTS Categoria INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Notas ADD COLUMN IF NOT EXISTS Categoria INT NOT NULL DEFAULT 0;",
            "ALTER TABLE Tareas ADD COLUMN IF NOT EXISTS Prioridad INT NOT NULL DEFAULT 1;",
            "ALTER TABLE Notas ADD COLUMN IF NOT EXISTS Prioridad INT NOT NULL DEFAULT 1;",
            "ALTER TABLE Tareas ADD COLUMN IF NOT EXISTS FechaVencimiento DATETIME;",
            // Agregar índice único en Username si no existe
            "CREATE UNIQUE INDEX IF NOT EXISTS IX_Usuarios_Username ON Usuarios(Username);"
        };

        foreach (var sql in migrations)
        {
            try { context.Database.ExecuteSqlRaw(sql); } catch { /* Ignorar errores de migración */ }
        }
        
        // Configurar foreign keys con RESTRICT si no existen
        var foreignKeys = new[] {
            @"ALTER TABLE Tareas ADD CONSTRAINT FK_Tareas_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE;",
            @"ALTER TABLE Notas ADD CONSTRAINT FK_Notas_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE;",
            @"ALTER TABLE Credenciales ADD CONSTRAINT FK_Credenciales_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE;",
            @"ALTER TABLE Comentarios ADD CONSTRAINT FK_Comentarios_Usuarios_UsuarioId FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id) ON DELETE RESTRICT ON UPDATE CASCADE;"
        };
        
        foreach (var sql in foreignKeys)
        {
            try { context.Database.ExecuteSqlRaw(sql); } catch { /* Ignorar si ya existe */ }
        }
        
        Console.WriteLine("✅ Base de datos lista.");
    } catch (Exception ex) {
        Console.WriteLine($"❌ Error BD: {ex.Message}");
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