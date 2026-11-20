using System.Text.Json.Serialization;

namespace TecnoMarket.Web.Servicios;

public sealed record ResumenDw
{
    [JsonPropertyName("ventas_totales")]
    public decimal VentasTotales { get; init; }

    [JsonPropertyName("total_pedidos")]
    public int TotalPedidos { get; init; }

    [JsonPropertyName("total_favoritos")]
    public int TotalFavoritos { get; init; }

    [JsonPropertyName("total_publicaciones")]
    public int TotalPublicaciones { get; init; }

    [JsonPropertyName("total_usuarios")]
    public int TotalUsuarios { get; init; }

    [JsonPropertyName("total_productos")]
    public int TotalProductos { get; init; }

    [JsonPropertyName("ultima_carga")]
    public DateTime? UltimaCarga { get; init; }
}

public sealed record VentaCategoria
{
    [JsonPropertyName("categoria")]
    public string Categoria { get; init; } = "";

    [JsonPropertyName("total_cordobas")]
    public decimal TotalCordobas { get; init; }

    [JsonPropertyName("pedidos")]
    public int Pedidos { get; init; }

    [JsonPropertyName("unidades")]
    public int Unidades { get; init; }
}

public sealed record ProductoPopular
{
    [JsonPropertyName("titulo")]
    public string Titulo { get; init; } = "";

    [JsonPropertyName("categoria")]
    public string Categoria { get; init; } = "";

    [JsonPropertyName("favoritos")]
    public int Favoritos { get; init; }

    [JsonPropertyName("pedidos")]
    public int Pedidos { get; init; }

    [JsonPropertyName("total_cordobas")]
    public decimal TotalCordobas { get; init; }
}

public sealed record VentaMes
{
    [JsonPropertyName("anio")]
    public int Anio { get; init; }

    [JsonPropertyName("mes")]
    public int Mes { get; init; }

    [JsonPropertyName("nombre_mes")]
    public string NombreMes { get; init; } = "";

    [JsonPropertyName("total_cordobas")]
    public decimal TotalCordobas { get; init; }

    [JsonPropertyName("pedidos")]
    public int Pedidos { get; init; }
}

public sealed record EtlBitacora
{
    [JsonPropertyName("inicio")]
    public DateTime Inicio { get; init; }

    [JsonPropertyName("fin")]
    public DateTime Fin { get; init; }

    [JsonPropertyName("pedidos")]
    public int Pedidos { get; init; }

    [JsonPropertyName("favoritos")]
    public int Favoritos { get; init; }

    [JsonPropertyName("publicaciones")]
    public int Publicaciones { get; init; }

    [JsonPropertyName("usuarios")]
    public int Usuarios { get; init; }

    [JsonPropertyName("estado")]
    public string Estado { get; init; } = "";
}
