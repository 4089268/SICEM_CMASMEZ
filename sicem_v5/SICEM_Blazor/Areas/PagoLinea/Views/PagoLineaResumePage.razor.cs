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
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.PagoLinea.Services;

using Radzen;
using Radzen.Blazor;
using DateRange = Sicem_Blazor.Data.DateRange;
namespace Sicem_Blazor.PagoLinea.Views
{
    public partial class PagoLineaResumePage
    {
        [Inject]
        public RadzenDataGridExportacionExcelService ExportacionExcel { get; set; }


        [Inject]
        public NavigationManager NavManager {get;set;} = default!;
        
        [Inject]
        public IMatToaster Toaster {get;set;} = default!;
        
        [Inject]
        public IPagoLineaService PagoLineaService1 {get;set;} = default!;
        
        [Inject]
        public SicemService SicemService {get;set;} = default!;
        
        [Inject]
        public ILogger<PagoLineaPage> Logger {get;set;} = default!;


        private RadzenDataGrid<PagoLineaResumen> DataGrid {get;set;}
        private bool _busyDialog = false;
        private DateTime _f1, _f2;
        private int _subsistema, _sector;
        private List<PagoLineaResumen> DataOffices {get;set;}
        private PagoLineaResumen _totalData;
        private CultureInfo _currentCultureInfo = new("es-MX");

        private ResumenDiasVtn _resumenDiasVtn;
        private bool _resumenDiasVisible = false;

        private PagoLineaDetalleVtn _pagoLineaDetalleVtn;
        private bool _detalleVtnVisible = false;
        
        protected override async Task OnInitializedAsync()
        {
            var _now = DateTime.Now;
            this._f1 = new DateTime(_now.Year, _now.Month, 1);
            this._f2 = new DateTime(_now.Year, _now.Month, DateTime.DaysInMonth(_now.Year, _now.Month));
            this._subsistema = 0;
            this._sector = 0;

            await Task.CompletedTask;
        }

        public async Task Procesar(SeleccionarFechaEventArgs e)
        {
            this._f1 = e.Fecha1;
            this._f2 = e.Fecha2;
            this._subsistema = e.Subsistema;
            this._sector = e.Sector;

            // * obtener enlaces del usuario
            IEnlace[] enlaces = SicemService.ObtenerOficinasDelUsuario().ToArray();

            // * Preparar filas
            DataOffices = new List<PagoLineaResumen>();
            var Tareas = new List<Task>();
            foreach (var enlace in enlaces)
            {
                var oficinaModel = new PagoLineaResumen(enlace);
                DataOffices.Add(oficinaModel);

                // * Preparar tareas
                Tareas.Add(new Task( async () => await ConsultarOficina(enlace)));
            }

            // * Agregar fila total
            _totalData = new PagoLineaResumen( new Ruta() { Oficina = "TOTAL", Id = 999 });
            

            // * Iniciar tareas
            foreach (var tarea in Tareas)
            {
                tarea.Start();
            }
            await Task.CompletedTask;
        }

        private async Task ConsultarOficina(IEnlace enlace)
        {
            try
            {
                // * Realizar consulta
                var dateRange = new DateRange(_f1, _f2, _subsistema, _sector);
                var tmpData = await PagoLineaService1.ObtenerRumenPagos(enlace, dateRange);
                
                var _random = new Random();
                var sleep = _random.Next(3000);
                System.Threading.Thread.Sleep(sleep);

                // * Refrescar datos grid
                lock(DataOffices)
                {
                    // * Actualizar fila grid
                    var item = DataOffices.Where(item => item.Id == enlace.Id).FirstOrDefault();
                    if (item != null)
                    {
                        if (tmpData.Estatus == ResumenOficinaEstatus.Completado)
                        {
                            item.TotalPagos = tmpData.TotalPagos;
                            item.TotalPagosAplicados = tmpData.TotalPagosAplicados;
                            item.TotalPagosPorAplicar = tmpData.TotalPagosPorAplicar;
                            item.ImportePagos = tmpData.ImportePagos;
                            item.ImportePagosAplicados = tmpData.ImportePagosAplicados;
                            item.ImportePagosPorAplicar = tmpData.ImportePagosPorAplicar;
                            item.Estatus = ResumenOficinaEstatus.Completado;
                        }
                        else
                        {
                            item.Estatus = ResumenOficinaEstatus.Error;
                        }
                    }

                    RecalcularFilaTotal();
                    _ = InvokeAsync(() => DataGrid.Reload());
                }
            }
            catch(Exception ex)
            {
                this.Logger.LogError(ex, "Fail at attemt to get data from {Oficina}", enlace.Nombre);
                lock(DataOffices)
                {
                    var item = DataOffices.Where(item => item.Enlace.Id == enlace.Id).FirstOrDefault();
                    if(item != null)
                    {
                        item.Estatus = ResumenOficinaEstatus.Error;
                    }
                    RecalcularFilaTotal();
                    _ = InvokeAsync(() => DataGrid.Reload());
                }
            }
        }

        private void RecalcularFilaTotal()
        {
            //*** Recalcular fila total
            _totalData.TotalPagos = DataOffices.Sum(item => item.TotalPagos);
            _totalData.TotalPagosAplicados = DataOffices.Sum(item => item.TotalPagosAplicados);
            _totalData.TotalPagosPorAplicar = DataOffices.Sum(item => item.TotalPagosPorAplicar);
            _totalData.ImportePagos = DataOffices.Sum(item => item.ImportePagos);
            _totalData.ImportePagosAplicados = DataOffices.Sum(item => item.ImportePagosAplicados);
            _totalData.ImportePagosPorAplicar = DataOffices.Sum(item => item.ImportePagosPorAplicar);
            InvokeAsync(() => StateHasChanged());
        }

        private async Task ExportarExcel_Click()
        {
            var _options = new ExcelExportProperties()
            {
                IncludeHiddenColumn = true
            };
            await ExportacionExcel.ExportarAsync(this.DataGrid, _options);
        }

        private async Task ResumenDiasClick(PagoLineaResumen data)
        {
            if (_resumenDiasVisible)
            {
                return;
            }

            this._busyDialog = true;
            await Task.Delay(200);
            var dateRange = new DateRange(_f1, _f2, _subsistema, _sector);
            var tmpData = await PagoLineaService1.ObtenerRumenPagosDia(data.Enlace, dateRange);
            if (tmpData == null)
            {
                Toaster.Add("Hubo un error al procesar la petición, inténtelo mas tarde.", MatToastType.Warning);
            }
            else
            {
                if(tmpData.Any())
                {
                    _resumenDiasVisible = true;
                    var titulo = $"{data.Enlace.Nombre.ToUpper()} - Resumen por dias";
                    _resumenDiasVtn.Show(tmpData, titulo);
                }
                else
                {
                    Toaster.Add("No hay datos disponibles para mostrar.", MatToastType.Info);
                }
            }
            await Task.Delay(200);
            this._busyDialog = false;
        }

        private async Task DetalleClick(PagoLineaResumen data)
        {
            if (_detalleVtnVisible)
            {
                return;
            }

            this._busyDialog = true;
            await Task.Delay(200);
            var dateRange = new DateRange(_f1,_f2, 0, 0);
            var tmpData = await PagoLineaService1.ObtenerDetallePagos2(data.Enlace, dateRange);
            if (tmpData == null)
            {
                Toaster.Add("Hubo un error al procesar la petición, inténtelo mas tarde.", MatToastType.Warning);
            }
            else
            {
                if(tmpData.Any())
                {
                    _detalleVtnVisible = true;
                    var titulo = $"{data.Enlace.Nombre.ToUpper()} - PAGOS DEL MES {_f1.ToString("MMMM yyyy").ToUpper()}";
                    _pagoLineaDetalleVtn.Show(data.Enlace, tmpData, titulo);
                }
                else
                {
                    Toaster.Add("No hay datos disponibles para mostrar.", MatToastType.Info);
                }
            }
            await Task.Delay(200);
            this._busyDialog = false;
        }
    }
}