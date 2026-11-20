using Microsoft.AspNetCore.Mvc;
using TecnoMarket.Api.Data;
using TecnoMarket.Api.Repositorios;
using TecnoMarket.Api.Seguridad;
using TecnoMarket.Api.Servicios;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Controllers;

/// <summary>
/// Rutas compatibles con PostgREST (/rest/v1/...) para que la app Android
/// y la página web consuman exactamente la misma API C#.
/// </summary>
[ApiController]
[Route("rest/v1")]
public sealed class RestController : ControllerBase
{
    private readonly CatalogoRepositorio _catalogo;
    private readonly UsuarioRepositorio _usuarios;
    private readonly FavoritoRepositorio _favoritos;
    private readonly PedidoRepositorio _pedidos;
    private readonly SupabaseDb _db;
    private readonly SupabaseRest _proxy;

    public RestController(
        CatalogoRepositorio catalogo,
        UsuarioRepositorio usuarios,
        FavoritoRepositorio favoritos,
        PedidoRepositorio pedidos,
        SupabaseDb db,
        SupabaseRest proxy)
    {
        _catalogo = catalogo;
        _usuarios = usuarios;
        _favoritos = favoritos;
        _pedidos = pedidos;
        _db = db;
        _proxy = proxy;
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias()
    {
        return Ok(await _catalogo.CategoriasAsync());
    }

    /// <summary>
    /// Vistas de agregación del data warehouse (vw_dw_*). Se reenvían tal cual
    /// a PostgREST; el middleware ya las marcó como de solo lectura pública.
    /// </summary>
    [HttpGet("vw_dw_{nombre}")]
    public Task<IActionResult> DataWarehouse(string nombre) => _proxy.ProxyAsync(Request);

    [HttpGet("usuarios")]
    public async Task<IActionResult> Usuarios()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        return Ok(await _usuarios.ListarAsync());
    }

    [HttpGet("usuarios/{id}")]
    public async Task<IActionResult> Usuario(string id)
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var usuario = await _usuarios.PorIdAsync(id);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpGet("productos")]
    public async Task<IActionResult> Productos()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var query = Request.QueryString.Value ?? "";
        return Ok(await _catalogo.ProductosPorQueryAsync(query.TrimStart('?')));
    }

    [HttpPost("productos")]
    public async Task<IActionResult> CrearProducto([FromBody] Producto producto)
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        if (!sesion.EsAdmin && producto.VendedorId != sesion.Id)
            return StatusCode(403, new { mensaje = "No tienes permiso para esta acción." });

        var creado = await _catalogo.CrearYDevolverAsync(producto);
        return creado is null ? BadRequest(new { mensaje = "No se pudo publicar el producto." })
                              : StatusCode(201, new[] { creado });
    }

    [HttpPatch("productos")]
    public async Task<IActionResult> ActualizarProducto([FromBody] ProductoPatch patch)
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var id = IdDeQuery();
        if (id is null) return BadRequest(new { mensaje = "Falta el id del producto." });

        var sesion = Sesion()!;
        if (!sesion.EsAdmin)
        {
            var permitido = patch.Estado == "Vendido" && await _pedidos.ExisteAsync(sesion.Id, id);
            if (!permitido)
                return StatusCode(403, new { mensaje = "Solo el administrador puede cambiar el estado." });
        }

        return await _catalogo.ActualizarAsync(id, patch) ? Ok() : NotFound();
    }

    [HttpDelete("productos")]
    public async Task<IActionResult> BorrarProducto()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var id = IdDeQuery();
        if (id is null) return BadRequest(new { mensaje = "Falta el id del producto." });
        return await _catalogo.EliminarAsync(id) ? Ok() : NotFound();
    }

    [HttpGet("favoritos")]
    public async Task<IActionResult> Favoritos()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        return Ok(await _favoritos.ListarAsync(sesion.Id));
    }

    [HttpPost("favoritos")]
    public async Task<IActionResult> CrearFavorito([FromBody] NuevoFavorito favorito)
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        var id = await _favoritos.CrearAsync(sesion.Id, favorito.ProductoId);
        return id is null ? BadRequest() : StatusCode(201);
    }

    [HttpDelete("favoritos")]
    public async Task<IActionResult> BorrarFavorito()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        var idFavorito = IdDeQuery();
        if (idFavorito is null) return BadRequest();

        var mios = await _favoritos.ListarAsync(sesion.Id);
        if (!mios.Any(f => f.Id == idFavorito))
            return StatusCode(403, new { mensaje = "Ese favorito no es tuyo." });
        return await _favoritos.EliminarAsync(idFavorito) ? Ok() : NotFound();
    }

    [HttpGet("pedidos")]
    public async Task<IActionResult> Pedidos()
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        return Ok(await _pedidos.ListarAsync(sesion.EsAdmin ? null : sesion.Id));
    }

    [HttpPost("pedidos")]
    public async Task<IActionResult> CrearPedido([FromBody] Pedido pedido)
    {
        if (!_db.Configurada) return await _proxy.ProxyAsync(Request);
        var sesion = Sesion()!;
        pedido.UsuarioId = sesion.Id;
        return await _pedidos.CrearAsync(pedido) ? StatusCode(201) : BadRequest();
    }

    // ---------- Chat vendedor <-> cliente ----------
    // Se reenvía tal cual a la misma BD de Supabase. El middleware exige
    // sesión y las políticas RLS garantizan que solo lean/escriban los
    // participantes de la conversación.

    [HttpGet("mensajes")]
    public Task<IActionResult> Mensajes() => _proxy.ProxyAsync(Request);

    [HttpPost("mensajes")]
    public Task<IActionResult> CrearMensaje() => _proxy.ProxyAsync(Request);

    [HttpPatch("mensajes")]
    public Task<IActionResult> ActualizarMensaje() => _proxy.ProxyAsync(Request);

    private SesionInfo? Sesion() => HttpContext.Items.TryGetValue("sesion", out var valor)
        ? valor as SesionInfo
        : null;

    private string? IdDeQuery()
    {
        var valor = Request.Query["id"].ToString();
        return valor.StartsWith("eq.", StringComparison.Ordinal) ? valor[3..] : null;
    }
}
