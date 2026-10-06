using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
public partial class PagoLineaDetalleVtn
{
    [Inject]
    public IMatToaster Toaster {get;set;} = default!;
    
    [Inject]
    public IPagoLineaService PagoLineaService1 {get;set;} = default!;
    
    [Inject]
    public ILogger<DetallePagosVtn> Logger {get;set;} = default!;
    
    [Inject]
    private IMatDialogService MatDialogService {get;set;}

    [Parameter]
    public EventCallback CerrarModal { get; set; }



    public bool mostrarVentana = false;
    
    private IEnlace _enlace = default!;

    public string titulo = "Detalle venta en linea";

    private RadzenDataGrid<PagoLineaDetalle> DataGrid {get;set;}
    public List<PagoLineaDetalle> Datos {get;set;}

    private bool _taskOnProgress = false;
    private bool showButton
    {
        get => this.Datos.Where(item => item.Aplicado).Any();
    }

    public void Show(IEnlace enlace, IEnumerable<PagoLineaDetalle> datos, string titulo = "")
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
    
    // TODO-EXPORT-RADZEN: exportación pendiente (ver docs/MIGRACION.md). Código original de Syncfusion:
    /*
    private async Task ExportData()
    {
        await this.DataGrid.ExcelExport(new ExcelExportProperties
        {
            FileName = string.Format("sicem_detallePagoLinea_{0}.xlsx", Guid.NewGuid().ToString().Replace("-", ""))
        });
    }
    */

    private async Task AplicarPagoClick(PagoLineaDetalle detallePago)
    {
        await Task.CompletedTask;
        throw new NotImplementedException("No implementado");

        // var data = this.Datos.FirstOrDefault( item => item.Folio == detallePago.Folio);
        // if(data == null) return;

        // data.aplicarPagoResult = new AplicarPagoResult
        // {
        //     Folio = detallePago.Folio,
        //     Fecha = detallePago.Fecha,
        //     Importe = detallePago.Importe,
        //     Estatus = ResumenOficinaEstatus.Pendiente
        // };
        // await Task.Delay(100);
        // await InvokeAsync(() => StateHasChanged());
        // this.DataGrid.Refresh();

        // // * attempt to send the paymeny
        // var response = await this.PagoLineaService1.AplicarPago(this._enlace, detallePago);
        // if(response.Error > 0)
        // {
        //     if(response.Mensaje.Contains("->"))
        //     {
        //         var message = $"rror al aplicar el pago: {response.Mensaje.Split("->").First().Trim()}".ToLower();
        //         Toaster.Add("E" + message, MatToastType.Danger, $"Pago {detallePago.Id} {detallePago.Fecha.ToShortDateString()}");
        //     }
        //     else
        //     {
        //         var message = $"error al aplicar el pago: {response.Mensaje}".ToLower();
        //         Toaster.Add("E" + message, MatToastType.Danger, $"Pago {detallePago.Id} {detallePago.Fecha.ToShortDateString()}");
        //     }
        // }
        // else
        // {
        //     Toaster.Add("Pago aplicado con folio: " + response.FolioPago, MatToastType.Success, $"Pago {detallePago.Id} {detallePago.Fecha.ToShortDateString()}");
        //     data.Aplicado = data.Importe;
        //     data.Diferencia = 0;
        // }

        // // * updated the status on the grid
        // data.aplicarPagoResult.Estatus = response.Estatus;
        // await Task.Delay(100);
        // await InvokeAsync(() => StateHasChanged());
        // this.DataGrid.Refresh();
    }

    private async Task AplicarPagosClick()
    {
        await Task.CompletedTask;
        throw new NotImplementedException("No implementado");

        // if(_taskOnProgress) return;

        // // * confirmation modal
        // var result = await MatDialogService.AskAsync("Está por aplicar los pagos pendientes, ¿Desea continuar?", new string[] {"Aplicar Pagos", "Cancelar"});
        // if(result != "Aplicar Pagos") return;


        // // * Preparar filas
        // _taskOnProgress = true;
        // var detallesPorAplicar = this.Datos.Where(item => item.Aplicado <= 0).ToList();
        // foreach(var detalle in detallesPorAplicar)
        // {
        //     var det = Datos.FirstOrDefault(item => item.Id == detalle.Id);
        //     if(det != null)
        //     {
        //         det.aplicarPagoResult = new AplicarPagoResult
        //         {
        //             Id = det.Id,
        //             Fecha = det.Fecha,
        //             Importe = det.Importe,
        //             Estatus = ResumenOficinaEstatus.Pendiente
        //         };
        //     }
        // }
        // await InvokeAsync( () => StateHasChanged());

        // foreach(var detalle in detallesPorAplicar)
        // {
        //     await Task.Run( async() => await AplicarPagoClick(detalle));
        // }


        // _taskOnProgress = false;
        // await InvokeAsync( () => StateHasChanged());


        // await MatDialogService.AlertAsync("Finalizado la aplicación de pagos.");
    }
}