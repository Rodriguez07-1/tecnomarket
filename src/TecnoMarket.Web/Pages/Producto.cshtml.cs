using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class ProductoModel : PageModel
{
    private readonly ApiCliente _api;
    public ProductoModel(ApiCliente api) => _api = api;

    public ProductoItem? Item { get; private set; }
    public string? Error { get; private set; }
    public bool EsFavorito { get; private set; }
    public bool EsVendedor => Item?.Producto.VendedorId == _api.Uid;

    public async Task OnGetAsync(string id)
    {
        Item = await _api.ProductoAsync(id);
        if (_api.SesionIniciada && !string.IsNullOrWhiteSpace(_api.Uid))
        {
            var favoritos = await _api.FavoritosAsync(_api.Uid!);
            EsFavorito = favoritos.Any(f => f.ProductoId == id);
        }
        Error = _api.UltimoError;
    }

    public async Task<IActionResult> OnPostAgregarAsync(string id)
    {
        var item = await _api.ProductoAsync(id);
        if (item is not null)
        {
            var carrito = HttpContext.Session.Obtener();
            var existente = carrito.FirstOrDefault(x => x.ProductoId == item.Producto.Id);
            var maximo = Math.Max(1, item.Producto.Stock);
            if (existente is null)
                carrito.Add(new ItemCarritoWeb { ProductoId = item.Producto.Id, Cantidad = 1 });
            else
                existente.Cantidad = Math.Min(maximo, existente.Cantidad + 1);
            HttpContext.Session.Guardar(carrito);
            TempData["Mensaje"] = "Producto agregado al carrito.";
        }
        else
        {
            TempData["Error"] = _api.UltimoError ?? "No se pudo agregar el producto.";
        }
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostFavoritoAsync(string id)
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");

        var favoritos = await _api.FavoritosAsync(_api.Uid!);
        var favorito = favoritos.FirstOrDefault(f => f.ProductoId == id);
        var ok = favorito is not null
            ? await _api.BorrarFavoritoAsync(favorito.Id)
            : await _api.AgregarFavoritoAsync(_api.Uid!, id);

        TempData[ok ? "Mensaje" : "Error"] = ok
            ? (favorito is not null ? "Quitado de favoritos." : "Guardado en favoritos.")
            : (_api.UltimoError ?? "No se pudo actualizar favoritos.");
        return RedirectToPage(new { id });
    }
}
