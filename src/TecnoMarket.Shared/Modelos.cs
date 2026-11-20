using System.Text.Json.Serialization;

namespace TecnoMarket.Shared;

public sealed class Categoria
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("descripcion")] public string? Descripcion { get; set; }
    [JsonPropertyName("icono")] public string? Icono { get; set; }
}

public sealed class Producto
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("titulo")] public string Titulo { get; set; } = "";
    [JsonPropertyName("descripcion")] public string Descripcion { get; set; } = "";
    [JsonPropertyName("precio_cordobas")] public decimal PrecioCordobas { get; set; }
    [JsonPropertyName("categoria_id")] public int CategoriaId { get; set; }
    [JsonPropertyName("condicion")] public string Condicion { get; set; } = "Nuevo";
    [JsonPropertyName("modalidad_entrega")] public string ModalidadEntrega { get; set; } = "";
    [JsonPropertyName("estado")] public string Estado { get; set; } = "Pendiente";
    [JsonPropertyName("vendedor_id")] public string VendedorId { get; set; } = "";
    [JsonPropertyName("fecha_publicacion")] public string? FechaPublicacion { get; set; }
    [JsonPropertyName("fecha_actualizacion")] public string? FechaActualizacion { get; set; }
    [JsonPropertyName("imagen_url")] public string? ImagenUrl { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; } = 1;
}

public sealed class Usuario
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("apellido")] public string Apellido { get; set; } = "";
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("telefono")] public string? Telefono { get; set; }
    [JsonPropertyName("ciudad")] public string? Ciudad { get; set; }
    [JsonPropertyName("fecha_registro")] public string? FechaRegistro { get; set; }
    [JsonPropertyName("rol")] public string Rol { get; set; } = "usuario";
}

public sealed class ProductoItem
{
    [JsonPropertyName("producto")] public Producto Producto { get; set; } = new();
    [JsonPropertyName("categoria_nombre")] public string CategoriaNombre { get; set; } = "";
    [JsonPropertyName("vendedor_nombre")] public string VendedorNombre { get; set; } = "";
}

public sealed class Pedido
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("usuario_id")] public string UsuarioId { get; set; } = "";
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
    [JsonPropertyName("producto_titulo")] public string ProductoTitulo { get; set; } = "";
    [JsonPropertyName("cantidad")] public int Cantidad { get; set; }
    [JsonPropertyName("precio_unitario_cordobas")] public decimal PrecioUnitarioCordobas { get; set; }
    [JsonPropertyName("total_cordobas")] public decimal TotalCordobas { get; set; }
    [JsonPropertyName("estado")] public string Estado { get; set; } = "Confirmado";
    [JsonPropertyName("modalidad_entrega")] public string? ModalidadEntrega { get; set; }
    [JsonPropertyName("fecha_pedido")] public string? FechaPedido { get; set; }
}

public sealed class CambiarEstadoRequest
{
    [JsonPropertyName("estado")] public string Estado { get; set; } = "";
}

public sealed class ProductoPatch
{
    [JsonPropertyName("estado")] public string? Estado { get; set; }
    [JsonPropertyName("stock")] public int? Stock { get; set; }
}

public sealed class Favorito
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("usuario_id")] public string UsuarioId { get; set; } = "";
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
    [JsonPropertyName("fecha_guardado")] public string? FechaGuardado { get; set; }
}

public sealed class NuevoFavorito
{
    [JsonPropertyName("usuario_id")] public string UsuarioId { get; set; } = "";
    [JsonPropertyName("producto_id")] public string ProductoId { get; set; } = "";
}

public sealed class RegistrarUsuarioRequest
{
    [JsonPropertyName("p_id")] public string Id { get; set; } = "";
    [JsonPropertyName("p_nombre")] public string Nombre { get; set; } = "";
    [JsonPropertyName("p_apellido")] public string Apellido { get; set; } = "";
    [JsonPropertyName("p_email")] public string Email { get; set; } = "";
    [JsonPropertyName("p_telefono")] public string Telefono { get; set; } = "";
    [JsonPropertyName("p_ciudad")] public string Ciudad { get; set; } = "";
}

public sealed class HealthResult
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("mensaje")] public string Mensaje { get; set; } = "";
}
