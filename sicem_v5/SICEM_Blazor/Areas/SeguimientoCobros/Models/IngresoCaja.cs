using System;
namespace Sicem_Blazor.SeguimientoCobros.Models {

    public class IngresoCaja{

        public string Id_Cajero {get; set;}
        public string Caja {get; set;}
        public int Recibos {get; set;} = 0;
        public decimal Cobrado {get; set;} = 0m;
        public decimal Hora0708 {get; set;} = 0m;
        public decimal Hora0809 {get; set;} = 0m;
        public decimal Hora0910 {get; set;} = 0m;
        public decimal Hora1011 {get; set;} = 0m;
        public decimal Hora1112 {get; set;} = 0m;
        public decimal Hora1213 {get; set;} = 0m;
        public decimal Hora1314 {get; set;} = 0m;
        public decimal Hora1415 {get; set;} = 0m;
        public decimal Hora1516 {get; set;} = 0m;
        public decimal Hora1617 {get; set;} = 0m;
        public decimal Hora1718 {get; set;} = 0m;
        public decimal Hora1819 {get; set;} = 0m;
        public decimal Hora1920 {get; set;} = 0m;

        public IngresoCaja(string id, string caja){
            Id_Cajero = id;
            Caja = caja;
        }

    }
}