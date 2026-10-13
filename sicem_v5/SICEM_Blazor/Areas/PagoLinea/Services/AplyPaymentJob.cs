using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Sicem_Blazor.Services;

namespace Sicem_Blazor.PagoLinea.Services;

public class AplyPaymentJob : BackgroundService
{
    private readonly PagoLineaUploadFilesData data;
    private readonly ILogger<AplyPaymentJob> logger;
    private readonly IServiceProvider serviceProvider;

    public AplyPaymentJob(PagoLineaUploadFilesData data, ILogger<AplyPaymentJob> logger, IServiceProvider serviceProvider)
    {
        this.data = data;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan jobTimeout = TimeSpan.FromHours(1);
        while(!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IPagoLineaService>();
            try
            {
                this.logger.LogInformation("Comenzando a procesar los pagos pendientes de PagoLinea a {Time}", DateTimeOffset.Now);
                await processor.AplicarPagosPendientes(stoppingToken);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error processing transacciones");
            }
            finally
            {
                this.logger.LogInformation("Finalizado proceso de pagos pendientes de PagoLinea {Time}", DateTimeOffset.Now);
            }
            await Task.Delay(jobTimeout, stoppingToken);
        }
    }

}
