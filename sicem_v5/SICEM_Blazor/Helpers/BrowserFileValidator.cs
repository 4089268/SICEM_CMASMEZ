using System;
using System.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace Sicem_Blazor.Helpers;

public class BrowserFileValidator
{
    public static bool IsCsvFile(IBrowserFile file)
    {
        // Check MIME type (text/csv is the standard for CSV files)
        var validContentTypes = new string[] { "application/vnd.ms-excel", "text/csv" };
        if(!validContentTypes.Contains(file.ContentType))
        {
            return false;
        }

        // Check file extension
        if(!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
    
    public static bool IsXLSFile(IBrowserFile file)
    {
        // Check MIME type (text/csv is the standard for CSV files)
        var validContentTypes = new string[] {
            "application/vnd.ms-excel", // .xls
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", // .xlsx
            "application/octet-stream", // some browsers fallback
            "application/x-excel" // legacy
        };
        if(!validContentTypes.Contains(file.ContentType))
        {
            return false;
        }

        // Check file extension
        var validExtensions = new[] { ".xls", ".xlsx" };
        if (!validExtensions.Any(ext => file.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }
}
