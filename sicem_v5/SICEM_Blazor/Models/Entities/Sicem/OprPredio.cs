using System;
using System.Collections.Generic;

namespace Sicem_Blazor.Models
{
    public partial class OprPredio
    {
        public int CodigoPredio { get; set; }
        public int IdOficina { get; set; }
        public int IdCuenta { get; set; }
        public string NombreComercial { get; set; }
        public string NumeroMedidor { get; set; }
        public int? IdAnomaliaPredio { get; set; }
        public string AnomaliaPredio { get; set; }
        public int? IdSectorHidraulico { get; set; }
        public string SectorHidraulico { get; set; }
        public int? IdNivelSocieconomico { get; set; }
        public string NivelSocieconomico { get; set; }
        public int? IdTarifa { get; set; }
        public string Tarifa { get; set; }
        public int? IdDiametro { get; set; }
        public string Diametro { get; set; }
        public int? IdGiroToma { get; set; }
        public string GiroToma { get; set; }
        public int? IdMarcaMedidor { get; set; }
        public string MarcaMedidor { get; set; }
        public int? IdAnomaliaToma { get; set; }
        public string AnomaliaToma { get; set; }
        public int? IdAnomaliaMedidor { get; set; }
        public string AnomalidaMedidor { get; set; }
        public int? IdTipoToma { get; set; }
        public string TipoToma { get; set; }
        public int? IdTipoInstalacion { get; set; }
        public string TipoInstalacion { get; set; }
        public string Calle { get; set; }
        public string NumeroExterior { get; set; }
        public string CalleLateral1 { get; set; }
        public string CalleLateral2 { get; set; }
        public bool? EsClandestino { get; set; }
        public bool? TienePozo { get; set; }
        public string Latitud { get; set; }
        public string Longitud { get; set; }
        public string Observaciones { get; set; }
        public DateTime? FechaCaptura { get; set; }
    }
}
