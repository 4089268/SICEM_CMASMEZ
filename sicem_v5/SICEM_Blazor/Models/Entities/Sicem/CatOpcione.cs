using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Sicem_Blazor.Data;

namespace Sicem_Blazor.Models
{
    public partial class CatOpcione : IOpcionSistema
    {
        public CatOpcione()
        {
            OprOpciones = new HashSet<OprOpcione>();
        }

        public int IdOpcion { get; set; }
        public string Descripcion { get; set; }
        public bool? Inactivo { get; set; }
        public virtual ICollection<OprOpcione> OprOpciones { get; set; }

        [NotMapped]
        public int Id
        {
            get => IdOpcion;
            set => IdOpcion = value;
        }
    }
}
