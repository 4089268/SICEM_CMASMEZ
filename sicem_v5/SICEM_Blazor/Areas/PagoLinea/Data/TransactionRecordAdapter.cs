using System;
using System.Collections.Generic;
using System.Globalization;
using Sicem_Blazor.PagoLinea.Models;
using Sicem_Blazor.Models;

namespace Sicem_Blazor.PagoLinea.Data;

public static class TransactionRecordAdapter
{

    /// <summary>
    /// Adapt the CSV row to a TransactionRecord, this is the format for 'Fortin'
    /// </summary>
    public static TransactionRecord Adapt(string fileName, string[] csvRow)
    {
        var transactionRecord = new TransactionRecord
        {
            Id = Guid.NewGuid(),
            IdPago = 0,
            Fecha = ParseDateTime(csvRow[0]),
            Servicio = csvRow[1]?.Trim() ?? string.Empty,
            Identificador = csvRow[2]?.Trim() ?? string.Empty,
            Concepto = csvRow[3]?.Trim() ?? string.Empty,
            Monto = ParseDecimal(csvRow[4]),
            Referencia = csvRow[5]?.Trim() ?? string.Empty,
            Folio = csvRow[6]?.Trim() ?? string.Empty,
            Medio = csvRow[7]?.Trim() ?? string.Empty,
            Tarjeta = csvRow[8]?.Trim() ?? string.Empty,
            Financiamiento = csvRow[9]?.Trim() ?? string.Empty,
            Autorizacion = csvRow[10]?.Trim() ?? string.Empty,
            Tarjetahabiente = csvRow[11]?.Trim() ?? string.Empty,
            Usuario = csvRow[12]?.Trim() ?? string.Empty,
            EstatusPago = ParseStatus(csvRow[13]),
            FechaCreacion = DateTime.Now,
            UltimaModificacion = DateTime.Now,
            IdPadron = 0, // Retrived fro the 'Referencia'
            Aplicado = EstatusAplicacionPagoEnum.PENDIENTE
        };

        // * Process the reference
        var (idPadron, mf, af, total) = ParseReference.Parse(transactionRecord.Referencia);

        transactionRecord.IdPadron = (int)idPadron;
        return transactionRecord;
    }


    public static Transaccion ToEntity(this TransactionRecord record)
    {
        var t = new Transaccion
        {
            Id = record.Id,
            IdPago = (int)record.IdPago,
            Fecha = record.Fecha,
            Servicio = record.Servicio,
            Identificador = record.Identificador,
            Concepto = record.Concepto,
            Monto = record.Monto,
            Referencia = record.Referencia,
            Folio = record.Folio,
            Medio = record.Medio,
            Tarjeta = record.Tarjeta,
            Financiamiento = record.Financiamiento,
            Autorizacion = record.Autorizacion,
            Tarjetahabiente = record.Tarjetahabiente,
            Usuario = record.Usuario,
            EstatusPago = record.EstatusPago.ToString(),
            CreatedDate = record.FechaCreacion,
            ModifiedDate = record.UltimaModificacion,
            IdPadron = record.IdPadron,
            Aplicado = record.Aplicado.ToString(),
            FechaAplicacion = record.FechaAplicacion,
            MensajeAplicacion = record.MensajeAplicacion,
        };
        return t;
    }

    public static TransactionRecord ToDTO(this Transaccion record)
    {
        return new TransactionRecord
        {
            Id = record.Id,
            IdPago = (int)record.IdPago,
            Fecha = record.Fecha,
            Servicio = record.Servicio,
            Identificador = record.Identificador,
            Concepto = record.Concepto,
            Monto = record.Monto,
            Referencia = record.Referencia,
            Folio = record.Folio,
            Medio = record.Medio,
            Tarjeta = record.Tarjeta,
            Financiamiento = record.Financiamiento,
            Autorizacion = record.Autorizacion,
            Tarjetahabiente = record.Tarjetahabiente,
            Usuario = record.Usuario,
            EstatusPago = ParseStatus(record.EstatusPago),
            FechaCreacion = record.CreatedDate.Value,
            UltimaModificacion = record.ModifiedDate.Value,
            IdPadron = record.IdPadron,
            Aplicado = ParseAplicacionStatus(record.Aplicado),
            FechaAplicacion = record.FechaAplicacion,
            MensajeAplicacion = record.MensajeAplicacion,
        };
    }


    private static decimal ParseDecimal(string value)
    {
        return decimal.TryParse(value.Replace("\"", "")
            .Replace(",", "")
            .Trim('$'), NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0m;
    }

    private static DateTime ParseDateTime(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return DateTime.MinValue;
        }

        // Handle different datetime formats
        if(DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }

        // Try parsing without milliseconds if needed
        if(value.Contains('.'))
        {
            string withoutMilliseconds = value.Split('.')[0];
            if(DateTime.TryParse(withoutMilliseconds, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }
        }

        throw new FormatException($"Invalid datetime format: {value}");
    }

    private static EstatusPagoEnum ParseStatus(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            return EstatusPagoEnum.PENDIENTE; // Default value for empty status
        }

        // Try exact match first
        if(Enum.TryParse<EstatusPagoEnum>(value, true, out var status))
        {
            return status;
        }

        // Handle case-insensitive matching with fallbacks
        var normalizedStatus = value.ToUpperInvariant().Trim();

        switch(normalizedStatus)
        {
            case "EXITOSO":
            case "SUCCESS":
            case "COMPLETED":
                return EstatusPagoEnum.EXITOSO;

            case "FALLIDO":
            case "FAILED":
            case "ERROR":
                return EstatusPagoEnum.FALLIDO;

            case "PENDIENTE":
            case "PENDING":
            case "PROCESSING":
                return EstatusPagoEnum.PENDIENTE;

            case "CANCELADO":
            case "CANCELLED":
            case "CANCELED":
                return EstatusPagoEnum.CANCELADO;

            default:
                throw new ArgumentException($"Invalid status value: '{value}'. Valid values are: {string.Join(", ", Enum.GetNames(typeof(EstatusPagoEnum)))}");
        }
    }
    
    private static EstatusAplicacionPagoEnum ParseAplicacionStatus(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return EstatusAplicacionPagoEnum.PENDIENTE; // Default value for empty status
        }

        // Try exact match first
        if (Enum.TryParse<EstatusAplicacionPagoEnum>(value, true, out var status))
        {
            return status;
        }

        // Handle case-insensitive matching with fallbacks
        var normalizedStatus = value.ToUpperInvariant().Trim();
        
        switch (normalizedStatus)
        {
            case "APLICADO":
            case "EXITOSO":
            case "SUCCESS":
            case "COMPLETED":
                return EstatusAplicacionPagoEnum.APLICADO;
                
            case "ERROR":
            case "FALLIDO":
            case "FAILED":
                return EstatusAplicacionPagoEnum.ERROR;
                
            case "PENDIENTE":
            case "PENDING":
            case "PROCESSING":
                return EstatusAplicacionPagoEnum.PENDIENTE;

            default:
                throw new ArgumentException($"Invalid status value: '{value}'. Valid values are: {string.Join(", ", Enum.GetNames(typeof(EstatusPagoEnum)))}");
        }
    }
}
