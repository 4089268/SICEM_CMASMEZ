using System;
using System.Data;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Sicem_Blazor.Data;
using Sicem_Blazor.Lecturas.Models;
using Microsoft.Extensions.Logging;

namespace Sicem_Blazor.Lecturas.Data {

    public class LecturasService {

        private readonly ILogger<LecturasService> logger;

        public LecturasService(ILogger<LecturasService> logger){
            this.logger = logger;
        }

        public IEnumerable<Lecturista> ObtenerLecturasPorLecturisa(DateRange dateRange, IEnlace enlace){
            logger.LogInformation("Obteniendo lecturas del enlace {enlace}", enlace.Nombre );
            var response = new List<Lecturista>();
            using(var sqlConnection = new SqlConnection(enlace.GetConnectionString())){
                sqlConnection.Open();
                var sqlCommand = new SqlCommand( StoredProcedures.DESEMPENIO_LECTURISTA, sqlConnection );
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Add( new SqlParameter("fecha", dateRange.Desde_ISO) );
                using(SqlDataReader reader = sqlCommand.ExecuteReader()){
                    while(reader.Read()){
                        response.Add( new Lecturista(){
                            IdLecturista = ConvertUtils.ParseInteger(reader["id_lecturista"].ToString()),
                            Nombre = reader["lecturista"].ToString(),
                            Inicio = reader.GetDateTime("inicio"),
                            Fin = reader.GetDateTime("termino"),
                            TotalLecturas = ConvertUtils.ParseInteger(reader["lecturas"].ToString()),
                            H0708 = ConvertUtils.ParseInteger(reader["07-08"].ToString()),
                            H0809 = ConvertUtils.ParseInteger(reader["08-09"].ToString()),
                            H0910 = ConvertUtils.ParseInteger(reader["09-10"].ToString()),
                            H1011 = ConvertUtils.ParseInteger(reader["10-11"].ToString()),
                            H1112 = ConvertUtils.ParseInteger(reader["11-12"].ToString()),
                            H1213 = ConvertUtils.ParseInteger(reader["12-13"].ToString()),
                            H1314 = ConvertUtils.ParseInteger(reader["13-14"].ToString()),
                            H1415 = ConvertUtils.ParseInteger(reader["14-15"].ToString()),
                            H1516 = ConvertUtils.ParseInteger(reader["15-16"].ToString()),
                            H1617 = ConvertUtils.ParseInteger(reader["16-17"].ToString())
                        });
                    }
                }
                sqlConnection.Close();
            }
            logger.LogInformation("Consulta lecturas enlace {enlace} concluido", enlace.Nombre);
            return response;
        }

        public IEnumerable<Incidencia> ObtenerIncidencias(DateRange dateRange, IEnlace enlace){
            logger.LogInformation("Obteniendo incidencias del enlace {enlace}", enlace.Nombre );
            var response = new List<Incidencia>();
            using(var sqlConnection = new SqlConnection(enlace.GetConnectionString())){
                sqlConnection.Open();
                var sqlCommand = new SqlCommand( StoredProcedures.RESUMEN_INCIDENCIAS, sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Add( new SqlParameter("desde", dateRange.Desde_ISO));
                sqlCommand.Parameters.Add( new SqlParameter("hasta", dateRange.Hasta_ISO));
                using(SqlDataReader reader = sqlCommand.ExecuteReader()){
                    while(reader.Read()){
                        response.Add( new Incidencia(){
                            Cuenta = (int) ConvertUtils.ParseInteger(reader["cuenta"].ToString()),
                            Localizacion = reader["localizacion"].ToString(),
                            Usuario = reader["usuario"].ToString(),
                            Lecturista = reader["lecturista"].ToString(),
                            Lectura = reader["lectura"].ToString(),
                            Anomalia = reader["anomalia"].ToString(),
                            Descripcion = reader["incidencia"].ToString(),
                            Fecha = reader.GetDateTime("fecha"),
                            Handheld = reader["handheld"].ToString()
                        });
                    }
                }
                sqlConnection.Close();
            }
            logger.LogInformation("Consulta incidencias lecturas enlace {enlace} concluido", enlace.Nombre);
            return response;
        }

        public IEnumerable<DetalleLecturista> DetalleLecturista(DateRange dateRange, IEnlace enlace, int id_lecturista){
            logger.LogInformation("Obteniendo detalle lecturas del lecturista {lecturista} del enlace {enlace}", id_lecturista, enlace.Nombre );
            var response = new List<DetalleLecturista>();
            using(var sqlConnection = new SqlConnection(enlace.GetConnectionString())){
                sqlConnection.Open();
                var sqlCommand = new SqlCommand(StoredProcedures.DETALLE_LECTURAS, sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                sqlCommand.Parameters.Add( new SqlParameter("fecha", dateRange.Desde_ISO ));
                sqlCommand.Parameters.Add( new SqlParameter("id_lecturista", id_lecturista ));
                using(SqlDataReader reader = sqlCommand.ExecuteReader()){
                    while(reader.Read()){
                        response.Add( new DetalleLecturista(){
                            Id_Localidad = ConvertUtils.ParseInteger(reader["id_localidad"].ToString()),
                            Localidad = reader["localidad"].ToString(),
                            Cuenta = (long) ConvertUtils.ParseInteger(reader["cuenta"].ToString()),
                            Localizacion = reader["localizacion"].ToString(),
                            Usuario = reader["usuario"].ToString(),
                            Direccion = reader["direccion"].ToString(),
                            Tarifa = reader["tarifa"].ToString(),
                            Fecha = reader.GetDateTime("fecha"),
                            Lectura_Anterior = ConvertUtils.ParseInteger( reader["lec_ant"].ToString() ),
                            Lectura_Actual = ConvertUtils.ParseInteger( reader["lec_act"].ToString() ),
                            Consumo = ConvertUtils.ParseInteger( reader["consumo"].ToString() ),
                            Anomalia = reader["anomalia"].ToString(),
                            Incidencia = reader["incidencia"].ToString()
                        });
                    }
                }
                sqlConnection.Close();
            }
            return response;
        }
    }


    public class StoredProcedures {
        public static readonly string DESEMPENIO_LECTURISTA = "[SICEM_Casf].[usp_desempeño_lecturista]";
        public static readonly string RESUMEN_INCIDENCIAS = "[SICEM_Casf].[usp_incidencias_lecturas]";
        public static readonly string DETALLE_LECTURAS = "[SICEM_Casf].[usp_detalle_lecturista]";
    }
    
}