using System.Text.Json;

namespace TecnoMarket.Web.Servicios;

/// <summary>Carrito local guardado en la sesión de ASP.NET (sin tablas redundantes).</summary>
public static class CarritoSesion
{
    private const string Clave = "tm_carrito";

    public static List<ItemCarritoWeb> Obtener(this ISession sesion)
    {
        var json = sesion.GetString(Clave);
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<ItemCarritoWeb>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void Guardar(this ISession sesion, List<ItemCarritoWeb> items) =>
        sesion.SetString(Clave, JsonSerializer.Serialize(items));

    public static int Cantidad(this ISession sesion) =>
        sesion.Obtener().Sum(x => x.Cantidad);
}
