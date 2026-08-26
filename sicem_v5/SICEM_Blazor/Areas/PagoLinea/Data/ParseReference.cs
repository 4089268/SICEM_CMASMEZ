using System;

namespace Sicem_Blazor.PagoLinea.Data;

public class ParseReference
{
    public static (long, int, int, decimal) Parse(string reference)
    {
        if(string.IsNullOrWhiteSpace(reference) || reference.Length < 28)
        {
            throw new ArgumentException("La referencia del pago es invalida");
        }

        long IdPadron = Convert.ToInt64(reference.Substring(1, 9));
        int Mf = Convert.ToInt32(reference.Substring(9,2));
        int Af = Convert.ToInt32(reference.Substring(11,4));
        decimal total = (Convert.ToDecimal(reference.Substring(15, 12))) / 100;

        return (IdPadron, Mf, Af, total);
    }
}