using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Sicem_Blazor.Data;
using Sicem_Blazor.Lecturas.Models;
using Microsoft.Extensions.Logging;

namespace Sicem_Blazor.Lecturas.Data
{
    public class StoredProcedures {
        public static readonly string DESEMPENIO_LECTURISTA = "[SICEM_Casf].[usp_desempeño_lecturista]";
        public static readonly string RESUMEN_INCIDENCIAS = "[SICEM_Casf].[usp_incidencias_lecturas]";
        public static readonly string DETALLE_LECTURAS = "[SICEM_Casf].[usp_detalle_lecturista]";
    }
}
