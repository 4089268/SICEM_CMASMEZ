using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Sicem_Blazor.Data;
using Sicem_Blazor.Facturacion.Models;
using Microsoft.Extensions.Logging;
using SICEM_Blazor.Data;
using System.Diagnostics;

namespace Sicem_Blazor.Services {
    public class FacturacionService {
        private readonly IConfiguration appSettings;
        private readonly SicemService sicemService;
        private readonly ILogger<FacturacionService> logger;

        public FacturacionService(IConfiguration c, SicemService s, ILogger<FacturacionService> l) {
            this.appSettings = c;
            this.sicemService = s;
            this.logger = l;
        }
        
        public Facturacion_Oficina ObtenerFacturacionOficina(IEnlace enlace, int anio, int mes, int sb, int sec){
            try {
                var databaseExecutor = new DatabaseExecutor( enlace.GetConnectionString() );
                var _query = string.Format("Execute [SICEM].[Facturacion_01] @nAno = {0}, @nMes = {1}, @nSb = {2}, @nSect = {3}", anio, mes, sb, sec);
                var results = databaseExecutor.ExecuteQuery<Facturacion_Oficina>( _query, (dataReader)=>{
                    return new Facturacion_Oficina
                    {
                        Id_Oficina = enlace.Id,
                        Oficina = enlace.Nombre,
                        Estatus = 1,
                        Domestico_Usu = int.TryParse(dataReader["usu_domestico"].ToString(), out int tmpInt) ? tmpInt : 0,
                        Domestico_Fact = decimal.TryParse(dataReader["fac_domestico"].ToString(), out decimal tmpDec) ? tmpDec : 0m,
                        Comercial_Usu = int.TryParse(dataReader["usu_comercial"].ToString(), out tmpInt) ? tmpInt : 0,
                        Comercial_Fact = decimal.TryParse(dataReader["fac_comercial"].ToString(), out tmpDec) ? tmpDec : 0m,
                        Industrial_Usu = int.TryParse(dataReader["usu_industrial"].ToString(), out tmpInt) ? tmpInt : 0,
                        Industrial_Fact = decimal.TryParse(dataReader["fac_industrial"].ToString(), out tmpDec) ? tmpDec : 0m,
                        ServGener_Usu = int.TryParse(dataReader["usu_servGen"].ToString(), out tmpInt) ? tmpInt : 0,
                        ServGener_Fact = decimal.TryParse(dataReader["fac_servGen"].ToString(), out tmpDec) ? tmpDec : 0m,
                        Subtotal = decimal.TryParse(dataReader["subtotal"].ToString(), out tmpDec) ? tmpDec : 0m,
                        Iva = decimal.TryParse(dataReader["iva"].ToString(), out tmpDec) ? tmpDec : 0m,
                        Total = decimal.TryParse(dataReader["total"].ToString(), out tmpDec) ? tmpDec : 0m,
                        Usuarios = int.TryParse(dataReader["usuarios"].ToString(), out tmpInt) ? tmpInt : 0
                    };
                });
                return results.First();
            }catch(Exception err){
                Console.WriteLine($">> Error al obtener la facturacion de la oficina {enlace.Nombre}\n\tError:{err.Message}\n\tStacktrace:{err.StackTrace}");
                return new Facturacion_Oficina(){
                    Id_Oficina = enlace.Id,
                    Oficina = enlace.Nombre,
                    Estatus = 2
                };
            }
        }

        public IEnumerable<Facturacion_Conceptos> ObtenerFacturacionConceptos(IEnlace enlace, int anio, int mes, int sb, int sec, int idLocalidad = 0) {
            try{
                var databaseExecutor = new DatabaseExecutor( enlace.GetConnectionString() );
                var _query = string.Format("Execute [SICEM].[Facturacion_02] @nAno = {0}, @nMes = {1}, @idLocalidad = {2}, @nSub = {3}, @nSec = {4}", anio, mes, idLocalidad, sb, sec);
                var results = databaseExecutor.ExecuteQuery<Facturacion_Conceptos>( _query, (dataReader)=>{
                    return new Facturacion_Conceptos
                    {
                        Id_Oficina = enlace.Id,
                        Oficina = enlace.Nombre,
                        Es_Rezago = 0,
                        Id_Concepto = int.TryParse(dataReader["id_concepto"].ToString(), out int tmpInt) ? tmpInt : 0,
                        Concepto = dataReader["concepto"].ToString().ToUpper(),
                        Domestico_Sub = decimal.TryParse(dataReader.GetValue("sub_domestico").ToString(), out decimal tmpDec) ? tmpDec : 0m,
                        Domestico_Iva = decimal.TryParse(dataReader.GetValue("iva_domestico").ToString(), out tmpDec) ? tmpDec : 0m,
                        Domestico_Total = decimal.TryParse(dataReader.GetValue("tot_domestico").ToString(), out tmpDec) ? tmpDec : 0m,
                        Comercial_Sub = decimal.TryParse(dataReader.GetValue("sub_comercial").ToString(), out tmpDec) ? tmpDec : 0m,
                        Comercial_Iva = decimal.TryParse(dataReader.GetValue("iva_comercial").ToString(), out tmpDec) ? tmpDec : 0m,
                        Comercial_Total = decimal.TryParse(dataReader.GetValue("tot_comercial").ToString(), out tmpDec) ? tmpDec : 0m,
                        Industrial_Sub = decimal.TryParse(dataReader.GetValue("sub_industrial").ToString(), out tmpDec) ? tmpDec : 0m,
                        Industrial_Iva = decimal.TryParse(dataReader.GetValue("iva_industrial").ToString(), out tmpDec) ? tmpDec : 0m,
                        Industrial_Total = decimal.TryParse(dataReader.GetValue("tot_industrial").ToString(), out tmpDec) ? tmpDec : 0m,
                        ServGen_Sub = decimal.TryParse(dataReader.GetValue("sub_servGen").ToString(), out tmpDec) ? tmpDec : 0m,
                        ServGen_Iva = decimal.TryParse(dataReader.GetValue("iva_servGen").ToString(), out tmpDec) ? tmpDec : 0m,
                        ServGen_Total = decimal.TryParse(dataReader.GetValue("tot_servGen").ToString(), out tmpDec) ? tmpDec : 0m,
                        Subtotal = decimal.TryParse(dataReader.GetValue("subtotal").ToString(), out tmpDec) ? tmpDec : 0m,
                        Iva = decimal.TryParse(dataReader.GetValue("iva").ToString(), out tmpDec) ? tmpDec : 0m,
                        Total = decimal.TryParse(dataReader.GetValue("total").ToString(), out tmpDec) ? tmpDec : 0m
                    };
                });
                return results;

            }catch(Exception err){
                Console.WriteLine($">> Error al obtener la facturacion por conceptos de la oficina {enlace.Nombre}\n\tError:{err.Message}\n\tStacktrace:{err.StackTrace}");
                return null;
            }
        }

        public Facturacion_Usuarios[] ObtenerFacturacionUsuarios(IEnlace enlace, int anio, int mes, int sb, int sec) {
            var respuesta = new List<Facturacion_Usuarios>();            
            using(var xConnecton = new SqlConnection(enlace.GetConnectionString() )) {
                xConnecton.Open();
                using(var xCommand = new SqlCommand()) {
                    xCommand.Connection = xConnecton;
                    xCommand.CommandText = $"EXEC [SICEM].[Facturacion] 'FACTURACION_DETALLE_CONPAGO',{sb},{sec},{anio},{mes}";
                    using(SqlDataReader xReader = xCommand.ExecuteReader()) {
                        while(xReader.Read()) {
                            respuesta.Add(new Facturacion_Usuarios {
                                Id_Cuenta = long.Parse(xReader.GetValue("CUENTA").ToString()),
                                Localizacion = xReader.GetValue("LOCALIZACION").ToString(),
                                Nombre = xReader.GetValue("NOMBRE").ToString(),
                                Direccion = xReader.GetValue("DIRECCION").ToString(),
                                Estatus = xReader.GetValue("ESTATUS").ToString(),
                                Pago = true
                            });
                        }
                    }
                }
                using(var xCommand = new SqlCommand()) {
                    xCommand.Connection = xConnecton;
                    xCommand.CommandText = $"EXEC [SICEM].[Facturacion] 'FACTURACION_DETALLE_SINPAGO',{sb},{sec},{anio},{mes}";
                    using(SqlDataReader xReader = xCommand.ExecuteReader()) {
                        while(xReader.Read()) {
                            respuesta.Add(new Facturacion_Usuarios {
                                Id_Cuenta = long.Parse(xReader.GetValue("CUENTA").ToString()),
                                Localizacion = xReader.GetValue("LOCALIZACION").ToString(),
                                Nombre = xReader.GetValue("NOMBRE").ToString(),
                                Direccion = xReader.GetValue("DIRECCION").ToString(),
                                Estatus = xReader.GetValue("ESTATUS").ToString(),
                                Pago = false
                            });
                        }
                    }
                }
            }            
            return respuesta.ToArray();
        }


        public IEnumerable<FacturacionAnual> ObtenerFacturacionAnual(IEnlace enlace, int ano, int sb, int sec){
            var _arquos = new ArquosRepositorie(enlace);
            return _arquos.ObtenerFacturacionAnual(ano, sb, sec).ToList();
        }
        public IEnumerable<Factura> ObtenerFacturas(IEnlace oficina, int ano, int mes, int sb, int sec){
            var _arquos = new ArquosRepositorie(oficina);
            return _arquos.ObtenerFacturas(ano, mes, sb, sec);
        }

        public IEnumerable<FacturacionLocalidad> ObtenerFacturacionLocalidades(IEnlace enlace, int anio, int mes, int sb, int sec){
            var result = new List<FacturacionLocalidad>();
            try{
                using(var sqlConnection = new SqlConnection(enlace.GetConnectionString())){
                    sqlConnection.Open();
                    var _query = string.Format("exec sicem.facturacion 'FACTURACION_LOCALIDADES', {0}, {1}, {2}, {3}", sb, sec, anio, mes);
                    var _command = new SqlCommand(_query, sqlConnection);
                    _command.CommandTimeout = (int)TimeSpan.FromMinutes(15).TotalSeconds;
                    using( var reader = _command.ExecuteReader()){
                        while(reader.Read()){
                            result.Add( FacturacionLocalidad.FromDataReader(reader));
                        }
                    }
                    sqlConnection.Close();
                }
                return result;

            }catch(Exception err){
                logger.LogError(err, $"Error al obtener la facturacion por localidades de {enlace.Nombre}");
                return new List<FacturacionLocalidad>();
            }
        }

        public IEnumerable<FacturacionColonia> ObtenerFacturacionColonias(IEnlace enlace, int anio, int mes, int sb, int sec, int idLocalidad){
            var result = new List<FacturacionColonia>();
            try{
                using(var sqlConnection = new SqlConnection(enlace.GetConnectionString())){
                    sqlConnection.Open();
                    var _query = string.Format("exec sicem.facturacion 'FACTURACION_LOCALIDADES_COLONIAS', {0}, {1}, {2}, {3}, @nLocalidad = {4}", sb, sec, anio, mes, idLocalidad);
                    var _command = new SqlCommand(_query, sqlConnection);
                    _command.CommandTimeout = (int)TimeSpan.FromMinutes(15).TotalSeconds;
                    using( var reader = _command.ExecuteReader()){
                        while(reader.Read()){
                            result.Add( FacturacionColonia.FromDataReader(reader));
                        }
                    }
                    sqlConnection.Close();
                }
                return result;

            }catch(Exception err){
                logger.LogError(err, $"Error al obtener la facturacion por localidades de {enlace.Nombre}");
                return new List<FacturacionColonia>();
            }
        }
    
    }
}