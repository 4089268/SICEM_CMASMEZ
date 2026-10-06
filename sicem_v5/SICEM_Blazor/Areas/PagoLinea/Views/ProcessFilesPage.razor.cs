using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MatBlazor;
using Sicem_Blazor.Services;
using Sicem_Blazor.Helpers;
using Sicem_Blazor.Models.PagoLinea;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Services;
using System.Reflection;
using Sicem_Blazor.Models;
using Sicem_Blazor.Data;

using Radzen;
using Radzen.Blazor;
using DateRange = Sicem_Blazor.Data.DateRange;
namespace Sicem_Blazor.PagoLinea.Views
{
    public partial class ProcessFilesPage : ComponentBase, IDisposable
    {
        [Inject]
        public NavigationManager NavManager { get; set; } = default!;

        [Inject]
        public IMatToaster Toaster { get; set; } = default!;

        [Inject]
        public PagoLineaUploadFilesData PagoLineaUploadFilesData1 { get; set; } = default!;

        [Inject]
        public PagoLineaProcessFileJob ProcessFileJob { get; set; } = default!;

        [Inject]
        public IWebHostEnvironment Environment { get; set; } = default!;

        [Inject]
        public ILogger<PagoLineaPage> Logger { get; set; } = default!;

        [Inject]
        private IMatDialogService MatDialogService {get;set;}

        [Inject]
        public IPagoLineaService PagoLineaService { get; set; } = default!;

        [Inject]
        public IPagoLineaRepo PagoLineaRepo { get; set; } = default!;

        [Inject]
        public SicemService SicemService { get; set; } = default!;

        private RadzenDataGrid<UploadFilePayloadJob> DataGridFiles { get; set; }
        private RadzenDataGrid<TransactionRecord> DataGridRecords { get; set; }
        private List<UploadFilePayloadJob> RecordsFiles { get; set; } = new();
        private List<TransactionRecord> Records { get; set; } = new();
        private List<TransactionRecord> _recordsView => _soloPendientes
            ? Records.Where(item => item.Aplicado != EstatusAplicacionPagoEnum.APLICADO).ToList()
            : Records;

        private bool _busyDialog = false;
        private bool _soloPendientes = false;
        private int _tabIndex = 0;
        private MatTabBar _mattabbar;
        private Timer _timer = null; // Used to periodically retrive the file-records
        private DateRange _dateRange = new DateRange(DateTime.Now, DateTime.Now);
        private IEnlace _enlace;
        private bool _showButton = false;
        private bool _taskOnProgress = false;


        protected override void OnAfterRender(bool firstRender)
        {
            if(firstRender)
            {
                _timer = new Timer(FetchData, null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
                _enlace = this.SicemService.ObtenerEnlaces().Where(i => i.Inactivo != true).First();
            }
        }

        private void FetchData(object state)
        {
            this.RecordsFiles = this.PagoLineaUploadFilesData1.Files2Process.OrderBy(item => item.CreatedAt).ToList();
            _ = InvokeAsync(() => this.DataGridFiles.Reload());
            if(_tabIndex == 0)
            {
                InvokeAsync(StateHasChanged);
            }
        }

        private async Task HandleUploadFile(InputFileChangeEventArgs e)
        {
            Logger.LogDebug("Attemp to upload the File:{name}, ContentType:{contentType}", e.File?.Name, e.File?.ContentType);

            if(e.File == null || (!BrowserFileValidator.IsCsvFile(e.File) && !BrowserFileValidator.IsXLSFile(e.File)) )
            {
                Toaster.Add("El archivo debe ser un archivo CSV o XLS válido.", MatToastType.Warning);
                return;
            }

            // Validate file size (max 2MB)
            const long maxFileSize = 10 * 1024 * 1024; // 2MB in bytes
            if (e.File.Size > maxFileSize)
            {
                Toaster.Add("El archivo no debe exceder los 10MB.", MatToastType.Warning);
                return;
            }

            // * Validate if the file is not already loaded
            if(RecordsFiles.Select(item => item.FileName.ToLower()).Contains(e.File.Name.ToLower()))
            {
                Toaster.Add("Este archivo ya se encuentra cargado", MatToastType.Info);
                return;
            }

            this._busyDialog = true;
            await Task.Delay(100);

            // * save the file temporarily
            var filePath = await SaveFileTemporally(e.File);
            Logger.LogDebug("File uploaded on path '{filePath}'", filePath);

            // * set the file to process on background
            this.PagoLineaUploadFilesData1.Files2Process.Add(new UploadFilePayloadJob(filePath));
            this.ProcessFileJob.StartTimer(); // start job
            
            this._busyDialog = false;
            Toaster.Add("Archivo cargado y en espera de ser procesado..", MatToastType.Success);

            // * force the get the files
            FetchData(null);
        }

        private async Task MatTabBarActiveChanged(BaseMatTabLabel label)
        {
            _tabIndex = label.Id switch
            {
                "archivos" => 0,
                "cargados" => 1,
                _ => 0
            };
            await Task.CompletedTask;
        }

        public async Task Procesar(SeleccionarFechaEventArgs e)
        {
            _dateRange = new DateRange(e.Fecha1, e.Fecha2);
            this._busyDialog = true;
            await Task.Delay(100);

            try
            {
                var rawRecords = await this.PagoLineaRepo.ObtenerTransacciones(_dateRange);
                if(rawRecords.Any())
                {
                    this.Records = rawRecords.Select(t => t.ToDTO()).ToList();
                }
                else
                {
                    this.Records = new List<TransactionRecord>();
                }
                await InvokeAsync(() => this.DataGridRecords.Reload());
            }
            catch(System.Exception err)
            {
                this.Logger.LogError(err, "Error al obtener las transacciones del peridodo: {message}", err.Message);
                this.Toaster.Add("Hubo un error al obtener las transacciones.", MatToastType.Danger);
            }
            finally
            {
                _showButton = this.Records.Where( item => item.Aplicado != EstatusAplicacionPagoEnum.APLICADO).Any();
                this._busyDialog = false;
                await InvokeAsync(StateHasChanged);
            }
        }

        private async Task AplicarPagoClick(TransactionRecord transaccionContext)
        {
            var transaccion = this.Records.FirstOrDefault( item => item.Folio == transaccionContext.Folio);
            if(transaccion == null)
            {
                return;
            }

            // * update status on grid
            transaccion.Aplicado = EstatusAplicacionPagoEnum.PROGRESS;
            await Task.Delay(100);
            await InvokeAsync(() => StateHasChanged());
            await InvokeAsync(() => this.DataGridRecords.Reload());

            // * prepare the payload to aply the payment
            var _paymentPayload = PagoLineaDetalle.FromEntity(_enlace, transaccionContext.ToEntity());

            // * aply the payment and process the response
            try
            {
                var response = await this.PagoLineaService.AplicarPago(this._enlace, _paymentPayload);
                if(response.Error == 1 || response.Estatus == Sicem_Blazor.Data.Contracts.ResumenOficinaEstatus.Error)
                {
                    transaccion.FechaAplicacion = DateTime.Now;
                    transaccion.Aplicado = EstatusAplicacionPagoEnum.ERROR;
                    transaccion.MensajeAplicacion = response.Mensaje;
                    if(response.Mensaje.Contains("->"))
                    {
                        var message = $"rror al aplicar el pago: {response.Mensaje.Split("->").First().Trim()}".ToLower();
                        Toaster.Add("E" + message, MatToastType.Danger, $"Pago {transaccion.Id} {transaccion.Fecha.ToShortDateString()}");
                    }
                    else
                    {
                        var message = $"error al aplicar el pago: {response.Mensaje}".ToLower();
                        Toaster.Add("E" + message, MatToastType.Danger, $"Pago {transaccion.Id} {transaccion.Fecha.ToShortDateString()}");
                    }
                }
                else
                {
                    transaccion.FechaAplicacion = DateTime.Now;
                    transaccion.Aplicado = EstatusAplicacionPagoEnum.APLICADO;
                    transaccion.MensajeAplicacion = $"Pago aplicado Folio:{response.FolioPago}, Mesaje:{response.Mensaje}";
                    Toaster.Add("Pago aplicado con folio: " + response.FolioPago, MatToastType.Success, $"Pago {transaccion.Id} {transaccion.Fecha.ToShortDateString()}");
                }
            }
            catch(Exception err)
            {
                Toaster.Add("Error al tratar de aplicar el pago", MatToastType.Danger);
                transaccion.Aplicado = EstatusAplicacionPagoEnum.ERROR;
                transaccion.MensajeAplicacion = err.Message;
                Logger.LogError(err, "Error al aplicar el pago: {message}", err.Message);
            }

            // * updated the status on the grid
            await Task.Delay(100);
            await InvokeAsync(() => StateHasChanged());
            await InvokeAsync(() => this.DataGridRecords.Reload());
        }

        private async Task AplicarPagosClick()
        {
            if(_taskOnProgress) return;

            // * confirmation modal
            var result = await MatDialogService.AskAsync("Está por aplicar los pagos pendientes, ¿Desea continuar?", new string[] {"Aplicar Pagos", "Cancelar"});
            if(result != "Aplicar Pagos")
            {
                return;
            };


            // * Preparar filas
            _taskOnProgress = true;
            var detallesPorAplicar = this.Records.Where(item => item.Aplicado != EstatusAplicacionPagoEnum.APLICADO).ToList();
            foreach(var detalle in detallesPorAplicar)
            {
                var det = Records.FirstOrDefault(item => item.Folio == detalle.Folio);
                if(det != null)
                {
                    det.Aplicado = EstatusAplicacionPagoEnum.PROGRESS;
                }
            }
            await InvokeAsync( () => StateHasChanged());

            foreach(var detalle in detallesPorAplicar)
            {
                await Task.Run( async() => await AplicarPagoClick(detalle));
            }


            _taskOnProgress = false;
            await InvokeAsync( () => StateHasChanged());


            await MatDialogService.AlertAsync("Finalizado la aplicación de pagos.");
        }
        // TODO-EXPORT-RADZEN: exportación pendiente (ver docs/MIGRACION.md). Código original de Syncfusion:
        /*
        private async Task DownloadExcel()
        {
            await DataGridRecords.ExportToExcelAsync();
        }
        */

        private async Task OnSoloPendientesChanged(bool value)
        {
            _soloPendientes = value;
            await InvokeAsync(StateHasChanged);
            await InvokeAsync(() => this.DataGridRecords.Reload());
        }

        #region temporally files functions
        private async Task<string> SaveFileTemporally(IBrowserFile file)
        {
            //* get a temporary path to store the file
            var filePath = Path.Combine(Path.GetTempPath(), "SicemBlazorFortin", "PagoLineaUploads", file.Name);
            Logger.LogDebug("Attempt to save the file on the temporally path: {path}", filePath);

            // * save the file temporarily
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using(var fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(fileStream);
            }
            Logger.LogDebug("File save at {tempPath}", filePath);
            return filePath;
        }

        private void ClearTemporallyFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
                Logger.LogDebug("Archivo eliminado");
            }
            catch(System.Exception err)
            {
                Logger.LogInformation("Cant delete the file {file}: {message}", filePath, err.Message);
            }
        }
        #endregion

        #region IDispose implementation
        public void Dispose()
        {
            this.Logger.LogDebug("Terminating UploadfilePage Tiemer");
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            _timer?.Dispose();
            _timer = null;
        }
        #endregion
    }
}