using System;
using System.Collections.Generic;

namespace Sicem_Blazor.Models
{
    public partial class Transaccion
    {
        public Guid Id { get; set; }
        public int IdPago { get; set; }
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
        public string EstatusPago { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int IdPadron { get; set; }
        public string Aplicado { get; set; }
        public DateTime? FechaAplicacion { get; set; }
        public string MensajeAplicacion { get; set; }
    }
}
