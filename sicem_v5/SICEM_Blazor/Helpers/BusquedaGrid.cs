using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sicem_Blazor.Helpers;

/// <summary>
/// Búsqueda de texto sobre los datos de un grid, en memoria.
/// Reemplaza a SfGrid.SearchAsync de Syncfusion, que RadzenDataGrid no tiene:
/// devuelve las filas en las que alguna propiedad simple (texto, número, fecha) contiene el texto buscado.
/// </summary>
public static class BusquedaGrid
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propiedades = new();

    public static IEnumerable<T> Filtrar<T>(IEnumerable<T> datos, string texto)
    {
        if (datos == null || string.IsNullOrWhiteSpace(texto))
        {
            return datos;
        }

        var buscado = texto.Trim();
        var propiedades = _propiedades.GetOrAdd(typeof(T), ObtenerPropiedadesSimples);

        return datos.Where(item => item != null && propiedades.Any(propiedad =>
        {
            var valor = propiedad.GetValue(item);
            return valor != null && valor.ToString().Contains(buscado, StringComparison.CurrentCultureIgnoreCase);
        })).ToList();
    }

    private static PropertyInfo[] ObtenerPropiedadesSimples(Type tipo)
    {
        return tipo.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propiedad => propiedad.CanRead && propiedad.GetIndexParameters().Length == 0 && EsSimple(propiedad.PropertyType))
            .ToArray();
    }

    private static bool EsSimple(Type tipo)
    {
        var baseType = Nullable.GetUnderlyingType(tipo) ?? tipo;
        return baseType.IsPrimitive || baseType.IsEnum || baseType == typeof(string) || baseType == typeof(decimal) || baseType == typeof(DateTime);
    }
}
