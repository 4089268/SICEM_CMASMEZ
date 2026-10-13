using System;
using Microsoft.Extensions.DependencyInjection;
using Sicem_Blazor.Services;

namespace Sicem_Blazor.PagoLinea.Services;

public static class PagoLineaServiceCollectionExtension
{
    public static void AddPagoLineaServices(this IServiceCollection services)
    {
        services.AddScoped<IPagoLineaService, PagoLineaService>();
        services.AddScoped<IPagoLineaRepo, PagoLineaService>();
        services.AddSingleton<PagoLineaUploadFilesData>();
        services.AddSingleton<PagoLineaProcessFileJob>();

        // * Se comenta el siguiente HostedService para pruebas en desarrollo.
        // services.AddHostedService(provider => provider.GetRequiredService<PagoLineaProcessFileJob>());

        // services.AddHostedService<AplyPaymentJob>();
    }
}
