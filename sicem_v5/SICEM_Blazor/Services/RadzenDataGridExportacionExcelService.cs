using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.JSInterop;
using Radzen.Blazor;
using Sicem_Blazor.Models;

namespace Sicem_Blazor.Services;

/// <summary>
/// Exporta a Excel (ClosedXML) los datos de un RadzenDataGrid tal como se ven:
/// columnas visibles con su título y formato, y filas filtradas y ordenadas (todas las páginas).
/// Reemplaza a SfGrid.ExcelExport / ExportToExcelAsync de Syncfusion.
/// </summary>
public class RadzenDataGridExportacionExcelService
{
    private const string _contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly IJSRuntime _jsRuntime;

    public RadzenDataGridExportacionExcelService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Genera el Excel del grid y lo descarga en el navegador.
    /// </summary>
    public async Task ExportarAsync<T>(RadzenDataGrid<T> grid, ExcelExportProperties propiedades = null)
    {
        if (grid == null)
        {
            return;
        }

        propiedades ??= new ExcelExportProperties();
        var columnas = grid.ColumnsCollection
            .Where(columna => !string.IsNullOrWhiteSpace(columna.Property))
            .Where(columna => propiedades.IncludeHiddenColumn || columna.GetVisible())
            .Select(columna => new ColumnaExportacion(columna.GetTitle() ?? columna.Property, columna.Property, columna.FormatString))
            .ToList();
        var datos = grid.View?.ToList() ?? new List<T>();

        var contenido = GenerarExcel(columnas, datos, propiedades.SheetName);
        var nombreArchivo = string.IsNullOrWhiteSpace(propiedades.FileName)
            ? $"sicem_{Guid.NewGuid().ToString("N")}.xlsx"
            : propiedades.FileName;

        await DescargarAsync(contenido, nombreArchivo);
    }

    /// <summary>
    /// Descarga un arreglo de bytes como archivo en el navegador.
    /// </summary>
    public async Task DescargarAsync(byte[] contenido, string nombreArchivo)
    {
        using var stream = new MemoryStream(contenido);
        using var streamReference = new DotNetStreamReference(stream);
        await _jsRuntime.InvokeVoidAsync("descargarArchivoDesdeStream", nombreArchivo, streamReference, _contentType);
    }

    /// <summary>
    /// Genera el libro de Excel: encabezados en negritas y una fila por registro con su tipo de dato.
    /// </summary>
    public byte[] GenerarExcel<T>(IList<ColumnaExportacion> columnas, IList<T> datos, string nombreHoja = "Datos")
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(string.IsNullOrWhiteSpace(nombreHoja) ? "Datos" : nombreHoja);

        // * Encabezados
        for (int c = 0; c < columnas.Count; c++)
        {
            var celda = hoja.Cell(1, c + 1);
            celda.Value = columnas[c].Titulo;
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8E8E8");
        }

        // * Filas
        var accesos = columnas.Select(columna => CrearAcceso(typeof(T), columna.Propiedad)).ToList();
        var formatos = columnas.Select(columna => FormatoExcel(columna.FormatString)).ToList();
        for (int f = 0; f < datos.Count; f++)
        {
            for (int c = 0; c < columnas.Count; c++)
            {
                var valor = accesos[c](datos[f]);
                var celda = hoja.Cell(f + 2, c + 1);
                celda.Value = ValorCelda(valor);
                if (formatos[c] != null)
                {
                    celda.Style.NumberFormat.Format = formatos[c];
                }
            }
        }

        hoja.SheetView.FreezeRows(1);
        if (columnas.Count > 0)
        {
            // Ajustar el ancho con base en las primeras filas (en archivos grandes recorrer todo es lento)
            hoja.Columns(1, columnas.Count).AdjustToContents(1, Math.Min(datos.Count + 1, 500));
        }

        using var salida = new MemoryStream();
        libro.SaveAs(salida);
        return salida.ToArray();
    }

    internal static XLCellValue ValorCelda(object valor)
    {
        switch (valor)
        {
            case null:
                return Blank.Value;
            case Enum enumerado:
                return enumerado.ToString();
            case DateTimeOffset fecha:
                return fecha.DateTime;
            case TimeSpan tiempo:
                return tiempo.ToString();
            case string or bool or DateTime or byte or short or int or long or float or double or decimal or sbyte or ushort or uint or ulong:
                return XLCellValue.FromObject(valor, CultureInfo.CurrentCulture);
            default:
                return valor.ToString();
        }
    }

    /// <summary>
    /// Acceso a la propiedad (admite rutas "A.B") por reflexión.
    /// </summary>
    private static Func<object, object> CrearAcceso(Type tipo, string ruta)
    {
        var propiedades = new List<PropertyInfo>();
        var actual = tipo;
        foreach (var nombre in ruta.Split('.'))
        {
            var propiedad = actual?.GetProperty(nombre, BindingFlags.Public | BindingFlags.Instance);
            if (propiedad == null)
            {
                return _ => null;
            }
            propiedades.Add(propiedad);
            actual = propiedad.PropertyType;
        }

        return item =>
        {
            object valor = item;
            foreach (var propiedad in propiedades)
            {
                if (valor == null)
                {
                    return null;
                }
                valor = propiedad.GetValue(valor);
            }
            return valor;
        };
    }

    /// <summary>
    /// Traduce el FormatString de Radzen ("{0:c2}", "{0:n0}", "{0:dd/MM/yyyy}", …) a formato numérico de Excel.
    /// </summary>
    private static string FormatoExcel(string formatString)
    {
        if (string.IsNullOrWhiteSpace(formatString))
        {
            return null;
        }

        var m = Regex.Match(formatString, @"\{0:([^}]+)\}");
        var formato = m.Success ? m.Groups[1].Value : formatString;

        var estandar = Regex.Match(formato, @"^([cCnNpPfF])(\d*)$");
        if (estandar.Success)
        {
            var decimales = estandar.Groups[2].Value == "" ? 2 : int.Parse(estandar.Groups[2].Value);
            var parteDecimal = decimales > 0 ? "." + new string('0', decimales) : "";
            switch (char.ToLower(estandar.Groups[1].Value[0]))
            {
                case 'c':
                    return "$#,##0" + parteDecimal;
                case 'n':
                    return "#,##0" + parteDecimal;
                case 'f':
                    return "0" + parteDecimal;
                case 'p':
                    return "0" + parteDecimal + "%";
            }
        }

        if (formato == "d")
        {
            return "dd/mm/yyyy";
        }
        if (formato == "g")
        {
            return "dd/mm/yyyy hh:mm";
        }

        // Formatos personalizados (fechas "dd/MM/yyyy HH:mm", ceros "000000000", etc.)
        return formato.Replace("HH", "hh");
    }
}


/// <summary>
/// Columna a exportar: título, propiedad y formato.
/// </summary>
public record ColumnaExportacion(string Titulo, string Propiedad, string FormatString);
