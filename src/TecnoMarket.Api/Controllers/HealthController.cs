using Microsoft.AspNetCore.Mvc;
using TecnoMarket.Api.Data;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly SupabaseDb _db;
    public HealthController(SupabaseDb db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        if (!_db.Configurada)
            return StatusCode(503, new HealthResult
            {
                Ok = false,
                Mensaje = "Falta la contraseña de Supabase en appsettings.json."
            });

        try
        {
            await using var cn = await _db.AbrirAsync();
            await using var cmd = cn.CreateCommand();
            cmd.CommandText = "select count(*) from public.categorias";
            var total = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return Ok(new HealthResult { Ok = true, Mensaje = $"Conectado a Supabase. Categorías: {total}" });
        }
        catch (Exception e)
        {
            return StatusCode(503, new HealthResult { Ok = false, Mensaje = e.Message });
        }
    }
}
