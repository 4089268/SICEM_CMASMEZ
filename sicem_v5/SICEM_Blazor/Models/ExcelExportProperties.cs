using System;

namespace Sicem_Blazor.Models;

public class ExcelExportProperties
{
    ///<summary>
    /// Nombre del archivo a descargar. Si se omite se genera uno.
    ///</summary> 
    public string FileName { get; set; }

    /// <summary>
    /// Incluir también las columnas ocultas (Visible="false").
    /// </summary>
    public bool IncludeHiddenColumn { get; set; } = false;

    ///<summary>
    /// Nombre de la hoja.
    ///</summary>
    public string SheetName { get; set; } = "Datos";
}