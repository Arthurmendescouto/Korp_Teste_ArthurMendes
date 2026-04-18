using System.Text.Json;
using System.Reflection;
using EstoqueService.Data;
using EstoqueService.Exceptions;
using EstoqueService.Repositories;
using EstoqueService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Microsserviço Estoque — Produtos e saldos",
        Version = "v1",
        Description = "Cadastro de produtos e ajuste de saldo. Consumido pelo Faturamento ao imprimir notas."
    });

    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var path = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(path))
        options.IncludeXmlComments(path, includeControllerXmlComments: true);
});

var connection = configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=estoque_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseNpgsql(connection));

builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;
                if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
                if (Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    if (uri.IsLoopback) return true;
                    if (uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            })
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Estoque v1");
    options.RoutePrefix = "swagger";
});

app.UseCors("Frontend");
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    try
    {
        await DbSeeder.SeedAsync(db);
        loggerFactory.CreateLogger("DbSeeder").LogInformation("Banco de estoque semeado com dados iniciais.");
    }
    catch (Exception ex)
    {
        loggerFactory.CreateLogger("DbSeeder").LogError(ex, "Erro ao semear o banco de estoque. A API seguirá disponível para diagnóstico.");
    }
}

app.Run();
