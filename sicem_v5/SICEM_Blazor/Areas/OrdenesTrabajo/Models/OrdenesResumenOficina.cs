using System;
using System.Data;
using Sicem_Blazor.Data;
using Sicem_Blazor.Data.Contracts;

namespace Sicem_Blazor.OrdenesTrabajos.Models
{
    public class OrdenesResumenOficina
    {
        public IEnlace Enlace {get;set;}
        public ResumenOficinaEstatus Estatus { get; set; }
        public int IdOficina { get => Enlace.Id;}
        public string Oficina { get => Enlace.Nombre;}
        public int Pendi { get; set; }
        public int Eneje { get; set; }
        public int Reali { get; set; }
        public int Cance { get; set; }
        public int Eje { get; set; }
        public int No_eje { get; set; }
        public int Total { get; set; }

        public OrdenesResumenOficina(IEnlace enlace)
        {
            this.Enlace = enlace;
            Estatus = ResumenOficinaEstatus.Pendiente;
            Pendi = 0;
            Eneje = 0;
            Reali = 0;
            Cance = 0;
            Eje = 0;
            No_eje = 0;
            Total = 0;
        }

        public static OrdenesResumenOficina FromDataReader(IEnlace enlace, IDataReader reader)
        {
            return new OrdenesResumenOficina(enlace)
            {
                Pendi = ConvertUtils.ParseInteger(reader["pendi"]),
                Eneje = ConvertUtils.ParseInteger(reader["eneje"]),
                Reali = ConvertUtils.ParseInteger(reader["reali"]),
                Cance = ConvertUtils.ParseInteger(reader["cance"]),
                Eje = ConvertUtils.ParseInteger(reader["eje"]),
                No_eje = ConvertUtils.ParseInteger(reader["no_eje"]),
                Total = ConvertUtils.ParseInteger(reader["total"]),
                Estatus = ResumenOficinaEstatus.Completado
            };
        }

    }
}
