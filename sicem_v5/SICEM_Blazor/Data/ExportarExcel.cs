using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using ClosedXML.Excel;
using Sicem_Blazor.Services;


namespace Sicem_Blazor.Data
{
    /// <summary>
    /// Genera un archivo de Excel (ClosedXML) con todos los campos de <typeparamref name="T"/>
    /// en la carpeta temporal; devuelve el identificador de la carpeta para descargarlo desde la API.
    /// </summary>
    public class ExportarExcel<T>
    {
        private readonly Type _dataType;
        private readonly ExportarExcelProperties _propiedadesExportar;
        private readonly Uri _tmpFolder;
        private ICollection<T> _datos;


        public ExportarExcel(ICollection<T> datos, Uri tmpFolder)
        {
            this._dataType = typeof(T);
            this._datos = datos;
            this._tmpFolder = tmpFolder;
            this._propiedadesExportar = new ExportarExcelProperties();
        }

        public ExportarExcel(ICollection<T> datos, Uri tmpFolder, ExportarExcelProperties props)
        {
            this._dataType = typeof(T);
            this._datos = datos;
            this._tmpFolder = tmpFolder;
            this._propiedadesExportar = props;
        }


        public string GenerarExcel()
        {
            var _guid = Guid.NewGuid().ToString();
            var _nombreArchivo = $"{_propiedadesExportar.Titulo}.xlsx";
            var _rutaArchivo = $"{_tmpFolder.AbsolutePath}{_guid}{System.IO.Path.DirectorySeparatorChar}{_nombreArchivo}";
            var _fileInfo = new System.IO.FileInfo(_rutaArchivo);
            _fileInfo.Directory.Create();

            Console.WriteLine("Generando archivo en ruta " + _rutaArchivo);

            using (var libro = new XLWorkbook())
            {
                var sheet = libro.Worksheets.Add("My Sheet");

                var _columnas = FiltrarColumnas(_dataType.GetRuntimeFields().ToList());
                int row = 1;

                // Generar cabeceras
                int col = 1;
                foreach (var _columna in _columnas)
                {
                    GenerarCabecera(row, col, sheet, _columna);
                    col++;
                }
                row++;

                // Generar datos
                foreach (var datoActual in _datos)
                {
                    col = 1;
                    foreach (var _columna in _columnas)
                    {
                        GenerarCelda(row, col, sheet, _columna, datoActual);
                        col++;
                    }
                    row++;
                }

                if (_columnas.Count > 0)
                {
                    sheet.Columns(1, _columnas.Count).AdjustToContents(1, Math.Min(row, 500));
                }

                // Guardar archivo
                libro.SaveAs(_fileInfo.FullName);
            }

            return _guid;
        }

        private ICollection<FieldInfo> FiltrarColumnas(ICollection<FieldInfo> fields)
        {
            return fields.Where(item => !_propiedadesExportar.CamposOmitir.Contains(ProcesarNombreCampo(item).ToLower())).ToList();
        }

        private void GenerarCabecera(int row, int col, IXLWorksheet sheet, FieldInfo field)
        {
            var _cell = sheet.Cell(row, col);
            _cell.Value = ProcesarNombreCampo(field).ToUpper();
            _cell.Style.Font.Bold = true;
            _cell.Style.Font.FontSize = 14;
        }

        private void GenerarCelda(int row, int col, IXLWorksheet sheet, FieldInfo field, T valor)
        {
            var _cell = sheet.Cell(row, col);
            _cell.Value = RadzenDataGridExportacionExcelService.ValorCelda(field.GetValue(valor));
            _cell.Style.Font.FontSize = 12;
        }


        public override string ToString()
        {
            var _fields = _dataType.GetRuntimeFields();
            Console.WriteLine($"Total Registro: {_datos.Count()} Total Fields: {_fields.Count()}");
            var _stringBuilder = new System.Text.StringBuilder();

            foreach (var f in _fields)
            {
                _stringBuilder.Append($"Field2:{ProcesarNombreCampo(f)} Type:{f.FieldType.Name}\n");
            }

            return _stringBuilder.ToString();
        }


        private string ProcesarNombreCampo(FieldInfo fieldText)
        {
            var name = fieldText.Name.Replace("<", "").Replace(">k__BackingField", "");
            return name;
        }

    }

    public class ExportarExcelProperties
    {
        public string Titulo { get; set; } = "Sicem";
        public ICollection<string> CamposOmitir { get; set; } = new string[] { "id_oficina", "id_padron" };
    }
}
