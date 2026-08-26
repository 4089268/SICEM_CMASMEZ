using System;

namespace Sicem_Blazor.Lecturas.Models{
    public class DetalleLecturista {
        public int Id_Localidad {get;set;}
        public string Localidad {get;set;}
        public long Cuenta {get;set;}
        public string Localizacion {get;set;}
        public string Usuario {get;set;}
        public string Direccion {get;set;}
        public string Tarifa {get;set;}
        public DateTime Fecha {get;set;}
        public int Lectura_Anterior {get;set;}
        public int Lectura_Actual {get;set;}
        public int Consumo {get; set;}
        public string Anomalia {get;set;}
        public string Incidencia {get; set;}
    }
}