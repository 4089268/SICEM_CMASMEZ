using System;
using System.Collections.Generic;

namespace Sicem_Blazor.Models
{
    public partial class OprOpcione
    {
        public int Id { get; set; }
        public int IdUsuario { get; set; }
        public int IdOpcion { get; set; }

        public virtual CatOpcione IdOpcionNavigation { get; set; }
        public virtual Usuario IdUsuarioNavigation { get; set; }
    }
}
