using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sicem_Blazor.Data;
using Sicem_Blazor.PagoLinea.Models;


namespace Sicem_Blazor.PagoLinea.Services;

public interface IPagoLineaService
{
    
    ResumeOfficeOld ObtenerResumen(IEnlace enlace, DateRange dateRange);

    IEnumerable<OprVenta> ObtenerDetallePagos(IEnlace enlace, DateRange dateRange);

    Task<IEnumerable<TransactionRecord>> CalulateStatusPayment(IEnumerable<TransactionRecord> records);

    Task<IEnumerable<TransactionRecord>> ProcessOffice(string concepto, IEnumerable<TransactionRecord> records);

    Task<PagoLineaResumen> ObtenerRumenPagos(IEnlace enlace, DateRange dateRange);

    Task<IEnumerable<PagoLineaResumenDia>> ObtenerRumenPagosDia(IEnlace enlace, DateRange dateRange);

    Task<IEnumerable<PagoLineaDetalle>> ObtenerDetallePagos2(IEnlace enlace, DateRange dateRange);

    Task AplicarPagosPendientes(CancellationToken cancellationToken);

    Task<AplicarPagoResult> AplicarPago(IEnlace enlace, PagoLineaDetalle detallePago);

    Task<string> GetCveCaja(IEnlace enlace);

}