using EstoqueService.Data;
using EstoqueService.Repositories;
using EstoqueService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// Temporariamente desabilitado para evitar TypeLoadException por conflito de assemblies
// builder.Services.AddSwaggerGen();

// Configure Postgres EF Core
var connection = configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=estoque_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<EstoqueDbContext>(options =>
    options.UseNpgsql(connection)
);

// DI registrations
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Temporariamente desabilitado enquanto alinha dependências
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Seed do banco para testes rápidos (cria esquema e popula se vazio)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    try
    {
        await DbSeeder.SeedAsync(db);
        loggerFactory.CreateLogger("DbSeeder").LogInformation("Banco semeado com dados iniciais.");
    }
    catch (Exception ex)
    {
        loggerFactory.CreateLogger("DbSeeder").LogError(ex, "Erro ao semear o banco.");
        throw;
    }
}

app.Run();