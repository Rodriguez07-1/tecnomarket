using Microsoft.AspNetCore.Mvc;
using TecnoMarket.Api.Repositorios;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
public sealed class CatalogoController : ControllerBase
{
    private readonly CatalogoRepositorio _repo;

    public CatalogoController(CatalogoRepositorio repo) => _repo = repo;

    [HttpGet("categorias")]
    public async Task<IActionResult> Categorias()
    {
        try { return Ok(await _repo.CategoriasAsync()); }
        catch (Exception e) { return Error(e); }
    }

    [HttpGet("productos")]
    public async Task<IActionResult> Productos(
        [FromQuery] int? categoriaId = null,
        [FromQuery] string? busqueda = null,
        [FromQuery] bool soloDisponibles = false,
        [FromQuery] bool incluirPendientes = false)
    {
        try
        {
            return Ok(await _repo.ProductosAsync(categoriaId, busqueda, soloDisponibles, incluirPendientes));
        }
        catch (Exception e) { return Error(e); }
    }

    [HttpGet("productos/{id}")]
    public async Task<IActionResult> Producto(string id)
    {
        try
        {
            var item = await _repo.ProductoPorIdAsync(id);
            return item is null ? NotFound() : Ok(item);
        }
        catch (Exception e) { return Error(e); }
    }

    [HttpPost("productos")]
    public async Task<IActionResult> Crear([FromBody] Producto producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Titulo) || producto.PrecioCordobas <= 0 || producto.CategoriaId <= 0)
            return BadRequest(new { mensaje = "Título, precio y categoría son obligatorios." });

        try
        {
            var creado = await _repo.CrearAsync(producto);
            return creado ? StatusCode(201, new { mensaje = "Producto creado en estado Pendiente." }) : BadRequest();
        }
        catch (Exception e) { return Error(e); }
    }

    [HttpPatch("productos/{id}/estado")]
    public async Task<IActionResult> CambiarEstado(string id, [FromBody] CambiarEstadoRequest cuerpo)
    {
        try
        {
            var ok = await _repo.CambiarEstadoAsync(id, cuerpo.Estado);
            return ok ? Ok(new { mensaje = "Estado actualizado." }) : BadRequest(new { mensaje = "Estado no válido." });
        }
        catch (Exception e) { return Error(e); }
    }

    [HttpDelete("productos/{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        try
        {
            return await _repo.EliminarAsync(id) ? Ok(new { mensaje = "Producto eliminado." }) : NotFound();
        }
        catch (Exception e) { return Error(e); }
    }

    private IActionResult Error(Exception e) => StatusCode(503, new { mensaje = e.Message });
}
