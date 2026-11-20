using Microsoft.AspNetCore.Mvc;

namespace TecnoMarket.Api.Controllers;

/// <summary>
/// Puente hacia Supabase Auth. La app NO habla con Supabase directo:
/// todas las peticiones (incluido el login) pasan por esta API.
/// </summary>
[ApiController]
[Route("api/auth")]
[Route("auth/v1")]
public sealed class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _factoria;

    public AuthController(IHttpClientFactory factoria) => _factoria = factoria;

    [HttpPost("login")]
    public Task<IActionResult> Login() =>
        ProxyAsync("/auth/v1/token?grant_type=password", reenviarToken: false);

    [HttpPost("token")]
    public Task<IActionResult> Token()
    {
        var grant = Request.Query["grant_type"].ToString();
        var destino = grant == "refresh_token" ? "refresh_token" : "password";
        return ProxyAsync("/auth/v1/token?grant_type=" + destino, reenviarToken: false);
    }

    [HttpPost("signup")]
    public Task<IActionResult> SignUp() =>
        ProxyAsync("/auth/v1/signup", reenviarToken: false);

    [HttpPost("refresh")]
    public Task<IActionResult> Refresh() =>
        ProxyAsync("/auth/v1/token?grant_type=refresh_token", reenviarToken: false);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var cliente = _factoria.CreateClient("supabase");
        var token = Request.Headers.Authorization.ToString();
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/auth/v1/logout");
        if (!string.IsNullOrWhiteSpace(token)) peticion.Headers.TryAddWithoutValidation("Authorization", token);
        using var respuesta = await cliente.SendAsync(peticion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        return Json((int)respuesta.StatusCode, cuerpo);
    }

    [HttpPost("registrar-usuario")]
    [HttpPost("~/rest/v1/rpc/registrar_usuario")]
    public Task<IActionResult> RegistrarUsuario() =>
        ProxyAsync("/rest/v1/rpc/registrar_usuario", reenviarToken: false);

    /// <summary>
    /// Dispara la recarga del data warehouse. El middleware exige rol admin.
    /// </summary>
    [HttpPost("~/rest/v1/rpc/ejecutar_etl")]
    public Task<IActionResult> EjecutarEtl() =>
        ProxyAsync("/rest/v1/rpc/ejecutar_etl", reenviarToken: true);

    private async Task<IActionResult> ProxyAsync(string ruta, bool reenviarToken)
    {
        using var lector = new StreamReader(Request.Body);
        var cuerpo = await lector.ReadToEndAsync();

        var cliente = _factoria.CreateClient("supabase");
        using var peticion = new HttpRequestMessage(HttpMethod.Post, ruta)
        {
            Content = new StringContent(cuerpo, System.Text.Encoding.UTF8, "application/json")
        };
        if (reenviarToken && Request.Headers.ContainsKey("Authorization"))
            peticion.Headers.TryAddWithoutValidation("Authorization", Request.Headers.Authorization.ToString());

        using var respuesta = await cliente.SendAsync(peticion);
        return Json((int)respuesta.StatusCode, await respuesta.Content.ReadAsStringAsync());
    }

    private static IActionResult Json(int status, string contenido) => new ContentResult
    {
        StatusCode = status,
        Content = contenido,
        ContentType = "application/json"
    };
}
