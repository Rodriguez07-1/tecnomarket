using Microsoft.AspNetCore.Mvc;
using TecnoMarket.Api.Repositorios;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
public sealed class PedidosController : ControllerBase
{
    private readonly PedidoRepositorio _repo;

    public PedidosController(PedidoRepositorio repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? usuarioId = null)
    {
        try { return Ok(await _repo.ListarAsync(usuarioId)); }
        catch (Exception e) { return StatusCode(503, new { mensaje = e.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Pedido pedido)
    {
        if (string.IsNullOrWhiteSpace(pedido.UsuarioId) || string.IsNullOrWhiteSpace(pedido.ProductoId) || pedido.TotalCordobas <= 0)
            return BadRequest(new { mensaje = "Datos del pedido incompletos." });

        try
        {
            return await _repo.CrearAsync(pedido)
                ? StatusCode(201, new { mensaje = "Pedido registrado." })
                : BadRequest();
        }
        catch (Exception e) { return StatusCode(503, new { mensaje = e.Message }); }
    }
}
