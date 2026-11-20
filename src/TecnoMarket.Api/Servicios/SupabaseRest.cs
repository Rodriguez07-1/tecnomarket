using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TecnoMarket.Api.Servicios;

/// <summary>
/// Acceso a Supabase REST/Auth/Storage con la clave publishable y el JWT
/// del usuario. Se usa como respaldo mientras no se configure la contraseña
/// de PostgreSQL (Npgsql).
/// </summary>
public sealed class SupabaseRest
{
    private readonly IHttpClientFactory _factoria;
    private readonly IHttpContextAccessor _contexto;

    public SupabaseRest(IHttpClientFactory factoria, IHttpContextAccessor contexto)
    {
        _factoria = factoria;
        _contexto = contexto;
    }

    public async Task<List<T>> ListarAsync<T>(string ruta)
    {
        var cliente = _factoria.CreateClient("supabase");
        using var peticion = new HttpRequestMessage(HttpMethod.Get, "/rest/v1/" + ruta);
        var autorizacion = _contexto.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(autorizacion))
            peticion.Headers.TryAddWithoutValidation("Authorization", autorizacion);
        using var respuesta = await cliente.SendAsync(peticion);
        if (!respuesta.IsSuccessStatusCode) return new List<T>();
        var json = await respuesta.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
    }

    public async Task<T?> UnoAsync<T>(string ruta)
    {
        var lista = await ListarAsync<T>(ruta);
        return lista.FirstOrDefault();
    }

    /// <summary>Reenvía la petición tal cual a Supabase (rutas /rest/v1/...).</summary>
    public async Task<IActionResult> ProxyAsync(HttpRequest peticion)
    {
        var cliente = _factoria.CreateClient("supabase");
        var ruta = peticion.Path.Value ?? "";
        while (ruta.StartsWith("//", StringComparison.Ordinal)) ruta = ruta[1..];
        if (ruta.StartsWith("/rest/v1", StringComparison.Ordinal)) ruta = ruta[8..];

        using var mensaje = new HttpRequestMessage(new HttpMethod(peticion.Method), "/rest/v1" + ruta + peticion.QueryString.Value);

        foreach (var cabecera in new[] { "apikey", "Authorization", "Prefer", "Accept" })
        {
            if (peticion.Headers.TryGetValue(cabecera, out var valores))
                mensaje.Headers.TryAddWithoutValidation(cabecera, valores.ToArray());
        }

        peticion.EnableBuffering();
        peticion.Body.Position = 0;
        using var memoria = new MemoryStream();
        await peticion.Body.CopyToAsync(memoria);
        peticion.Body.Position = 0;

        if (memoria.Length > 0)
        {
            mensaje.Content = new ByteArrayContent(memoria.ToArray());
            if (!string.IsNullOrWhiteSpace(peticion.ContentType))
                mensaje.Content.Headers.TryAddWithoutValidation("Content-Type", peticion.ContentType);
        }

        using var respuesta = await cliente.SendAsync(mensaje);
        var contenido = await respuesta.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)respuesta.StatusCode,
            Content = contenido,
            ContentType = respuesta.Content.Headers.ContentType?.ToString() ?? "application/json"
        };
    }
}
