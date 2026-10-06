using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sicem_Blazor.Attributes;

namespace Sicem_Blazor.Controllers.Api;

[ApiAuth]
[ApiController]
[Route("/api/[Controller]")]
public class CuentasController : ControllerBase
{
    private readonly ILogger<CuentasController> logger;
    private readonly string connectionString;

    public CuentasController(IConfiguration configuration, ILogger<CuentasController> l)
    {
        connectionString = configuration.GetConnectionString("ARQUOS");
        logger = l;
    }

    [HttpGet]
        public IActionResult ObtenerCuentas()
        {
            var cuentas = new List<dynamic>();
            var _culture = new CultureInfo("es-MX");
            var query = @"SELECT
                            p.id_padron,
                            p.id_cuenta,
                            p.razon_social,
                            p.sb,
                            p.sector,
                            p._localizacion as localizacion,
                            r.Redondeado as total,
                            p.af,
                            p.mf,
                            p._fecha_factura_act as fecha_factura_act,
                            CONCAT(p.af, '-', RIGHT(Concat(REPLICATE('0',2), mf),2) ) AS periodo,
                            p._estatus
                        FROM [Padron].[vw_Cat_Padron] p
                        CROSS APPLY [Global].[uif_Redondear](P.total) r
                        WHERE p._estatus like 'Activo'";

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var sqlCommand = new SqlCommand(query, connection)
                {
                    CommandType = System.Data.CommandType.Text,
                };

                using (var reader = sqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cuentas.Add(new
                            {
                                IdPadron = reader["id_padron"],
                                IdCuenta = reader["id_cuenta"],
                                RazonSocial = reader["razon_social"],
                                Sb = reader["sb"],
                                Sector = reader["sector"],
                                Localizacion = reader["localizacion"],
                                Total = reader["total"],
                                Af = reader["af"],
                                Mf = reader["mf"],
                                FechaFacturaAct = reader["fecha_factura_act"],
                                Periodo = reader["periodo"],
                                Estatus = reader["_estatus"]
                            }
                        );
                    }
                }
                connection.Close();
            }

            this.logger.LogInformation("Total cuentas: {totalCuentas}", cuentas.Count);

            return Ok(cuentas);
        }

}