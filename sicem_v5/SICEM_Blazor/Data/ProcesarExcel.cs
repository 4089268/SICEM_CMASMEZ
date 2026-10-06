using System;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;

namespace Sicem_Blazor.Data
{
    /// <summary>
    /// Lectura de archivos de Excel (.xlsx / .xls) con ExcelDataReader.
    /// </summary>
    public class ProcesarExcel
    {
        static ProcesarExcel()
        {
            // Requerido por ExcelDataReader para leer archivos .xls (codificaciones de Windows)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>Lee todas las filas y columnas de la primera hoja (columnas "column0", "column1", …).</summary>
        public DataTable ToDataTable(string fileName)
        {
            return LeerPrimeraHoja(fileName, null);
        }

        /// <summary>Lee únicamente la primera columna de la primera hoja (columna "column0").</summary>
        public DataTable ToDataTableFirstCol(string fileName)
        {
            return LeerPrimeraHoja(fileName, 1);
        }

        private static DataTable LeerPrimeraHoja(string fileName, int? maxColumnas)
        {
            var dataTableResponse = new DataTable();

            using (var stream = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    while (reader.Read())
                    {
                        int totalColumnas = maxColumnas.HasValue ? Math.Min(maxColumnas.Value, reader.FieldCount) : reader.FieldCount;
                        while (dataTableResponse.Columns.Count < totalColumnas)
                        {
                            dataTableResponse.Columns.Add(new DataColumn($"column{dataTableResponse.Columns.Count}"));
                        }

                        var _tmpRow = dataTableResponse.NewRow();
                        for (int columna = 0; columna < totalColumnas; columna++)
                        {
                            _tmpRow[columna] = reader.GetValue(columna)?.ToString() ?? string.Empty;
                        }
                        dataTableResponse.Rows.Add(_tmpRow);
                    }
                }
            }

            if (maxColumnas.HasValue && dataTableResponse.Columns.Count == 0)
            {
                dataTableResponse.Columns.Add(new DataColumn("column0"));
            }

            return dataTableResponse;
        }
    }
}
