using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sicem_Blazor.Data;
using Sicem_Blazor.Models;
using Sicem_Blazor.PagoLinea.Models;

namespace Sicem_Blazor.PagoLinea.Services;

public interface IPagoLineaRepo
{
    /// <summary>
    /// Almacena los registros en la base dedatos
    /// </summary>
    /// <param name="records"></param>
    /// <returns>Listado de folios almacenados</returns>
    IEnumerable<string> AlmacenarTransacciones(IEnumerable<TransactionRecord> records);
    Task<IEnumerable<Transaccion>> ObtenerTransacciones(DateRange dateRange);
}
