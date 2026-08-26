using System;
using System.Collections.Generic;

namespace Sicem_Blazor.Models
{
    public partial class ModsOficina
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdOficina { get; set; }
        public string Tabla { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; }
    }
}
