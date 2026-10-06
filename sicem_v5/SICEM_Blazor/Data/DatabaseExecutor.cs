using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace SICEM_Blazor.Data
{
    public class DatabaseExecutor
    {
        private readonly string _connectionString = "";

        public DatabaseExecutor( string connectionString)
        {
            this._connectionString = connectionString;
        }

        public IEnumerable<T> ExecuteQuery<T>(string sql_query, Func<DbDataReader, T> mapFunc)
        {
            using var sqlConnection = new SqlConnection(this._connectionString);
            sqlConnection.Open();
            using var sqlCommand = new SqlCommand()
            {
                Connection = sqlConnection,
                CommandText = sql_query,
                CommandType = System.Data.CommandType.Text
            };
            using var sqlDataReader =  sqlCommand.ExecuteReader();
            var results = new List<T>();
            while(sqlDataReader.Read())
            {
                results.Add( mapFunc(sqlDataReader) );
            }
            sqlConnection.Close();
            return results ;
        }

        public IEnumerable<T> ExecuteStoredProcedure<T>(string procedure_name, Dictionary<string, object> parameters, Func<DbDataReader, T> mapFunc)
        {
            using var sqlConnection = new SqlConnection(this._connectionString);
            sqlConnection.Open();
            using var sqlCommand = new SqlCommand()
            {
                Connection = sqlConnection,
                CommandText = procedure_name,
                CommandType = System.Data.CommandType.StoredProcedure
            };
            foreach( var parameter in parameters)
            {
                sqlCommand.Parameters.Add( new SqlParameter(parameter.Key, parameter.Value) );
            }
            using var sqlDataReader =  sqlCommand.ExecuteReader();
            var results = new List<T>();
            while(sqlDataReader.Read())
            {
                results.Add( mapFunc(sqlDataReader) );
            }
            sqlConnection.Close();
            return results ;
        }
        
    }
}