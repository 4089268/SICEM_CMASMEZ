using System;
using System.Data;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.PagoLinea.Data;

namespace Sicem_Blazor.PagoLinea.Models;

public class TransactionRecord
{
    public Guid Id { get; set; }
    public long IdPago { get; set; }
    public DateTime Fecha { get; set; }
    public string Servicio { get; set; }
    public string Identificador { get; set; }
    public string Concepto { get; set; }
    public decimal Monto { get; set; }
    public string Referencia { get; set; }
    public string Folio { get; set; }
    public string Medio { get; set; }
    public string Tarjeta { get; set; }
    public string Financiamiento { get; set; }
    public string Autorizacion { get; set; }
    public string Tarjetahabiente { get; set; }
    public string Usuario { get; set; }
    public EstatusPagoEnum EstatusPago { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime UltimaModificacion { get; set; }

    public int IdPadron { get; set; }
    public EstatusAplicacionPagoEnum Aplicado { get; set; }
    public DateTime? FechaAplicacion { get; set; }

    public string MensajeAplicacion { get; set; }

}