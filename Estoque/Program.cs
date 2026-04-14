using System.Text.Json;
using EstoqueService.Data;
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
});

var connection = configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=estoque_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseNpgsql(connection));

builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var corsOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

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
