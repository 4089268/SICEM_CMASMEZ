using System;
using Microsoft.Extensions.DependencyInjection;

namespace Sicem_Blazor.PagoLinea.Services;

public static class PagoLineaServiceCollectionExtension
{
    public static void AddPagoLineaServices(this IServiceCollection services)
    {
        services.AddScoped<IPagoLineaService, PagoLineaService>();
        services.AddScoped<IPagoLineaRepo, PagoLineaService>();
        services.AddSingleton<PagoLineaUploadFilesData>();
        services.AddSingleton<PagoLineaProcessFileJob>();
        services.AddHostedService(provider => provider.GetRequiredService<PagoLineaProcessFileJob>());
        // services.AddHostedService<AplyPaymentJob>();
    }
}
