using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using Sicem_Blazor.Data;
using Sicem_Blazor.SeguimientoCobros.Models;

namespace Sicem_Blazor.SeguimientoCobros.Data {

    public class IncomeOfficeService {

        private readonly string cadConexion;

        public IncomeOfficeService(string connection_string){
            this.cadConexion = connection_string;
        }

        public IEnumerable<OfficePushpinMap> GetIncomes(){
            var results = new List<OfficePushpinMap>();
            using(var sqlConnection = new SqlConnection(cadConexion) ){
                sqlConnection.Open();
                var command = new SqlCommand( StoreProceduresIncomeOffice.COBROS_VIVO, sqlConnection);
                command.CommandType = CommandType.StoredProcedure;
                using(SqlDataReader reader = command.ExecuteReader()){
                    while(reader.Read()){
                        var id = ConvertUtils.ParseInteger( reader["id_sucursal"].ToString() );
                        var oficina = reader["sucursal"].ToString();
                        results.Add( new OfficePushpinMap(id, oficina){
                            Lat = reader["latitud"].ToString(),
                            Lon = reader["longitud"].ToString(),
                            Bills = ConvertUtils.ParseInteger( reader["recibos"].ToString() ),
                            Income = ConvertUtils.ParseDecimal( reader["cobrado"].ToString() ),
                        });
                    }
                }
                sqlConnection.Close();
            }
            return results;
        }

        public IEnumerable<IngresoCaja> GetCobrosHorario(DateTime dia, int id_sucursal)
        {
            var results = new List<IngresoCaja>();

            using(var sqlConnection = new SqlConnection(cadConexion) ){
                sqlConnection.Open();
                var command = new SqlCommand( StoreProceduresIncomeOffice.COBROS_CAJAS, sqlConnection);
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add( new SqlParameter( "id_sucursal", id_sucursal ));
                command.Parameters.Add( new SqlParameter( "dia", dia.ToString("yyyyMMdd") ));
                using(SqlDataReader reader = command.ExecuteReader()){
                    while(reader.Read()){
                        var id = reader["id_cajero"].ToString();
                        var caja = reader["caja"].ToString();
                        results.Add( new IngresoCaja(id, caja){
                            Recibos = ConvertUtils.ParseInteger( reader["recibos"].ToString() ),
                            Cobrado = ConvertUtils.ParseDecimal( reader["cobrado"].ToString() ),
                            Hora0708 = ConvertUtils.ParseDecimal( reader["07-08"].ToString() ),
                            Hora0809 = ConvertUtils.ParseDecimal( reader["08-09"].ToString() ),
                            Hora0910 = ConvertUtils.ParseDecimal( reader["09-10"].ToString() ),
                            Hora1011 = ConvertUtils.ParseDecimal( reader["10-11"].ToString() ),
                            Hora1112 = ConvertUtils.ParseDecimal( reader["11-12"].ToString() ),
                            Hora1213 = ConvertUtils.ParseDecimal( reader["12-13"].ToString() ),
                            Hora1314 = ConvertUtils.ParseDecimal( reader["13-14"].ToString() ),
                            Hora1415 = ConvertUtils.ParseDecimal( reader["14-15"].ToString() ),
                            Hora1516 = ConvertUtils.ParseDecimal( reader["15-16"].ToString() ),
                            Hora1617 = ConvertUtils.ParseDecimal( reader["16-17"].ToString() ),
                            Hora1718 = ConvertUtils.ParseDecimal( reader["17-18"].ToString() ),
                            Hora1819 = ConvertUtils.ParseDecimal( reader["18-19"].ToString() ),
                            Hora1920 = ConvertUtils.ParseDecimal( reader["19-20"].ToString() )
                        });
                    }
                }
                sqlConnection.Close();
            }


            // Generate total row
            if(results.Count > 0 ){
                var totalRow = new IngresoCaja("zzz", "TOTAL")
                {
                    Recibos = results.Sum(item => item.Recibos),
                    Cobrado = results.Sum(item => item.Cobrado),
                    Hora0708 = results.Sum(item => item.Hora0708),
                    Hora0809 = results.Sum(item => item.Hora0809),
                    Hora0910 = results.Sum(item => item.Hora0910),
                    Hora1011 = results.Sum(item => item.Hora1011),
                    Hora1112 = results.Sum(item => item.Hora1112),
                    Hora1213 = results.Sum(item => item.Hora1213),
                    Hora1314 = results.Sum(item => item.Hora1314),
                    Hora1415 = results.Sum(item => item.Hora1415),
                    Hora1516 = results.Sum(item => item.Hora1516),
                    Hora1617 = results.Sum(item => item.Hora1617),
                    Hora1718 = results.Sum(item => item.Hora1718),
                    Hora1819 = results.Sum(item => item.Hora1819),
                    Hora1920 = results.Sum(item => item.Hora1920)
                };
                results.Add( totalRow );
            }

            // return results;
            return results;
        }

    }
    
}