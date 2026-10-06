using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MatBlazor;
using Sicem_Blazor.Services;
using Sicem_Blazor.Models;
using Sicem_Blazor.Models.PagoLinea;
using Sicem_Blazor.PagoLinea.Services;

using Radzen;
using Radzen.Blazor;
using DateRange = Sicem_Blazor.Data.DateRange;
namespace Sicem_Blazor.PagoLinea.Views
{
    public partial class PagoLineaPage
    {

        [Inject]
        public NavigationManager NavManager {get;set;} = default!;
        
        [Inject]
        public IMatToaster Toaster {get;set;} = default!;
        
        [Inject]
        public IPagoLineaService PagoLineaService {get;set;} = default!;
        
        [Inject]
        public SicemService SicemService {get;set;} = default!;
        
        [Inject]
        public ILogger<PagoLineaPage> Logger {get;set;} = default!;


        private RadzenDataGrid<ResumeOffice> DataGrid {get;set;}
        private RadzenChart GraficaIngresos { get; set; }
        private DetallePagosVtn _detallePagosVtn;
        private DetalleOficinaVtn _detalleOficinaVtn;

        private bool _busyDialog = false;
        private DateTime _f1, _f2;
        // private int subsistema, sector;
        private ResumeMonth _resumeIncomeMonth = null;
        private List<ResumeOffice> DataOffices {get;set;}
        private List<ChartItem> DatosGraficaImporte { get; set; }

        private CultureInfo _currentCultueInfo = new("es-MX");

        protected override async Task OnInitializedAsync()
        {
            var _now = DateTime.Now;
            this._f1 = new DateTime(_now.Year, _now.Month, 1);
            this._f2 = new DateTime(_now.Year, _now.Month, DateTime.DaysInMonth(_now.Year, _now.Month));

            await Procesar(new SeleccionarFechaEventArgs
            {
                Fecha1 = _f1,
                Fecha2 = _f2,
            });
        }

        public async Task Procesar(SeleccionarFechaEventArgs e)
        {
            Toaster.Add("No implementado", MatToastType.Warning);
            // this._busyDialog = true;
            // await Task.Delay(100);

            // this._f1 = e.Fecha1;
            // this._f2 = e.Fecha2;
            // this.Subsistema = e.Subsistema;
            // this.Sector = e.Sector;

            // _resumeIncomeMonth = await this.PagoLineaService.GetResumeMonth(_f1.Year, _f1.Month);
            // this.DataOffices = _resumeIncomeMonth.Offices.OrderBy(item => item.OfficeName).ToList();


            // // * generate the chart data
            // DatosGraficaImporte = this.DataOffices.Select(item => new ChartItem {
            //     Id = item.OfficeId,
            //     Descripcion = item.OfficeName,
            //     Valor1 = item.TotalIncome
            // }).OrderBy( item => item.Valor1).ToList();
            // await GraficaIngresos.RefreshAsync();

            // this._busyDialog = false;
            // StateHasChanged();
        }
        
        // TODO-EXPORT-RADZEN: exportación pendiente (ver docs/MIGRACION.md). Código original de Syncfusion:
        /*
        private async Task ExportarExcel_Click(){
            var _options = new ExcelExportProperties()
            {
                IncludeHiddenColumn = true
            };
            await this.DataGrid.ExportToExcelAsync(_options);
        }
        */

        private async Task HandleShowDetails(ResumeOffice office)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("");
            // this._busyDialog = true;
            // await Task.Delay(100);

            // IEnumerable<OprVenta> data = [];
            // try
            // {
            //     data = this.PagoLineaService.ObtenerDetallePagos(office.Enlace, new DateRange(_f1,_f2));
            //     if (!data.Any())
            //     {
            //         throw new KeyNotFoundException("No hay datos disponibles");
            //     }
            // }
            // catch (KeyNotFoundException)
            // {
            //     Toaster.Add("No hay datos disponibles para este periodo", MatToastType.Info);
            //     this._busyDialog = false;
            //     return;
            // }
            // catch (System.Exception)
            // {
            //     Toaster.Add("Erro al obtener el detalle", MatToastType.Danger);
            //     this._busyDialog = false;
            //     return;
            // }


            // if(_detallePagosVtn != null)
            // {
            //     var titulo = string.Format("Detalle de pagos del {0} al {1}, {2}", _f1.ToShortDateString(), _f2.ToShortDateString(), office.Enlace.Nombre);
            //     _detallePagosVtn.Show(office.Enlace, data, titulo);
            // }

            // this._busyDialog = false;
        }

        private async Task HandleClosedDetallePagos(object e)
        {
            this.Logger.LogDebug("Detalle Pagos vtn closed");
            await Task.CompletedTask;
        }

        private async Task HandleShowDetailsOffice(ResumeOffice office)
        {
            Toaster.Add("No implementado", MatToastType.Warning);
            // this._busyDialog = true;
            // await Task.Delay(100);

            // try
            // {
            //     var dataMovements = await this.PagoLineaService.GetMovements(_f1, _f2, office.OfficeId);
            //     var dataMovementsDays = await this.PagoLineaService.GetMovementsByDays(_f1.Year, _f2.Month, office.OfficeId);

            //     if(!dataMovements.Any())
            //     {
            //         throw new KeyNotFoundException("No hay datos disponibles");
            //     }

            //     if(_detalleOficinaVtn != null)
            //     {
            //         var titulo = string.Format("{0}, Pagos realizados {1}", office.OfficeName, _f1.ToString("MMM yyyy"));
            //         _detalleOficinaVtn.Show(office, dataMovements, dataMovementsDays, titulo);
            //     }

            //     this._busyDialog = false;

            // }
            // catch (KeyNotFoundException)
            // {
            //     Toaster.Add("No hay datos disponibles para este periodo", MatToastType.Info);
            //     this._busyDialog = false;
            //     return;
            // }
            // catch(Exception err)
            // {
            //     this.Logger.LogError(err, "Fail at retrive the data");
            //     Toaster.Add("Erro al obtener los datos de la oficina.", MatToastType.Danger);
            //     this._busyDialog = false;
            //     return;
            // }
        }

        private async Task HandleClosedDetalleOficina(object e)
        {
            this.Logger.LogDebug("Detalle Oficina vtn closed");
            await Task.CompletedTask;
        }

    }

}