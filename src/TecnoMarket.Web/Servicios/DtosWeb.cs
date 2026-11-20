using System.Text.Json.Serialization;

namespace TecnoMarket.Web.Servicios;

/// <summary>Campos exactos que Supabase acepta al publicar (igual que Android).</summary>
public sealed class NuevoProductoWeb
{
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
    [JsonPropertyName("precio_cordobas")] public decimal PrecioCordobas { get; set; }
    [JsonPropertyName("categoria_id")] public int CategoriaId { get; set; }
    [JsonPropertyName("condicion")] public string Condicion { get; set; } = "Nuevo";
    [JsonPropertyName("modalidad_entrega")] public string ModalidadEntrega { get; set; } = "Retiro en Masaya";
    [JsonPropertyName("estado")] public string Estado { get; set; } = "Pendiente";
    [JsonPropertyName("vendedor_id")] public string VendedorId { get; set; } = "";
    [JsonPropertyName("imagen_url")] public string? ImagenUrl { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; } = 1;
}

public sealed class NuevoPedidoWeb
{
    [JsonPropertyName("usuario_id")] public string UsuarioId { get; set; } = "";
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
    [JsonPropertyName("producto_titulo")] public string ProductoTitulo { get; set; } = "";
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; } = 1;
    [JsonPropertyName("precio_unitario_cordobas")] public decimal PrecioUnitarioCordobas { get; set; }
    [JsonPropertyName("total_cordobas")] public decimal TotalCordobas { get; set; }
    [JsonPropertyName("estado")] public string Estado { get; set; } = "Confirmado";
    [JsonPropertyName("modalidad_entrega")] public string ModalidadEntrega { get; set; } = "";
}

/// <summary>Ítem del carrito local (no crea tablas en Supabase, igual que Android).</summary>
public sealed class ItemCarritoWeb
{
    public string ProductoId { get; set; } = "";
    public int Cantidad { get; set; } = 1;
}

/// <summary>Mensaje del chat vendedor/cliente (tabla public.mensajes).</summary>
public sealed class MensajeWeb
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
    [JsonPropertyName("comprador_id")] public string CompradorId { get; set; } = "";
    [JsonPropertyName("vendedor_id")] public string VendedorId { get; set; } = "";
    [JsonPropertyName("remitente_id")] public string RemitenteId { get; set; } = "";
    [JsonPropertyName("contenido")] public string Contenido { get; set; } = "";
    [JsonPropertyName("leido")] public bool Leido { get; set; }
    [JsonPropertyName("fecha_envio")] public string FechaEnvio { get; set; } = "";
}

public sealed class NuevoMensajeWeb
{
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
    [JsonPropertyName("comprador_id")] public string CompradorId { get; set; } = "";
    [JsonPropertyName("vendedor_id")] public string VendedorId { get; set; } = "";
    [JsonPropertyName("remitente_id")] public string RemitenteId { get; set; } = "";
    [JsonPropertyName("contenido")] public string Contenido { get; set; } = "";
}

public sealed class MensajeLeidoPatch
{
    [JsonPropertyName("leido")] public bool Leido { get; set; } = true;
}
