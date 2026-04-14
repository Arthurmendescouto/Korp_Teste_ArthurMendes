using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true));
    });
builder.Services.AddHealthChecks();

var connection = configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Database=faturamento_db;Username=postgres;Password=postgres";
builder.Services.AddDbContext<Faturamento.Data.FaturamentoDbContext>(options =>
    options.UseNpgsql(connection));

builder.Services.AddHttpClient("estoque", client =>
{
    var baseUrl = configuration.GetValue<string>("EstoqueServiceBaseUrl") ?? "http://localhost:5259";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<Faturamento.Repositories.IInvoiceRepository, Faturamento.Repositories.InvoiceRepository>();
builder.Services.AddScoped<Faturamento.Services.IInvoiceService, Faturamento.Services.InvoiceService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Microsserviço Faturamento — Notas fiscais",
        Version = "v1",
        Description = "Gestão de notas fiscais. A impressão integra com o microsserviço de Estoque para baixa de saldo."
    });
});

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
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Faturamento v1");
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
    var db = scope.ServiceProvider.GetRequiredService<Faturamento.Data.FaturamentoDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    try
    {
        await Faturamento.Data.DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Falha ao executar seed do banco de faturamento.");
    }
}

app.Run();
