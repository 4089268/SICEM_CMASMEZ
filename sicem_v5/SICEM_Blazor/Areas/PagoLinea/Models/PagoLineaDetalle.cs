using System;
using System.Data;
using System.Xml.Linq;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;
using Sicem_Blazor.Models;

namespace Sicem_Blazor.PagoLinea.Models;

public class    PagoLineaDetalle
{
    public IEnlace Enlace {get;set;}
    public string Folio {get; set;}
    public DateTime Fecha {get;set;}
    public string MedioPago {get;set;}
    public string Referencia {get;set;}
    public string ReferenciaTarjeta {get;set;}
    public string TipoTarjeta {get;set;}
    public string Autorizacion {get;set;}
    public string Estado {get;set;}
    public string Plataforma {get;set;}
    public decimal Importe {get;set;}
    public decimal ImporteAplicado {get;set;}

    
    public bool Aplicado
    {
        get => aplicarPagoResult?.Estatus == ResumenOficinaEstatus.Completado;
    }

    public AplicarPagoResult aplicarPagoResult { get; set; }

    public PagoLineaDetalle(IEnlace enlace)
    {
        this.Enlace = enlace;
    }

    public static PagoLineaDetalle FromEntity(IEnlace enlace, Transaccion transaccion)
    {
        var newItem = new PagoLineaDetalle(enlace)
        {
            Folio = transaccion.Folio,
            Fecha = transaccion.Fecha,
            Importe = transaccion.Monto,
            MedioPago = transaccion.Medio,
            Referencia = transaccion.Referencia,
            ReferenciaTarjeta = transaccion.Tarjeta,
            TipoTarjeta = transaccion.Medio,
            Autorizacion = transaccion.Autorizacion,
            Estado = transaccion.EstatusPago,
            Plataforma = transaccion.Servicio,
        };
        return newItem;
    }

    public string ToXml(string cveCaja, string idOperador)
    {
        string formattedFecha = Fecha.ToString("yyyy-MM-ddTHH:mm:sszzz");
        XElement xml = new XElement("FILTROS",
            new XElement("PAGOWEB",
                new XElement("id_caja", string.IsNullOrEmpty(cveCaja) ? "PAGOL" : cveCaja),
                new XElement("id_operador", string.IsNullOrEmpty(idOperador) ? "PAGOL" : idOperador),
                new XElement("id_transaccion", Folio.Trim()),
                new XElement("fecha", formattedFecha),
                new XElement("referencia_comercio", Referencia?.Trim()),
                new XElement("medio_de_pago", MedioPago?.Trim()),
                new XElement("referencia_tarjeta", ReferenciaTarjeta?.Trim()),
                new XElement("tipo_tarjeta", TipoTarjeta?.Trim()),
                new XElement("autorizacion", Autorizacion?.Trim()),
                new XElement("total_cobrado", Importe.ToString("F2")),
                new XElement("estado", Estado?.Trim()),
                new XElement("email", String.Empty),
                new XElement("telefono", String.Empty),
                new XElement("plataforma", Plataforma?.Trim())
            )
        );
        return xml.ToString();
    }

}
