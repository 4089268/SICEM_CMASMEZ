using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MatBlazor;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.Models;
using Sicem_Blazor.Models.PagoLinea;
using Sicem_Blazor.Services;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Services;

using Radzen;
using Radzen.Blazor;
namespace Sicem_Blazor.PagoLinea.Views;
public partial class DetalleOficinaVtn
{
    [Inject]
    public IMatToaster Toaster {get;set;} = default!;
    
    [Inject]
    public IPagoLineaService PagoLineaService {get;set;} = default!;
    
    [Inject]
    public ILogger<DetalleOficinaVtn> Logger {get;set;} = default!;
    
    [Parameter]
    public EventCallback CerrarModal { get; set; }

    public bool mostrarVentana = false;
    
    private ResumeOffice _resumeOffice1;
    public string titulo = "Pagos realizados";
    private RadzenDataGrid<PaymentDetails> DataGrid {get;set;}
    public List<PaymentDetails> Datos {get;set;}
    public List<ResumeDay> IncomeDays {get;set;}
    
    public void Show(ResumeOffice resumeOffice, IEnumerable<PaymentDetails> datos, IEnumerable<ResumeDay> days, string titulo = "")
    {
        this._resumeOffice1 = resumeOffice;
        this.Datos = datos.ToList();
        this.IncomeDays = days.ToList();
        if(!string.IsNullOrWhiteSpace(titulo))
        {
            this.titulo = titulo;
        }
        mostrarVentana = true;
        StateHasChanged();
    }

    private async Task HandleCloseModal()
    {
        mostrarVentana = false;
        StateHasChanged();
        await CerrarModal.InvokeAsync("");
    }
    // TODO-EXPORT-RADZEN: exportación pendiente (ver docs/MIGRACION.md). Código original de Syncfusion:
    /*
    private async Task ExportData()
    {
        await this.DataGrid.ExcelExport(new ExcelExportProperties {
            FileName = string.Format("sicem_ingresosxConceptos_{0}.xlsx", Guid.NewGuid().ToString().Replace("-", ""))
        });
    }
    */

}