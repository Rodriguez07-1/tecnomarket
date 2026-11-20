using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class LoginModel : PageModel
{
    private readonly ApiCliente _api;
    private readonly IConfiguration _configuracion;

    public LoginModel(ApiCliente api, IConfiguration configuracion)
    {
        _api = api;
        _configuracion = configuracion;
    }

    [BindProperty] public string Email { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var resultado = await _api.IniciarSesionAsync(Email.Trim(), Password);
        if (resultado is null)
        {
            Error = _api.UltimoError ?? "No se pudo iniciar sesión.";
            return Page();
        }

        var opciones = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        var correoAdmin = _configuracion["Admin:Email"] ?? "rr4439210@gmail.com";
        Response.Cookies.Append("tm_token", resultado.Token, opciones);
        Response.Cookies.Append("tm_email", resultado.Email, opciones);
        Response.Cookies.Append("tm_uid", resultado.Id, opciones);
        Response.Cookies.Append("tm_admin",
            resultado.Email.Equals(correoAdmin, StringComparison.OrdinalIgnoreCase) ? "1" : "0", opciones);

        return RedirectToPage("/Index");
    }

    public async Task<IActionResult> OnPostSalirAsync()
    {
        await _api.CerrarSesionAsync();
        foreach (var nombre in new[] { "tm_token", "tm_email", "tm_uid", "tm_admin" })
            Response.Cookies.Delete(nombre);
        return RedirectToPage("/Index");
    }
}
