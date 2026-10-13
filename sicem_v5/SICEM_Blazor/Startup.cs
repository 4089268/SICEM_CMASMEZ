using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using MatBlazor;
using Radzen;
using Sicem_Blazor.Models;
using Sicem_Blazor.Services;
using Sicem_Blazor.Helpers;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Filtros;
using Sicem_Blazor.Data.Whatsapp;
using Sicem_Blazor.Recaudacion.Data;
using Sicem_Blazor.Descuentos.Data;
using Sicem_Blazor.SeguimientoCobros.Data;
using Sicem_Blazor.PagoLinea.Services;

namespace Sicem_Blazor
{
    public class Startup
    {

        public IConfiguration Configuration { get; }
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }


        // This method gets1 called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddMatBlazor();
            services.AddControllers();
            services.AddDbContext<SicemContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("SICEM"));
            });

            services.AddLogging( config =>
            {
                config.AddConsole();
                config.AddEventSourceLogger();
            });

            services.Configure<MessageNotificationSettings>(Configuration.GetSection("MessageNotification"));
            services.Configure<ApiAuthSettings>(Configuration.GetSection("ApiAuth"));
            services.AddSingleton<SessionService>();
            services.AddScoped<SicemService>();
            services.AddScoped<RadzenDataGridExportacionExcelService>();
            services.AddScoped<ConsultaGralService>();
            services.AddScoped<IRecaudacionService, RecaudacionService>();
            services.AddScoped<ControlRezagoService>();
            services.AddScoped<MicromedicionService>();
            services.AddScoped<IDescuentosService, DescuentosService>();
            services.AddScoped<IEficienciaService, EficienciaService>();
            services.AddScoped<FacturacionService>();
            services.AddScoped<OrdenesService>();
            services.AddScoped<PadronService>();
            services.AddScoped<ConceptosService>();
            services.AddScoped<LogeadoFilter>();
            services.AddScoped<NotificacionesTemplateService>();
            services.AddPagoLineaServices();
            services.AddSingleton<IncomeOfficeService>( provider => new IncomeOfficeService( Configuration.GetConnectionString("ARQUOS") ));
            services.AddSingleton<LecturasService>();
            services.AddHttpClient("notificacionApi", client =>
            {
                client.BaseAddress = new Uri("http://nerus.sytes.net:3001/lead");
            });
            services.AddWhatsappService(Configuration);

            services.AddServerSideBlazor();
            services.AddRadzenComponents();
            services.AddMatToaster(config =>
            {
                config.Position = MatToastPosition.TopCenter;
                config.PreventDuplicates = true;
                config.NewestOnTop = true;
                config.ShowCloseButton = true;
                config.MaximumOpacity = 95;
                config.VisibleStateDuration = 4000;
            });
            
            services.AddCors(policy =>
            {
                policy.AddPolicy("CorsPolicy", opt => opt
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var _ci = new CultureInfo("es-MX");
            app.UseRequestLocalization( new RequestLocalizationOptions()
            {
                DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(_ci),
                SupportedCultures = new List<CultureInfo>(){_ci},
                SupportedUICultures = new List<CultureInfo>(){_ci}
            });

            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
                //app.UseHttpsRedirection();
            }
            
            app.UseStaticFiles();
            app.UseRouting();
            app.UseRequestLocalization();
            app.UseCors("CorsPolicy");
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
