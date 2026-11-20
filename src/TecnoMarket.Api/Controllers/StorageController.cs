using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace TecnoMarket.Api.Controllers;

/// <summary>
/// Sube la foto al MISMO bucket de Supabase Storage, pero pasando primero
/// por la API C# (así Android y Web comparten una sola puerta de entrada).
/// </summary>
[ApiController]
[Route("storage/v1/object")]
public sealed class StorageController : ControllerBase
{
    private readonly IHttpClientFactory _factoria;
    private readonly IConfiguration _configuracion;

    public StorageController(IHttpClientFactory factoria, IConfiguration configuracion)
    {
        _factoria = factoria;
        _configuracion = configuracion;
    }

    [HttpPut("{bucket}/{*ruta}")]
    public async Task<IActionResult> Subir(string bucket, string ruta)
    {
        using var memoria = new MemoryStream();
        await Request.Body.CopyToAsync(memoria);
        var bytes = memoria.ToArray();

        if (bytes.Length == 0) return BadRequest(new { mensaje = "La imagen está vacía." });
        if (bytes.Length > 10 * 1024 * 1024)
            return BadRequest(new { mensaje = "La imagen debe pesar menos de 10 MB." });

        var cliente = _factoria.CreateClient("supabase");
        using var peticion = new HttpRequestMessage(HttpMethod.Put, $"/storage/v1/object/{bucket}/{ruta}")
        {
            Content = new ByteArrayContent(bytes)
        };
        peticion.Content.Headers.ContentType =
            new MediaTypeHeaderValue(Request.ContentType ?? "application/octet-stream");
        peticion.Headers.TryAddWithoutValidation("x-upsert", "true");
        peticion.Headers.TryAddWithoutValidation(
            "Authorization", Request.Headers.Authorization.ToString());

        using var respuesta = await cliente.SendAsync(peticion);
        if (!respuesta.IsSuccessStatusCode)
        {
            return StatusCode((int)respuesta.StatusCode,
                new { mensaje = await respuesta.Content.ReadAsStringAsync() });
        }

        var url = (_configuracion["Supabase:Url"] ?? "https://uvllrtzcatnrfopjtohs.supabase.co")
            .TrimEnd('/') + $"/storage/v1/object/public/{bucket}/{ruta}";
        return Content(url, "text/plain");
    }
}
