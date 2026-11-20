using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace TecnoMarket.Api.Seguridad;

public sealed record SesionInfo(string Id, string Email, bool EsAdmin);

/// <summary>
/// Valida el JWT de Supabase preguntándole al propio Supabase (/auth/v1/user).
/// Así la API no necesita manipular claves privadas ni JWKS.
/// </summary>
public sealed class SesionSupabase
{
    private readonly IHttpClientFactory _factoria;
    private readonly IMemoryCache _cache;
    private readonly string _adminEmail;

    public SesionSupabase(IHttpClientFactory factoria, IMemoryCache cache, IConfiguration configuracion)
    {
        _factoria = factoria;
        _cache = cache;
        _adminEmail = configuracion["Admin:Email"] ?? "rr4439210@gmail.com";
    }

    public async Task<SesionInfo?> ValidarAsync(string? autorizacion)
    {
        if (string.IsNullOrWhiteSpace(autorizacion)) return null;
        if (!autorizacion.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return null;

        var token = autorizacion[7..].Trim();
        if (token.Length == 0) return null;
        if (_cache.TryGetValue(token, out SesionInfo? guardada) && guardada is not null) return guardada;

        try
        {
            var cliente = _factoria.CreateClient("supabase");
            using var peticion = new HttpRequestMessage(HttpMethod.Get, "/auth/v1/user");
            peticion.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            using var respuesta = await cliente.SendAsync(peticion);
            if (!respuesta.IsSuccessStatusCode) return null;

            using var documento = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
            var raiz = documento.RootElement;
            var id = raiz.TryGetProperty("id", out var nodoId) ? nodoId.GetString() ?? "" : "";
            var email = raiz.TryGetProperty("email", out var nodoEmail) ? nodoEmail.GetString() ?? "" : "";

            var rol = "";
            if (raiz.TryGetProperty("user_metadata", out var meta) &&
                meta.ValueKind == JsonValueKind.Object &&
                meta.TryGetProperty("rol", out var nodoRol))
            {
                rol = nodoRol.GetString() ?? "";
            }

            var esAdmin = rol.Equals("administrador", StringComparison.OrdinalIgnoreCase) ||
                          email.Equals(_adminEmail, StringComparison.OrdinalIgnoreCase);

            var sesion = new SesionInfo(id, email, esAdmin);
            _cache.Set(token, sesion, TimeSpan.FromMinutes(5));
            return sesion;
        }
        catch
        {
            return null;
        }
    }
}
