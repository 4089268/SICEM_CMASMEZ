using System;
using System.Data;

namespace Sicem_Blazor.OrdenesTrabajos.Models
{
    public class Ordenes_Realizacion
    {
        public int Id {get;set;}
        public string Descripcion{get;set;}
        public int Ejecutadas {get;set;}
        public int No_Ejecutadas {get;set;}
        public int Eje_0_3 {get;set;}
        public int Eje_4_6 {get;set;}
        public int Eje_7_9 {get;set;}
        public int Eje_10 {get;set;}
        public int Total {get;set;}

        public static Ordenes_Realizacion FromDataReader(IDataReader reader)
        {
            return new Ordenes_Realizacion {
                Id = int.Parse(reader["id_realizo"].ToString()),
                Descripcion = reader["realizo"].ToString(),
                Ejecutadas = int.Parse(reader["eje"].ToString()),
                No_Ejecutadas = int.Parse(reader["no_eje"].ToString()),
                Eje_0_3 = int.Parse(reader["eje_0_3"].ToString()),
                Eje_4_6 = int.Parse(reader["eje_4_6"].ToString()),
                Eje_7_9 = int.Parse(reader["eje_7_9"].ToString()),
                Eje_10 = int.Parse(reader["eje_10"].ToString()),
                Total = int.Parse(reader["total"].ToString())
            };
        }
    }
}