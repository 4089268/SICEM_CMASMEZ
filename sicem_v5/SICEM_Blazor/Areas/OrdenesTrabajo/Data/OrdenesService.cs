using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sicem_Blazor.Models;
using Sicem_Blazor.Data;
using Sicem_Blazor.OrdenesTrabajos.Models;
using Sicem_Blazor.Data.Contracts;

namespace Sicem_Blazor.Services
{
    public class OrdenesService
    {
        private readonly ILogger<OrdenesService> logger;
        
        public OrdenesService(ILogger<OrdenesService> logger)
        {
            this.logger = logger;
        }

        public Ordenes_Resumen ObtenerOrdenesResumen(IEnlace enlace, string Fecha1, string Fecha2, int Sb, int Sect)
        {
            var respuesta = new Ordenes_Resumen();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "RESUMEN");
                    sqlCommand.Parameters.AddWithValue("@xfec1", Fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", Fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", Sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", Sect);
                    var xDataSet = new DataSet();
                    var xDataAdapter = new SqlDataAdapter(sqlCommand);
                    xDataAdapter.Fill(xDataSet);
                    if (xDataSet.Tables.Count != 3)
                    {
                        throw new Exception("Error al hacer la consulta a la base de datos.");
                    }

                    // ****** Obtener Totales ******
                    DataRow xRow = xDataSet.Tables[0].Rows[0];
                    respuesta.Pendi = int.Parse(xRow["pendi"].ToString());
                    respuesta.Eneje = int.Parse(xRow["eneje"].ToString());
                    respuesta.Reali = int.Parse(xRow["reali"].ToString());
                    respuesta.Cance = int.Parse(xRow["Cance"].ToString());
                    respuesta.Eje = int.Parse(xRow["eje"].ToString());
                    respuesta.No_eje = int.Parse(xRow["no_eje"].ToString());
                    respuesta.Total = int.Parse(xRow["total"].ToString());

                    // ****** Obtener Trabajos ******
                    var tmpList = new List<Ordenes_Resumen_Trabajo>();
                    foreach (DataRow xRow1 in xDataSet.Tables[1].Rows)
                    {
                        tmpList.Add(new Ordenes_Resumen_Trabajo
                        {
                            Id = int.Parse(xRow1["id_trabajo"].ToString()),
                            Descripcion = xRow1["descripcion"].ToString(),
                            Pendi = int.Parse(xRow1["pendi"].ToString()),
                            Eneje = int.Parse(xRow1["eneje"].ToString()),
                            Reali = int.Parse(xRow1["reali"].ToString()),
                            Cance = int.Parse(xRow1["cance"].ToString()),
                            Eje = int.Parse(xRow1["eje"].ToString()),
                            No_eje = int.Parse(xRow1["no_eje"].ToString()),
                            Total = int.Parse(xRow1["total"].ToString())
                        });
                    }
                    respuesta.Trabajos = tmpList.ToArray<Ordenes_Resumen_Trabajo>();

                    // ****** Obtener Departamentos ******
                    var tmpList2 = new List<Ordenes_Resumen_Departamento>();
                    foreach (DataRow xRow1 in xDataSet.Tables[2].Rows)
                    {
                        tmpList2.Add(new Ordenes_Resumen_Departamento
                        {
                            Id = int.Parse(xRow1["id_departamento"].ToString()),
                            Descripcion = xRow1["descripcion"].ToString(),
                            Total = int.Parse(xRow1["total"].ToString())
                        });
                    }
                    respuesta.Departamentos = tmpList2.ToArray<Ordenes_Resumen_Departamento>();
                }
            }
            return respuesta;
        }
        public OrdenesResumenOficina ObtenerOrdenesPorOficina(IEnlace enlace, string fecha1, string fecha2, int sb, int sect)
        {
            var response = new OrdenesResumenOficina(enlace)
            {
                Estatus = ResumenOficinaEstatus.Error
            };

            try
            {
                using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                    {
                        sqlCommand.CommandType = CommandType.StoredProcedure;
                        sqlCommand.Parameters.AddWithValue("@cAlias", "TOTALES");
                        sqlCommand.Parameters.AddWithValue("@xfec1", fecha1);
                        sqlCommand.Parameters.AddWithValue("@xfec2", fecha2);
                        sqlCommand.Parameters.AddWithValue("@nSub", sb);
                        sqlCommand.Parameters.AddWithValue("@nSec", sect);
                        using (SqlDataReader reader = sqlCommand.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                response = OrdenesResumenOficina.FromDataReader(enlace, reader);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError("Error al procesar la consulta oficina {enlace}: {Message}", enlace.Nombre, ex.Message);
                response.Estatus = ResumenOficinaEstatus.Error;
            }
            return response;
        }

        public Ordenes_Detalle[] ObtenerOrdenesDetalle(IEnlace enlace, string Fecha1, string Fecha2, int Sb, int Sect, string filtro)
        {
            var respuesta = new List<Ordenes_Detalle>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "DETALLE_ORDENES");
                    sqlCommand.Parameters.AddWithValue("@xfec1", Fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", Fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", Sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", Sect);
                    sqlCommand.Parameters.AddWithValue("@cFiltro", filtro);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(new Ordenes_Detalle
                            {
                                Orden = reader["id_orden"].ToString(),
                                Cuenta = long.Parse(reader["id_cuenta"].ToString()),
                                Nombre = reader["nombre"].ToString(),
                                Id_Trabajo = int.Parse(reader["id_trabajo"].ToString()),
                                Trabajo = reader["trabajo"].ToString(),
                                Fecha_Genero = DateTime.Parse(reader["fecha_genero"].ToString()).ToString("dd/MM/yyyy HH:mm:ss"),
                                Fecha_Realizo = (reader.IsDBNull("fecha_realizo") ? "" : DateTime.Parse(reader["fecha_realizo"].ToString()).ToString("dd/MM/yyyy HH:mm:ss")),
                                Id_Estatus = int.Parse(reader["id_estatus"].ToString()),
                                Estatus = reader["estatus"].ToString(),
                                Colonia = reader["colonia"].ToString(),
                                Id_Colonia = int.Parse(reader["id_colonia"].ToString()),
                                Tarifa = reader["tarifa"].ToString(),
                                Id_Tarifa = int.Parse(reader["id_tarifa"].ToString()),
                                Id_Genero = reader["id_genero"].ToString(),
                                Genero = reader["genero"].ToString(),
                                Motivo = reader["motivo"].ToString(),
                                Resultado = reader["resultado"].ToString(),
                                Id_Departamento = 0,
                                Departamento = "indefinido",
                                Id_Realizo = int.Parse(reader["id_realizo"].ToString()),
                                Realizo = reader["realizo"].ToString()
                            });
                        }
                    }
                }
            }
            return respuesta.ToArray();
        }

        public Ordenes_Agrupado[] ObtenerOrdenesColonias(IEnlace enlace, string Fecha1, string Fecha2, int Sb, int Sect, string Filtro)
        {
            var respuesta = new List<Ordenes_Agrupado>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "POR_COLONIAS");
                    sqlCommand.Parameters.AddWithValue("@xfec1", Fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", Fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", Sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", Sect);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(new Ordenes_Agrupado
                            {
                                Id = int.Parse(reader["id_colonia"].ToString()),
                                Descripcion = reader["colonia"].ToString(),
                                Pendientes = int.Parse(reader["pendi"].ToString()),
                                En_Ejecucion = int.Parse(reader["eneje"].ToString()),
                                Realizadas = int.Parse(reader["reali"].ToString()),
                                Canceladas = int.Parse(reader["cance"].ToString()),
                                Ejecutadas = int.Parse(reader["eje"].ToString()),
                                No_Ejecutadas = int.Parse(reader["no_eje"].ToString()),
                                Total = int.Parse(reader["total"].ToString())
                            });
                        }
                    }
                }
            }
            return respuesta.ToArray();
        }

        public IEnumerable<Ordenes_Agrupado> ObtenerOrdenesTrabajos(IEnlace enlace, string fecha1, string fecha2, int sb, int sect)
        {
            var respuesta = new List<Ordenes_Agrupado>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "POR_TRABAJOS");
                    sqlCommand.Parameters.AddWithValue("@xfec1", fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", sect);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(new Ordenes_Agrupado
                            {
                                Id = int.Parse(reader["id_trabajo"].ToString()),
                                Descripcion = reader["trabajo"].ToString(),
                                Pendientes = int.Parse(reader["pendi"].ToString()),
                                En_Ejecucion = int.Parse(reader["eneje"].ToString()),
                                Realizadas = int.Parse(reader["reali"].ToString()),
                                Canceladas = int.Parse(reader["cance"].ToString()),
                                Ejecutadas = int.Parse(reader["eje"].ToString()),
                                No_Ejecutadas = int.Parse(reader["no_eje"].ToString()),
                                Total = int.Parse(reader["total"].ToString())
                            });
                        }
                    }
                }
            }
            return respuesta;
        }

        public IEnumerable<Ordenes_Realizacion> ObtenerOrdenesRealizacion(IEnlace enlace, string Fecha1, string Fecha2, int Sb, int Sect, string Filtro)
        {
            var respuesta = new List<Ordenes_Realizacion>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "POR_REALIZACION");
                    sqlCommand.Parameters.AddWithValue("@xfec1", Fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", Fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", Sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", Sect);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(Ordenes_Realizacion.FromDataReader(reader));
                        }
                    }
                }
            }
            return respuesta.ToArray();
        }

        public Ordenes_Capturacion[] ObtenerOrdenesCapturacion(IEnlace enlace, string Fecha1, string Fecha2, int Sb, int Sect, string Filtro)
        {
            var respuesta = new List<Ordenes_Capturacion>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@cAlias", "POR_CAPTURACION");
                    sqlCommand.Parameters.AddWithValue("@xfec1", Fecha1);
                    sqlCommand.Parameters.AddWithValue("@xfec2", Fecha2);
                    sqlCommand.Parameters.AddWithValue("@nSub", Sb);
                    sqlCommand.Parameters.AddWithValue("@nSec", Sect);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(new Ordenes_Capturacion
                            {
                                Id = reader["id_capturo"].ToString(),
                                Descripcion = reader["nombre"].ToString(),
                                Pendientes = int.Parse(reader["pendi"].ToString()),
                                En_Ejecucion = int.Parse(reader["eneje"].ToString()),
                                Realizadas = int.Parse(reader["reali"].ToString()),
                                Canceladas = int.Parse(reader["cance"].ToString()),
                                Ejecutadas = int.Parse(reader["eje"].ToString()),
                                No_Ejecutadas = int.Parse(reader["no_eje"].ToString()),
                                Total = int.Parse(reader["total"].ToString())
                            });
                        }
                    }
                }
            }
            return respuesta.ToArray();
        }

        public IEnumerable<Ordenes_PagoRealizadoItem> Ordenes_PagosRealizadosOrdenes(IEnlace enlace, string fecha1, string fecha2, int id_trabajo = -1, int id_estatus = -1)
        {
            var respuesta = new List<Ordenes_PagoRealizadoItem>();
            using (var sqlConnection = new SqlConnection(enlace.GetConnectionString()))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand("[Sicem].[OrdenesTrabajos_PagosRealizados]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;
                    sqlCommand.Parameters.AddWithValue("@fecha1", fecha1);
                    sqlCommand.Parameters.AddWithValue("@fecha2", fecha2);
                    sqlCommand.Parameters.AddWithValue("@id_trabajo", id_trabajo);
                    sqlCommand.Parameters.AddWithValue("@id_estatus", id_estatus);
                    using (SqlDataReader reader = sqlCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            respuesta.Add(Ordenes_PagoRealizadoItem.FromDataReader(reader));
                        }
                    }
                }
            }
            return respuesta;
        }

        public static string GenerarFiltro_Detalle(int isEstatus = 0, int ejecutada = -1, int colonia = -1, int trabajo = -1, int realizo = -1, string capturo = "")
        {
            var xmlFiltro = new System.Text.StringBuilder();
            xmlFiltro.Append("<filtro>");
            xmlFiltro.Append($"<id_estatus>{isEstatus}</id_estatus>");
            xmlFiltro.Append($"<ejecutada>{ejecutada}</ejecutada>");
            xmlFiltro.Append($"<colonia>{colonia}</colonia>");
            xmlFiltro.Append($"<id_trabajo>{trabajo}</id_trabajo>");
            xmlFiltro.Append($"<id_realizo>{realizo}</id_realizo>");
            xmlFiltro.Append($"<id_capturo>{capturo}</id_capturo>");
            xmlFiltro.Append("</filtro>");
            return xmlFiltro.ToString();
        }
    }
}