using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sicem_Blazor.PagoLinea.Services;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Sicem_Blazor.PagoLinea.Services;

public class PagoLineaProcessFileJob : IHostedService, IDisposable
{
    private readonly PagoLineaUploadFilesData data;
    private readonly ILogger<PagoLineaProcessFileJob> logger;
    private readonly IServiceProvider serviceProvider;
    private Timer _timer = null;
    private bool _isRunning = false;

    public PagoLineaProcessFileJob(PagoLineaUploadFilesData data, ILogger<PagoLineaProcessFileJob> logger, IServiceProvider serviceProvider)
    {
        this.data = data;
        this.logger = logger;
        this.serviceProvider = serviceProvider;
    }

    public void StartTimer()
    {
        if(!_isRunning)
        {
            _timer = new Timer(ProcessFile, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
            _isRunning = true;
        }
    }

    public void StopTimer()
    {
        this.logger.LogInformation("Deteniendo timer");
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        _timer?.Dispose();
        _isRunning = false;
        this.data.LoopCount = 0;
        this.logger.LogInformation("Timer detenido");
    }

    private void ProcessFile(object state)
    {
        if(this.data.LoopCount >= 10)
        {
            StopTimer();
        }

        this.logger.LogDebug("Procesando archivos {i}...", this.data.LoopCount);

        // * get the next file to process
        var filePayload = this.data.Files2Process.Where(item => item.Status == UploadFileJobStatusEnum.Pending).OrderBy(item => item.CreatedAt).FirstOrDefault();
        if(filePayload == null)
        {
            this.logger.LogDebug("No hay archivos pendientes por procesar");
            this.data.LoopCount++;
            return;
        }

        filePayload.Status = UploadFileJobStatusEnum.InProgress;
        this.logger.LogInformation("PagoLinea|Procesando archivo '{file}'", filePayload.FileName);

        // * Process the file
        try
        {
            var task = Task.Run(async () => await ProcessTransactions(filePayload));
            task.Wait();
            filePayload.Records = task.Result;
            filePayload.Message = "Procesado correctamente";
            filePayload.Status = UploadFileJobStatusEnum.Completed;
        }
        catch(Exception ex)
        {
            filePayload.Message = $"Error al procesar: {ex.Message}";
            filePayload.Status = UploadFileJobStatusEnum.Fail;
            this.logger.LogError(ex, "Error procesando archivo '{file}'", filePayload.FileName);
        }
        finally
        {
            filePayload.UploadedAt = DateTime.UtcNow;
        }

        // Update the file satus
        var index = this.data.Files2Process.FindIndex(f => f.Id == filePayload.Id);
        if(index >= 0)
        {
            this.data.Files2Process[index] = filePayload;
        }

        this.logger.LogInformation("PagoLinea|Finalizado archivo '{file}' start={start} end={end}", filePayload.FileName, filePayload.CreatedAt, filePayload.UploadedAt);
    }

    #region Pago Linea Process
    private async Task<long> ProcessTransactions(UploadFilePayloadJob uploadFilePayloadJob)
    {
        // * process the CSV file
        var transactionRecords = await ProcessTheFileCsvAsync(uploadFilePayloadJob.FileName);
        if(!transactionRecords.Any())
        {
            throw new Exception("No hay elementos disponibles en el archivo");
        }

        // * get the scoped pagoLineaRepoService
        using var scope = serviceProvider.CreateAsyncScope();
        var pagoLineaRepo = scope.ServiceProvider.GetRequiredService<IPagoLineaRepo>();

        // * store the records on the db in chunks of 300
        var storedRecordsId = pagoLineaRepo.AlmacenarTransacciones(transactionRecords);

        // * clean up the temporary filea
        ClearTemporallyFile(uploadFilePayloadJob.FileName);

        return storedRecordsId.Count();
    }

    private async Task<IEnumerable<TransactionRecord>> ProcessTheFileCsvAsync(string filePath)
    {
        logger.LogDebug("Attempt ot process the file {tempPath}", filePath);
        
        var records = new List<TransactionRecord>();
        var fileName = filePath.Split(Path.DirectorySeparatorChar).Last();

        try
        {
            IEnumerable<string[]> csvData = Array.Empty<string[]>();

            // * process the file and return the lines
            if(fileName.EndsWith(".csv"))
            {
                this.logger.LogDebug("Process file {file} as csv", filePath);
                csvData = Helpers.ProcessCSV.LoadFile(filePath);
            }
            else
            {
                this.logger.LogDebug("Process file {file} as xls", filePath);
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var rows = new List<string[]>();
                using(var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream);
                    do
                    {
                        while (reader.Read())
                        {
                            var row = new string[reader.FieldCount];
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row[i] = reader.GetValue(i)?.ToString() ?? string.Empty;
                            }
                            rows.Add(row);
                        }
                    } while (reader.NextResult());

                    csvData = rows;
                }
            }

            foreach(var data in csvData.Skip(1)) // * skip the first row because has the headers
            {
                if(string.IsNullOrWhiteSpace(data[0])) continue;
                var newRecord = TransactionRecordAdapter.Adapt(fileName, data);
                records.Add(newRecord);
            }
        }
        catch(System.Exception ex)
        {
            this.logger.LogError(ex, "Fail at process the csv: {message}", ex.Message);
            throw;
        }
        
        logger.LogDebug("Total records to process: {total} ", records.Count);
        await Task.CompletedTask;
        return records;
    }

    private void ClearTemporallyFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
            logger.LogDebug("Archivo '{path}' eliminado", filePath);
        }
        catch (System.Exception err)
        {
            logger.LogInformation("Cant delete the file {file}: {message}", filePath, err.Message);
        }
    }
    #endregion

    #region IHostedService implementation
    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartTimer();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StopTimer();
        _timer = null;
        return Task.CompletedTask;
    }

    #endregion

    #region IDisposable implementation
    public void Dispose()
    {
        StopTimer();
        _timer = null;
    }
    #endregion
}
