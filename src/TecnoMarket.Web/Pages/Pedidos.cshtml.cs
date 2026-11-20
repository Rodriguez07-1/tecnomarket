using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class PedidosModel : PageModel
{
    private readonly ApiCliente _api;
    public PedidosModel(ApiCliente api) => _api = api;

    public List<Pedido> Pedidos { get; private set; } = new();
    public string? Error { get; private set; }
    public bool EsAdmin => _api.EsAdmin;

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_api.SesionIniciada) return RedirectToPage("/Login");
        Pedidos = await _api.PedidosAsync();
        Error = _api.UltimoError;
        return Page();
    }
}
