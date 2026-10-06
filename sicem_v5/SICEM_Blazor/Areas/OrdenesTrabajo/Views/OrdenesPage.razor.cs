using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;
using MatBlazor;
using Sicem_Blazor.OrdenesTrabajos.Models;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.Models;
using Sicem_Blazor.Services;
using Sicem_Blazor.Areas.OrdenesTrabajo.Views;

namespace Sicem_Blazor.OrdenesTrabajos.Views;

public partial class OrdenesPage : ComponentBase
{
    [Inject]
    public ExportacionExcelService ExportacionExcel { get; set; }

    [Inject]
    private NavigationManager navigationManager { get; set; }

    [Inject]
    private IMatToaster Toaster { get; set; }

    [Inject]
    private OrdenesService ordenesService { get; set; }

    [Inject]
    private SicemService sicemService { get; set; }

    private RadzenDataGrid<OrdenesResumenOficina> DataGrid { get; set; }
    private RadzenChart GraficaUsuarios { get; set; }

    private bool _busyDialog = false;
    private List<OrdenesResumenOficina> DatosGrid { get; set; }
    private List<ChartItem> DatosGrafica { get; set; }
    private DateTime _f1, _f2;
    private int _subsistema, _sector;

    Ordenes_DetalleVtn _vtn_detalle;
    Ordenes_ColoniasVtn _vtn_colonias;
    Ordenes_TrabajosVtn _vtn_trabajos;
    Ordenes_RealizacionVtn _vtn_realizacion;
    Ordenes_CapturacionVtn _vtn_capturacion;
    private bool _vtn_detalle_visible = false, vtn_colonias_visible = false, vtn_trabajos_visible = false, vtn_realizacion_visible = false, vtn_capturacion_visible = false;

    protected override void OnInitialized()
    {
        // Validar si se cuenta con la opcion
        if (!sicemService.Usuario.OpcionSistemas.Select(item => item.Id).Contains(OpcionesSistema.ORDENES_TRABAJO))
        {
            navigationManager.NavigateTo("/");
            return;
        }

        var _now = DateTime.Now;
        this._f1 = new DateTime(_now.Year, _now.Month, 1);
        this._f2 = new DateTime(_now.Year, _now.Month, DateTime.DaysInMonth(_now.Year, _now.Month));
        this._subsistema = 0;
        this._sector = 0;
    }

    public void Procesar(SeleccionarFechaEventArgs e)
    {
        this._f1 = e.Fecha1;
        this._f2 = e.Fecha2;
        this._subsistema = e.Subsistema;
        this._sector = e.Sector;

        this.DatosGrid = new List<OrdenesResumenOficina>();
        this.DatosGrafica = new List<ChartItem>();
        var Tareas = new List<Task>();
        IEnlace[] oficinas = sicemService.ObtenerOficinasDelUsuario().ToArray();

        foreach (var oficina in oficinas)
        {
            var _itemGrid = new OrdenesResumenOficina(oficina)
            {
                Estatus = ResumenOficinaEstatus.Pendiente
            };
            this.DatosGrid.Add(_itemGrid);

            var _itemGrafica = new ChartItem()
            {
                Id = oficina.Id,
                Descripcion = oficina.Nombre
            };
            this.DatosGrafica.Add(_itemGrafica);

            Tareas.Add(new Task(() => ProcesarConsulta(oficina)));
        }

        // * agregar fila total
        if (oficinas.Count() > 1)
        {
            var _itemTotal = new OrdenesResumenOficina(new Ruta
            {
                Id = 999,
                Oficina = " TOTAL"
            });
            this.DatosGrid.Add(_itemTotal);
        }

        Tareas.ForEach(item => item.Start());
    }

    private void ProcesarConsulta(IEnlace oficina)
    {
        // Un error en esta oficina no debe detener a las demás (CLAUDE.md)
        OrdenesResumenOficina tmpDatos;
        try
        {
            tmpDatos = ordenesService.ObtenerOrdenesPorOficina(oficina, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector);
        }
        catch (Exception)
        {
            tmpDatos = null;
        }
        tmpDatos ??= new OrdenesResumenOficina(oficina) { Estatus = ResumenOficinaEstatus.Error };

        var _random = new Random();
        var _sleepTime = _random.Next(100, 3000);
        System.Threading.Thread.Sleep(_sleepTime);

        lock (DatosGrid)
        {
            var _itemGrid = DatosGrid.Where(item => item.IdOficina == oficina.Id).FirstOrDefault();
            if (_itemGrid != null)
            {
                _itemGrid.Estatus = tmpDatos.Estatus;
                _itemGrid.Pendi = tmpDatos.Pendi;
                _itemGrid.Eneje = tmpDatos.Eneje;
                _itemGrid.Reali = tmpDatos.Reali;
                _itemGrid.Cance = tmpDatos.Cance;
                _itemGrid.Eje = tmpDatos.Eje;
                _itemGrid.No_eje = tmpDatos.No_eje;
                _itemGrid.Total = tmpDatos.Total;
            }
            RecalcularFilaTotal();
        }
        _ = InvokeAsync(() => DataGrid.Reload());

        lock (DatosGrafica)
        {
            var _itemGrafica = DatosGrafica.Where(item => item.Id == oficina.Id).FirstOrDefault();
            if (_itemGrafica != null)
            {
                _itemGrafica.Valor1 = (decimal)tmpDatos.Pendi;
                _itemGrafica.Valor2 = (decimal)tmpDatos.Eneje;
                _itemGrafica.Valor3 = (decimal)tmpDatos.Reali;
                _itemGrafica.Valor4 = (decimal)tmpDatos.Cance;
            }
            _ = InvokeAsync(() => GraficaUsuarios.Reload());
        }
    }

    private void Procesar_Datos_Grafica()
    {
        var tmpList1 = new List<ChartItem>();
        foreach (var item in this.DatosGrid)
        {
            if (item.IdOficina > 0 && item.IdOficina < 999)
            {
                var newItem = new ChartItem();
                newItem.Id = item.IdOficina;
                newItem.Descripcion = item.Oficina;
                newItem.Valor1 = item.Pendi;
                newItem.Valor2 = item.Eneje;
                newItem.Valor3 = item.Reali;
                newItem.Valor4 = item.Cance;
                tmpList1.Add(newItem);
            }
        }
        this.DatosGrafica = tmpList1;
    }

    private void RecalcularFilaTotal()
    {
        var _itemTotal = DatosGrid.Where(item => item.IdOficina == 999).FirstOrDefault();
        if (_itemTotal != null)
        {
            var _datos = DatosGrid.Where(item => item.IdOficina != 999).ToList();
            _itemTotal.Pendi = _datos.Sum(item => item.Pendi);
            _itemTotal.Eneje = _datos.Sum(item => item.Eneje);
            _itemTotal.Reali = _datos.Sum(item => item.Reali);
            _itemTotal.Cance = _datos.Sum(item => item.Cance);
            _itemTotal.Eje = _datos.Sum(item => item.Eje);
            _itemTotal.No_eje = _datos.Sum(item => item.No_eje);
            _itemTotal.Total = _datos.Sum(item => item.Total);
        }
    }

    // * Funciones Ventanas Secundarias
    private async Task Mostrar_Vtn_Detalle(OrdenesResumenOficina data, int opcion)
    {
        if (!_vtn_detalle_visible)
        {
            this._busyDialog = true;
            await Task.Delay(100);

            Ordenes_Detalle[] datos = new Ordenes_Detalle[] { };
            var titulo = $"{data.Oficina} / Ordenes Generadas";
            switch (opcion)
            {
                case 0:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "0");
                    titulo = $"Ordenes Generadas del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 1:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "7");
                    titulo = $"Ordenes Pendientes del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 2:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "8");
                    titulo = $"Ordenes En Ejecucion del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 3:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "9");
                    titulo = $"Ordenes Realizadas del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 4:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "10");
                    titulo = $"Ordenes Canceladas del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 5:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "9,1");
                    titulo = $"Ordenes Ejecutadas del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
                case 6:
                    datos = ordenesService.ObtenerOrdenesDetalle(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "9,0");
                    titulo = $"Ordenes No Ejecutadas del {_f1.ToString("dd/MM/yyyy")} al  {_f2.ToString("dd/MM/yyyy")}";
                    break;
            }

            if (datos == null)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }
            if (datos.Length <= 0)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }

            _vtn_detalle_visible = true;
            await _vtn_detalle.Inicializar(datos, titulo);

            await Task.Delay(100);
            this._busyDialog = false;
        }
    }

    private async Task Mostrar_Vtn_Colonias(OrdenesResumenOficina data)
    {
        if (!vtn_colonias_visible)
        {
            this._busyDialog = true;
            await Task.Delay(100);

            Ordenes_Agrupado[] datos = ordenesService.ObtenerOrdenesColonias(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "");

            if (datos == null)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }
            if (datos.Length <= 0)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }

            var tmpOfi = sicemService.ObtenerEnlaces(data.IdOficina).FirstOrDefault();
            if (tmpOfi != null)
            {
                vtn_colonias_visible = true;
                _vtn_colonias.Titulo = $"{data.Oficina} / Ordenes agrupadas por colonias";
                _vtn_colonias.OficinaActual = tmpOfi;
                await _vtn_colonias.Inicializar(datos);
            }

            await Task.Delay(100);
            this._busyDialog = false;
        }
    }

    private async Task Mostrar_Vtn_Trabajos(OrdenesResumenOficina data)
    {
        if (!vtn_trabajos_visible)
        {
            this._busyDialog = true;
            await Task.Delay(100);
            Ordenes_Agrupado[] datos = ordenesService.ObtenerOrdenesTrabajos(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector).ToArray();
            if (datos == null)
            {
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                await Task.Delay(100);
                _busyDialog = false;
                return;
            }
            if (datos.Length <= 0)
            {
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                await Task.Delay(100);
                _busyDialog = false;
                return;
            }
            var tmpOfi = sicemService.ObtenerEnlaces(data.IdOficina).FirstOrDefault();
            if (tmpOfi != null)
            {
                vtn_trabajos_visible = true;
                _vtn_trabajos.OficinaActual = tmpOfi;
                _vtn_trabajos.Titulo = $"{data.Oficina} / Ordenes por Trabajos";
                await _vtn_trabajos.Inicializar(datos);
            }

            await Task.Delay(100);
            this._busyDialog = false;
        }
    }

    private async Task Mostrar_Vtn_Realizacion(OrdenesResumenOficina data)
    {
        if (!vtn_realizacion_visible)
        {
            this._busyDialog = true;
            await Task.Delay(100);

            Ordenes_Realizacion[] datos = ordenesService.ObtenerOrdenesRealizacion(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "").ToArray();

            if (datos == null)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }
            if (datos.Length <= 0)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }

            var tmpOfi = sicemService.ObtenerEnlaces(data.IdOficina).FirstOrDefault();
            if (tmpOfi != null)
            {
                vtn_realizacion_visible = true;
                _vtn_realizacion.OficinaActual = tmpOfi;
                _vtn_realizacion.Titulo = $"{data.Oficina} / Ordenes Agrupadas por Realizacion";
                await _vtn_realizacion.Inicializar(datos);
            }

            await Task.Delay(100);
            this._busyDialog = false;
        }
    }

    private async Task Mostrar_Vtn_Capturacion(OrdenesResumenOficina data)
    {
        if (!vtn_capturacion_visible)
        {
            this._busyDialog = true;
            await Task.Delay(100);

            Ordenes_Capturacion[] datos = ordenesService.ObtenerOrdenesCapturacion(data.Enlace, _f1.ToString("yyyyMMdd"), _f2.ToString("yyyyMMdd"), _subsistema, _sector, "");

            if (datos == null)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }
            if (datos.Length <= 0)
            {
                _busyDialog = false;
                Toaster.Add("No hay datos disponibles", MatToastType.Info, "Sin Datos", null);
                return;
            }

            var tmpOfi = sicemService.ObtenerEnlaces(data.IdOficina).FirstOrDefault();
            if (tmpOfi != null)
            {
                vtn_capturacion_visible = true;
                _vtn_capturacion.OficinaActual = tmpOfi;
                _vtn_capturacion.Titulo = $"{data.Oficina} /  Ordenes Agrupadas Por Captura";
                await _vtn_capturacion.Inicializar(datos);
            }

            await Task.Delay(100);
            this._busyDialog = false;
        }
    }

    private async Task ExportarExcel_Click()
    {
        var p = new ExcelExportProperties
        {
            FileName = $"Sicem_{Guid.NewGuid().ToString().Replace("-", "")}.xlsx",
            IncludeHiddenColumn = true
        };
        await ExportacionExcel.ExportarAsync(DataGrid, p);
    }
}
