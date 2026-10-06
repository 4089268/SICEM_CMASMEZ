using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.Models;
using Sicem_Blazor.PagoLinea.Data;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.Services;

namespace Sicem_Blazor.PagoLinea.Services;

public class PagoLineaService : IPagoLineaService, IPagoLineaRepo
{
    private readonly ILogger<PagoLineaService> logger;
    private readonly SicemService sicemService;
    private readonly SicemContext sicemContext;
    private IEnumerable<Ruta> enlaces = Array.Empty<Ruta>();

    public PagoLineaService(ILogger<PagoLineaService> l, SicemService ss, SicemContext sc)
    {
        this.logger = l;
        this.sicemService = ss;
        this.sicemContext = sc;
    }

    public ResumeOfficeOld ObtenerResumen(IEnlace enlace, DateRange dateRange)
    {

        var response = new ResumeOfficeOld(enlace)
        {
            Estatus = ResumenOficinaEstatus.Pendiente
        };

        try
        {
            logger.LogInformation("Obteniendo ingresos por pago en linea oficina:{Oficina} del {FechaInicial} al {FechaFin} ", enlace.Nombre, dateRange.Desde.Date, dateRange.Hasta.Date);
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                string sqlCommandText = @"
                BEGIN
                    Declare @i1 as decimal(14,2)
                    Declare @i2 as decimal(14,2)
                    Declare @i3 as decimal(14,2)
                    Declare @i4 as decimal(14,2)
                    
                    Declare @u1 as int
                    Declare @u2 as int
                    Declare @u3 as int

                    Select  @i1=SUM(total),
                            @u1=COUNT(id_padron),
                            @i3=SUM(cobrado) 
                    From [Facturacion].[Opr_Abonos] With(NoLock)
                    Where CONVERT(char(8),fecha,112) between @cFecha1 and @cFecha2 
                        And id_tipomovto = 6 
                        And id_estatus!=31
                        And id_caja=(select id_caja from [ventanillas].[Cfg_ArchivosXLS] where descripcion='LAYOUT BBVA')

                    Select  @i2=SUM(total),
                            @u2=COUNT(id_padron),
                            @i4=SUM(cobrado) 
                    From [Ventanillas].[Opr_Ventas] With(NoLock) 
                    Where CONVERT(char(8),fecha,112) between @cFecha1 and @cFecha2 
                        And id_estatus!=44
                        And cve_caja = ( select id_caja from [ventanillas].[Cfg_ArchivosXLS] where descripcion='LAYOUT BBVA')
                    
                    Select 
                        isnull(@i1,0) as importe_propios,
                        isnull(@u1,0)  as usuarios_propios,
                        isnull(@i2,0)  as importe_otros,
                        isnull(@u2,0)  as usuarios_otros,
                        importe = isnull(@i1,0) + isnull(@i2,0),
                        cobrado = isnull(@i3,0) + isnull(@i4,0),
                        usuarios = isnull(@u1,0) + isnull(@u2,0)
                END";
                var command = new SqlCommand()
                {
                    Connection = connection,
                    CommandText = sqlCommandText
                };
                command.Parameters.AddWithValue("@cFecha1", dateRange.Desde_ISO);
                command.Parameters.AddWithValue("@cFecha2", dateRange.Hasta_ISO);
                using(var dataReader = command.ExecuteReader())
                {
                    if(dataReader.Read())
                    {
                        response.LoadFromReader(dataReader);
                    }
                }
                connection.Close();
            }
        }
        catch(Exception err)
        {
            logger.LogError(0, err, "Error al obtener resumen recaudacion enlace:{Enlace}", enlace.Nombre);
            response.Estatus = ResumenOficinaEstatus.Error;
        }
        return response;
    }

    public IEnumerable<OprVenta> ObtenerDetallePagos(IEnlace enlace, DateRange dateRange)
    {

        var response = new List<OprVenta>();

        try
        {
            logger.LogInformation("Obteniendo listado de pagos en linea oficina:{Oficina} del {FechaInicial} al {FechaFin} ", enlace.Nombre, dateRange.Desde, dateRange.Hasta);
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                string sqlCommandText = @"
                BEGIN
                    ;With cajasExterna as (
                        SELECT CONCAT('E', FORMAT(id_externa, '0000')) as id_caja
                        FROM [Ventanillas].[Cat_CajasExternas]
                        WHERE descripcion like '%PAGOS EN LINEA%'
                    )
                    SELECT
                        oa.id_abono as id,
                        [table] = 'Opr_Abonos',
                        oa.id_caja as cve_caja,
                        oa.id_tipomovto,
                        oa.fecha,
                        oa.id_padron,
                        oa.id_cuenta,
                        oa.subtotal,
                        oa.iva,
                        oa.total,
                        oa.cobrado,
                        oa.af,
                        oa.mf,
                        oa.observa_a as observa
                    From [Facturacion].[Opr_Abonos]  oa
                    Inner Join cajasExterna ce on oa.id_caja = ce.id_caja
                    WHERE 
                        oa.id_tipomovto = 6 and
                        CONVERT(varchar(8), fecha, 112) BETWEEN @dateFrom and @dateTo
                    UNION ALl
                    SELECT
                        ov.id_venta as id,
                        [table] = 'Opr_Ventas',
                        ov.cve_caja,
                        ov.id_tipomovto,
                        ov.fecha,
                        ov.id_padron,
                        ov.id_cuenta,
                        ov.subtotal,
                        ov.iva,
                        ov.total,
                        ov.cobrado,
                        ov.af,
                        ov.mf,
                        ov.observa_a as observa
                    FROM [Ventanillas].[Opr_Ventas] ov
                    Inner Join cajasExterna ce on ov.cve_caja = ce.id_caja
                    Where CONVERT(varchar(8), fecha, 112) BETWEEN @dateFrom and @dateTo
                    Order by fecha desc
                END";
                var command = new SqlCommand()
                {
                    Connection = connection,
                    CommandText = sqlCommandText
                };
                command.Parameters.AddWithValue("@dateFrom", dateRange.Desde_ISO);
                command.Parameters.AddWithValue("@dateTo", dateRange.Hasta_ISO);
                using(var dataReader = command.ExecuteReader())
                {
                    while(dataReader.Read())
                    {
                        response.Add(OprVenta.FromSqlDataReader(enlace, dataReader));
                    }
                }
                connection.Close();
            }
        }
        catch(Exception err)
        {
            logger.LogError(0, err, "Error al obtener detalle de pagos en linea enlace:{Enlace}", enlace.Nombre);
        }
        return response;
    }

    public async Task<IEnumerable<TransactionRecord>> CalulateStatusPayment(IEnumerable<TransactionRecord> records)
    {
        // * load the enlaces
        this.enlaces = this.sicemService.ObtenerEnlaces();

        // * group the records by Concepto (Office)
        var recordsGroupedByConcepto = records.GroupBy(item => item.Concepto);

        // * process each group asynchronously
        var processedRecords = await Task.WhenAll(
            recordsGroupedByConcepto.Select(async group =>
                await ProcessOffice(group.Key, group.ToArray())
            )
        );

        // * flatten the results and return
        return processedRecords.SelectMany(records => records);
    }

    public async Task<IEnumerable<TransactionRecord>> ProcessOffice(string concepto, IEnumerable<TransactionRecord> records)
    {
        // * TODO: Create adapter to retrive the enlace by the concept
        await Task.CompletedTask;
        throw new NotImplementedException();

        // try
        // {
        //     var enlace = this.enlaces.FirstOrDefault(item => concepto.ToLower().Contains(item.Nombre, StringComparison.CurrentCultureIgnoreCase));
        //     if(enlace == null)
        //     {
        //         throw new ArgumentNullException($"Enlace of concept {concepto} not found");
        //     }

        //     var f1 = records.OrderBy(item => item.Fecha).First().Fecha;
        //     var f2 = records.OrderByDescending(item => item.Fecha).First().Fecha.AddDays(1);
        //     var payments = this.ObtenerDetallePagos(enlace, new DateRange(f1, f2));
        //     var referencesPaided = payments.Select(item => item.ReferenciaComercio.Trim()).ToArray();

        //     // * compare the reference with the references stored in the office
        //     foreach(var r in records)
        //     {
        //         r.Status = referencesPaided.Contains(r.ReferenciaComercio.Trim())
        //             ? TransactionRecord.ProcessFileStatues.Paided
        //             : TransactionRecord.ProcessFileStatues.NoPaided;
        //     }

        //     return records;
        // }
        // catch(Exception err)
        // {
        //     this.logger.LogError(err, "Fail at retrive payments: {message}", err.Message);
        //     foreach(var r in records)
        //     {
        //         r.Status = TransactionRecord.ProcessFileStatues.Fail;
        //     }
        //     ;
        //     return records;
        // }
    }

    public async Task<PagoLineaResumen> ObtenerRumenPagos(IEnlace enlace, DateRange dateRange)
    {
        var response = new PagoLineaResumen(enlace)
        {
            Estatus = ResumenOficinaEstatus.Pendiente
        };

        try
        {
            logger.LogInformation("Obteniendo resumen pago en linea oficina:{Oficina} del {FechaInicial} al {FechaFin} ", enlace.Nombre, dateRange.Desde.Date, dateRange.Hasta.Date);
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                var sqlCommandText = "EXEC [Sicem].[Pago_en_Linea] 'RESUMEN', @oficeId, @f1, @f2";
                var command = new SqlCommand()
                {
                    Connection = connection,
                    CommandText = sqlCommandText
                };
                command.Parameters.AddWithValue("@oficeId", enlace.Id);
                command.Parameters.AddWithValue("@f1", dateRange.Desde_ISO);
                command.Parameters.AddWithValue("@f2", dateRange.Hasta_ISO);
                using(var dataReader = await command.ExecuteReaderAsync())
                {
                    if(dataReader.Read())
                    {
                        response.LoadFromReader(dataReader);
                    }
                }
                connection.Close();
            }
        }
        catch(Exception err)
        {
            logger.LogError(0, err, "Error al obtener resumen recaudacion enlace:{Enlace}", enlace.Nombre);
            response.Estatus = ResumenOficinaEstatus.Error;
        }
        return response;
    }

    public async Task<IEnumerable<PagoLineaResumenDia>> ObtenerRumenPagosDia(IEnlace enlace, DateRange dateRange)
    {
        var responseList = new List<PagoLineaResumenDia>();
        try
        {
            logger.LogInformation("Obteniendo resumen por dias de por pago en linea oficina:{Oficina} del {FechaInicial} al {FechaFin} ", enlace.Nombre, dateRange.Desde.Date, dateRange.Hasta.Date);
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                var sqlCommandText = "EXEC [Sicem].[Pago_en_Linea] 'RESUMEN_X_DIA',@oficeId, @f1, @f2";
                var command = new SqlCommand()
                {
                    Connection = connection,
                    CommandText = sqlCommandText
                };
                command.Parameters.AddWithValue("@oficeId", enlace.Id);
                command.Parameters.AddWithValue("@f1", dateRange.Desde_ISO);
                command.Parameters.AddWithValue("@f2", dateRange.Hasta_ISO);
                using(var dataReader = await command.ExecuteReaderAsync())
                {
                    while(dataReader.Read())
                    {
                        var item = new PagoLineaResumenDia(enlace);
                        item.LoadFromReader(dataReader);
                        responseList.Add(item);
                    }
                }
                connection.Close();
            }
        }
        catch(Exception err)
        {
            logger.LogError(0, err, "Error al obtener resumen por dias enlace:{Enlace}", enlace.Nombre);
            return null;
        }
        return responseList;
    }

    public async Task<IEnumerable<PagoLineaDetalle>> ObtenerDetallePagos2(IEnlace enlace, DateRange dateRange)
    {
        await Task.CompletedTask;
        throw new NotImplementedException();
        // var responseList = new List<PagoLineaDetalle>();
        // try
        // {
        //     logger.LogInformation("Obteniendo resumen por dias de por pago en linea oficina:{Oficina} del {FechaInicial} al {FechaFin} ", enlace.Nombre, dateRange.Desde.Date, dateRange.Hasta.Date);
        //     using(var connection = new SqlConnection(enlace.GetConnectionString()))
        //     {
        //         connection.Open();
        //         var sqlCommandText = "EXEC [Sicem].[Pago_en_Linea] 'DETALLE', @oficeId, @f1, @f2";
        //         var command = new SqlCommand()
        //         {
        //             Connection = connection,
        //             CommandText = sqlCommandText
        //         };
        //         command.Parameters.AddWithValue("@oficeId", enlace.Id);
        //         command.Parameters.AddWithValue("@f1", dateRange.Desde_ISO);
        //         command.Parameters.AddWithValue("@f2", dateRange.Hasta_ISO);
        //         using(var dataReader = await command.ExecuteReaderAsync())
        //         {
        //             while(dataReader.Read())
        //             {
        //                 responseList.Add(PagoLineaDetalle.FromDataReader(enlace, dataReader));
        //             }
        //         }
        //         connection.Close();
        //     }
        // }
        // catch(Exception err)
        // {
        //     logger.LogError(0, err, "Error al obtener resumen por dias enlace:{Enlace}", enlace.Nombre);
        //     return null;
        // }
        // return responseList;
    }

    public async Task AplicarPagosPendientes(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // * retrive pending payments
        var pendingPayments = this.sicemContext.PagoLineaTransacciones.OrderBy(t => t.Fecha).Where(t => t.Aplicado != "APLICADO").ToArray();

        // TODO: Si se agregan mas oficinas, obtener el id_oficina de donde se va a aplicar el pago
        IEnlace enlace = sicemService.ObtenerEnlaces().First();

        foreach(var transaccion in pendingPayments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // prepare the transaction to aply the payment
                PagoLineaDetalle pagoLineaDetalle = PagoLineaDetalle.FromEntity(enlace, transaccion);

                var resultado = await AplicarPago(enlace, pagoLineaDetalle);
                if(resultado.Error == 1 || resultado.Estatus == ResumenOficinaEstatus.Error)
                {
                    transaccion.FechaAplicacion = DateTime.Now;
                    transaccion.Aplicado = EstatusAplicacionPagoEnum.ERROR.ToString();
                    transaccion.MensajeAplicacion = resultado.Mensaje;
                }
                else
                {
                    transaccion.FechaAplicacion = DateTime.Now;
                    transaccion.Aplicado = EstatusAplicacionPagoEnum.APLICADO.ToString();
                    transaccion.MensajeAplicacion = $"Pago aplicado Folio:{resultado.FolioPago}, Mesaje:{resultado.Mensaje}";
                }
            }
            catch(Exception err)
            {
                this.logger.LogError("Error al aplicar el pago del folio:{folio}", transaccion.Folio);
                transaccion.Aplicado = EstatusAplicacionPagoEnum.ERROR.ToString();
                transaccion.MensajeAplicacion = err.Message;
            }
            finally
            {
                sicemContext.PagoLineaTransacciones.Update(transaccion);
                sicemContext.SaveChanges();
            }
        }
    }

    public async Task<AplicarPagoResult> AplicarPago(IEnlace enlace, PagoLineaDetalle detallePago)
    {
        var responseResult = new AplicarPagoResult()
        {
            Folio = detallePago.Folio,
            Fecha = detallePago.Fecha,
            Importe = detallePago.Importe,
            Estatus = ResumenOficinaEstatus.Pendiente
        };

        try
        {
            // // * obtener la cve de la caja
            // var cveCaja = await this.GetCveCaja(enlace);

            logger.LogInformation("Aplicando pago oficina:{Oficina} foli:{folio}", enlace.Nombre, detallePago.Folio);
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                var sqlCommandText = "EXEC [Ventanillas].[usp_AplicarPagoWEB] @cAlias=N'APLICAR_PAGO',@cIdReferencia=N'', @xmlTables=@cXML";
                var command = new SqlCommand()
                {
                    Connection = connection,
                    CommandText = sqlCommandText
                };
                var xmlString = detallePago.ToXml(cveCaja: "PAGOL", idOperador: "PAGOL");
                command.Parameters.AddWithValue("@cXML", xmlString);

                // Save xmlString to disk for debugging/auditing
                // var fileName = $"PagoLinea_{detallePago.Folio}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                // var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), fileName);
                // System.IO.File.WriteAllText(filePath, xmlString);
                // this.logger.LogDebug("File saved at: " + filePath);
                // Save xmlString to disk for debugging/auditing

                using(var reader = await command.ExecuteReaderAsync())
                {
                    if(!await reader.ReadAsync())
                    {
                        responseResult.Estatus = ResumenOficinaEstatus.Error;
                        responseResult.Fecha = DateTime.Now;
                        responseResult.Error = 1;
                        responseResult.Mensaje = "No hay respuesta de la base de datos.";
                    }
                    else
                    {
                        responseResult.Error = reader.GetInt32(reader.GetOrdinal("Error"));
                        responseResult.Mensaje = reader["Mensaje"].ToString();
                        responseResult.Fecha = DateTime.Now;

                        if(responseResult.Error == 0) // Con exito
                        {
                            // * Move to second result set
                            if(await reader.NextResultAsync() && await reader.ReadAsync())
                            {
                                responseResult.Estatus = ResumenOficinaEstatus.Completado;
                                responseResult.FolioPago = reader["id_folio"].ToString();
                            }
                            else
                            {
                                responseResult.Estatus = ResumenOficinaEstatus.Error;
                                responseResult.Error = 0;
                                responseResult.Mensaje = "Sin respuesta";
                            }
                        }
                        else
                        {
                            responseResult.Estatus = ResumenOficinaEstatus.Error;
                            responseResult.Operacion = reader["Operacion"].ToString();
                        }
                    }
                }
            }

            // * attempt to update the Transaction on the db
            try
            {
                var _trans = this.sicemContext.PagoLineaTransacciones.FirstOrDefault(item => item.Folio == detallePago.Folio);
                if(_trans != null)
                {
                    if(responseResult.Error == 1 || responseResult.Estatus == ResumenOficinaEstatus.Error)
                    {
                        _trans.FechaAplicacion = DateTime.Now;
                        _trans.Aplicado = EstatusAplicacionPagoEnum.ERROR.ToString();
                        _trans.MensajeAplicacion = responseResult.Mensaje;
                    }
                    else
                    {
                        _trans.FechaAplicacion = DateTime.Now;
                        _trans.Aplicado = EstatusAplicacionPagoEnum.APLICADO.ToString();
                        _trans.MensajeAplicacion = $"Pago aplicado Folio:{responseResult.FolioPago}, Mesaje:{responseResult.Mensaje}";
                    }
                    this.sicemContext.PagoLineaTransacciones.Update(_trans);
                }
            }
            catch(Exception err)
            {
                this.logger.LogError(err, "Error al actualizar la transacción despues de intentar aplicar el pago: {message}", err.Message);
            }
        }
        catch(Exception err)
        {
            logger.LogError(err, "Hubo un problema al aplicar el pago: {message}", err.Message);
            responseResult.Estatus = ResumenOficinaEstatus.Error;
            responseResult.Error = 1;
            responseResult.Mensaje = $"Hubo un problema al aplicar el pago: {err.Message}";
        }
        return responseResult;
    }

    public async Task<string> GetCveCaja(IEnlace enlace)
    {
        string cveCaja = string.Empty;
        try
        {
            using(var connection = new SqlConnection(enlace.GetConnectionString()))
            {
                connection.Open();
                var sqlCommandText = "Select id_caja From [Ventanillas].[Cfg_ArchivosXLS] Where Descripcion='TRANFERENCIA'";
                var command = new SqlCommand(sqlCommandText, connection);
                cveCaja = (await command.ExecuteScalarAsync()).ToString();
                connection.Close();
            }
        }
        catch(Exception err)
        {
            logger.LogError(0, err, "Error al obtener resumen por dias enlace:{Enlace}", enlace.Nombre);
        }
        return cveCaja;
    }


    private async Task<IDictionary<string, bool>> ComprobarPagosAplicados(IEnlace enalce, IEnumerable<Transaccion>transacciones)
    {
        var resultado = new Dictionary<string, bool>();

        // * Prepara tabla pauload
        var tablaPayload = new DataTable();
        tablaPayload.Columns.Add("id_transaccion", typeof(string));
        tablaPayload.Columns.Add("id_padron", typeof(decimal));
        tablaPayload.Columns.Add("fecha", typeof(DateTime));
        tablaPayload.Columns.Add("total_cobrado", typeof(decimal));
        foreach (var transaccion in transacciones)
        {
            tablaPayload.Rows.Add(transaccion.Folio, transaccion.IdPadron, transaccion.Fecha, transaccion.Monto);
        }

        // * Realizar consulta
        using (var sqlConnection = new SqlConnection(enalce.GetConnectionString()))
        {
            using (var sqlCommand = new SqlCommand("[SICEM].[USP_PagoLinea_ConsultaEstatusPago]", sqlConnection))
            {
                sqlCommand.CommandType = CommandType.StoredProcedure;

                // El parametro se manda como Structured, apuntando al UDT
                // [SICEM].[UDT_PagosConsulta] creado en la base de datos.
                var parametroTvp = sqlCommand.Parameters.AddWithValue("@tPagos", tablaPayload);
                parametroTvp.SqlDbType = SqlDbType.Structured;
                parametroTvp.TypeName = "[SICEM].[UDT_PagosConsulta]";

                sqlConnection.Open();
                using (var reader = sqlCommand.ExecuteReader())
                {
                    var ordIdTransaccion = reader.GetOrdinal("id_transaccion");
                    var ordEstatus = reader.GetOrdinal("estatus");

                    while (reader.Read())
                    {
                        resultado.Add(reader.GetString(ordIdTransaccion), reader.GetString(ordEstatus) == "PAGADO");
                    }
                }

                sqlConnection.Close();
            }
        }

        return resultado;
    }

    #region Implementation IPagoLineaRepo
    public IEnumerable<string> AlmacenarTransacciones(IEnumerable<TransactionRecord> newRecords)
    {
        this.logger.LogDebug("Almacenando transacciones en la base de datos total: {total}", newRecords.Count());

        // * retrive the existing records
        List<string> foliosTransacciones = newRecords.Select(r => r.Folio).ToList();
        var existingRecords = this.sicemContext.PagoLineaTransacciones
            .Where(t => foliosTransacciones.Contains(t.Folio))
            .ToDictionary(t => t.Folio, t => t);

        var toInsert = new List<Transaccion>();
        var toUpdate = new List<Transaccion>();

        // * check and clasify if the record exist or needs to update
        foreach(var _record in newRecords)
        {
            if(existingRecords.TryGetValue(_record.Folio, out var _transaccion))
            {
                if(_transaccion.EstatusPago != EstatusPagoEnum.EXITOSO.ToString())
                {
                    var _tmpRecord = _record.ToEntity();
                    _transaccion.Fecha = _tmpRecord.Fecha;
                    _transaccion.EstatusPago = _tmpRecord.EstatusPago;
                    _transaccion.Monto = _tmpRecord.Monto;
                    _transaccion.Medio = _tmpRecord.Medio;
                    _transaccion.Concepto = _tmpRecord.Concepto;
                    _transaccion.Financiamiento = _tmpRecord.Financiamiento;
                    _transaccion.Autorizacion = _tmpRecord.Autorizacion;
                    _transaccion.Identificador = _tmpRecord.Identificador;
                    _transaccion.Tarjeta = _tmpRecord.Tarjeta;
                    _transaccion.Tarjetahabiente = _tmpRecord.Tarjetahabiente;
                    _transaccion.Usuario = _tmpRecord.Usuario;
                    _transaccion.ModifiedDate = DateTime.Now;
                    toUpdate.Add(_transaccion);
                }
            }
            else
            {
                toInsert.Add(_record.ToEntity());
            }
        }
        this.logger.LogDebug(" Records to insert:{newRecordsCount}, Record to update:{updateRecordsCount}", toInsert.Count, toUpdate.Count);


        // * beggin insert/update the records
        const int chunkSize = 100;
        foliosTransacciones = new List<string>(); // * clear the list
        using var dbTransaction = sicemContext.Database.BeginTransaction();
        try
        {
            // insert new records
            for(int i = 0; i < toInsert.Count; i += chunkSize)
            {
                this.logger.LogDebug("Inserting chunk {start} to {end} of {total} transactions", i + 1, Math.Min(i + chunkSize, toInsert.Count), toInsert.Count);
                var transaccionesChunk = toInsert.Skip(i).Take(chunkSize).ToList();
                sicemContext.PagoLineaTransacciones.AddRange(transaccionesChunk);
                sicemContext.SaveChanges();
                foliosTransacciones.AddRange(transaccionesChunk.Select(t => t.Folio));
            }

            // updated existing records
            for(int i = 0; i < toUpdate.Count; i += chunkSize)
            {
                this.logger.LogDebug("Updating chunk {start} to {end} of {total} transactions", i + 1, Math.Min(i + chunkSize, toUpdate.Count), toUpdate.Count);
                var transaccionesChunk = toUpdate.Skip(i).Take(chunkSize).ToList();
                sicemContext.PagoLineaTransacciones.UpdateRange(transaccionesChunk);
                sicemContext.SaveChanges();
                foliosTransacciones.AddRange(transaccionesChunk.Select(t => t.Folio));
            }

            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
        return foliosTransacciones;
    }

    public async Task<IEnumerable<Transaccion>> ObtenerTransacciones(DateRange dateRange)
    {
        await Task.CompletedTask;

        // * Recuperar las transacciones del periodo
        var transacciones = sicemContext.PagoLineaTransacciones
            .Where(t => t.Fecha.Date >= dateRange.Desde && t.Fecha.Date <= dateRange.Hasta)
            .ToList();

        // * Comprobar cuales de las transacciones se han aplicado y cuales no, para eso se puede usar el campo Aplicado y MensajeAplicacion
        var pagosAplicados = await ComprobarPagosAplicados(sicemService.ObtenerEnlaces().First(), transacciones);

        // * Actualizar el estado local con la respuesta del sistema de pagos
        foreach(var folio in pagosAplicados.Where(x => x.Value).Select(x => x.Key))
        {
            var transaccion = transacciones.FirstOrDefault(t => t.Folio == folio);
            transaccion.Aplicado = EstatusAplicacionPagoEnum.APLICADO.ToString();
        }

        return transacciones;
    }

    #endregion
}