using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContractePublice.Infrastructure.Services.Import;

// Ruleaza la pornirea aplicatiei si apoi periodic, in fundal: verifica pe data.gov.ro daca au
// aparut rapoarte noi (luna curenta se publica treptat) si le importa automat. Sursele deja
// importate cu succes sunt sarite, deci rularile repetate sunt ieftine.
public class RecurringImportHostedService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly IServiceProvider _services;
    private readonly ILogger<RecurringImportHostedService> _logger;

    public RecurringImportHostedService(IServiceProvider services, ILogger<RecurringImportHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var importService = scope.ServiceProvider.GetRequiredService<DataGovImportService>();
                _logger.LogInformation("Verificare automata pentru rapoarte noi pe data.gov.ro...");
                await importService.ImportAllAvailableAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Eroare in verificarea automata a rapoartelor.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // aplicatia se opreste
            }
        }
    }
}
