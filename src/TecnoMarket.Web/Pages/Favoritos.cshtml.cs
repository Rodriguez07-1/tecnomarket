using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class FavoritosModel : PageModel
{
    private readonly ApiCliente _api;
    public FavoritosModel(ApiCliente api) => _api = api;

    public List<Producto> Productos { get; private set; } = new();
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");

        var favoritos = await _api.FavoritosAsync(_api.Uid!);
        Productos = await _api.ProductosPorIdsAsync(favoritos.Select(f => f.ProductoId));
        Error = _api.UltimoError;
        return Page();
    }

    public async Task<IActionResult> OnPostQuitarAsync(string id)
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");

        var favoritos = await _api.FavoritosAsync(_api.Uid!);
        var favorito = favoritos.FirstOrDefault(f => f.ProductoId == id);
        if (favorito is not null)
            await _api.BorrarFavoritoAsync(favorito.Id);
        else
            TempData["Error"] = "No se encontró ese favorito.";

        return RedirectToPage();
    }
}
