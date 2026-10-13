using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Sicem_Blazor.Data;
using Sicem_Blazor.SeguimientoCobros.Data;
using Sicem_Blazor.SeguimientoCobros.Models;
using Sicem_Blazor.Services;

namespace Sicem_Blazor.SeguimientoCobros.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class IncomeOfficeController : Controller {

        private readonly IncomeOfficeService incomeOfficeService;

        public IncomeOfficeController( IncomeOfficeService s) {
            this.incomeOfficeService = s;
        }

        [HttpGet]
        [Route("caja/{idCaja}")]
        public IActionResult IngresosCaja( [FromRoute] string idCaja, [FromQuery] string d){
            if (DateTime.TryParseExact(d, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime date))
            {
                var ingresosCajas = incomeOfficeService.GetCobrosHorario(date, ConvertUtils.ParseInteger(idCaja) );
                return Ok( ingresosCajas );
            }else{
                return BadRequest();
            }
            
        }
    }
    
}
