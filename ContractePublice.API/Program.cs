using System.Threading.RateLimiting;
using ContractePublice.Infrastructure.Persistence;
using ContractePublice.Infrastructure.Services.Anomalies;
using ContractePublice.Infrastructure.Services.Import;
using ContractePublice.Infrastructure.Services.News;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection nu este setat. În dezvoltare, pune-l în appsettings.Development.json " +
        "(fișier ignorat de git). În producție, setează variabila de mediu ConnectionStrings__DefaultConnection.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<DataGovImportService>();
builder.Services.AddScoped<DataGovImportService>();
builder.Services.AddScoped<AnomalyDetectionService>();
builder.Services.AddHostedService<RecurringImportHostedService>();
// Inlocuieste cu o implementare reala (Bing/Google/Serper) cand exista o cheie API disponibila.
builder.Services.AddScoped<INewsSearchService, NotConfiguredNewsSearchService>();
builder.Services.AddControllers();

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
    });
});

// Limitare de rată: previne bombardarea API-ului public. Endpoint-urile de citire au o fereastră
// generoasă; import-ul (deja protejat cu cheie) rămâne separat, nelimitat de această politică.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var app = builder.Build();

// Auto-aplică migrațiile la pornire
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("Frontend");
app.UseRateLimiter();

// Declanșează manual o verificare + import complet (toate rapoartele disponibile pe data.gov.ro,
// din toți anii, sărind peste ce e deja importat). Rulează în fundal — răspunde imediat cu 202,
// progresul se vede pe /api/import/status. Protejat printr-o cheie API (X-Import-Key).
app.MapPost("/api/import", (HttpRequest request, IServiceScopeFactory scopeFactory, IConfiguration config) =>
{
    var expectedKey = config["ImportApiKey"];
    if (string.IsNullOrEmpty(expectedKey))
        return Results.Problem("ImportApiKey nu este configurată pe server.", statusCode: 500);

    if (!request.Headers.TryGetValue("X-Import-Key", out var providedKey) ||
        providedKey != expectedKey)
        return Results.Unauthorized();

    _ = Task.Run(async () =>
    {
        using var scope = scopeFactory.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<DataGovImportService>();
        await importService.ImportAllAvailableAsync(CancellationToken.None);
    });

    return Results.Accepted(value: new { message = "Import pornit în fundal. Verifică /api/import/status pentru progres." });
});

app.MapGet("/api/import/status", async (AppDbContext db) =>
{
    var logs = await db.DataImportLogs
        .OrderByDescending(l => l.ImportedAt)
        .Take(50)
        .Select(l => new { l.Id, l.Source, l.ImportedAt, l.RecordsImported, l.Status, l.Notes })
        .ToListAsync();
    return Results.Ok(logs);
});

app.MapControllers();
app.UseHttpsRedirection();
app.Run();
