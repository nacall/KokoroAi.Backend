using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using KokoroAi.Backend.Data;
using KokoroAi.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=kokoro_ai.db";

builder.Services.AddDbContext<KokoroDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Controladores con serialización JSON adecuada
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// 3. Registrar Servicio de Gemini AI con HttpClient
builder.Services.AddHttpClient<IGeminiChatService, GeminiChatService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(90);
});

// 4. Swagger / OpenAPI para pruebas
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Kokoro AI Companion API",
        Version = "v1",
        Description = "API de backend para Kokoro AI: Asistente Virtual y Compañera basada en los wireframes de Figma con Gemini AI."
    });
});

// 5. Configuración de CORS permisivo para desarrollo y túneles remotos (Dev Tunnels / Ngrok / Android)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 6. Inicialización y siembra de SQLite al arrancar
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KokoroDbContext>();
    // Asegura que el archivo kokoro_ai.db exista con todas las tablas
    await dbContext.Database.EnsureCreatedAsync();
    // Carga los datos por defecto (Matias, trajes Seifuku/Lo-Fi/VIP y ajustes de personalidad)
    await KokoroDbContext.SeedAsync(dbContext);
}

// 7. Configurar Middleware Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kokoro AI API v1");
    c.RoutePrefix = string.Empty; // Muestra Swagger directamente en la raíz (http://localhost:5123)
});

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
