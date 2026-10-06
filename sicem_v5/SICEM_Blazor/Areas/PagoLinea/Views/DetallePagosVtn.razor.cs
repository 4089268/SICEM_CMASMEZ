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
using Sicem_Blazor.Services;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Services;

using Radzen;
using Radzen.Blazor;
namespace Sicem_Blazor.PagoLinea.Views;
public partial class DetallePagosVtn
{
    [Inject]
    public ExportacionExcelService ExportacionExcel { get; set; }

    [Inject]
    public IMatToaster Toaster {get;set;} = default!;
    
    [Inject]
    public IPagoLineaService PagoLineaService {get;set;} = default!;
    
    [Inject]
    public ILogger<DetallePagosVtn> Logger {get;set;} = default!;
    
    [Parameter]
    public EventCallback CerrarModal { get; set; }

    public bool mostrarVentana = false;
    
    private IEnlace _enlace = default!;

    public string titulo = "Detalle venta en linea";

    private RadzenDataGrid<OprVenta> DataGrid {get;set;}
    public List<OprVenta> Datos {get;set;}


    public void Show(IEnlace enlace, IEnumerable<OprVenta> datos, string titulo = "")
    {
        if(!string.IsNullOrWhiteSpace(titulo))
        {
            this.titulo = titulo;
        }
        this._enlace = enlace;
        this.Datos = datos.ToList();
        mostrarVentana = true;
        StateHasChanged();
    }

    private async Task HandleCloseModal()
    {
        mostrarVentana = false;
        StateHasChanged();
        await CerrarModal.InvokeAsync("");
    }
    private async Task ExportData()
    {
        await ExportacionExcel.ExportarAsync(this.DataGrid, new ExcelExportProperties
        {
            FileName = string.Format("sicem_ingresosxConceptos_{0}.xlsx", Guid.NewGuid().ToString().Replace("-", ""))
        });
    }

}