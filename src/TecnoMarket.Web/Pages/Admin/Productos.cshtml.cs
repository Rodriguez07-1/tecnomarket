using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages.Admin;

public sealed class ProductosModel : PageModel
{
    private readonly ApiCliente _api;
    public ProductosModel(ApiCliente api) => _api = api;

    public List<Producto> Productos { get; private set; } = new();
    public List<Pedido> Pedidos { get; private set; } = new();
    public List<Categoria> Categorias { get; private set; } = new();
    public string? Error { get; private set; }

    public int Pendientes => Productos.Count(p => p.Estado == "Pendiente");
    public int Disponibles => Productos.Count(p => p.Estado == "Disponible");
    public int Vendidos => Productos.Count(p => p.Estado == "Vendido");
    public decimal Ventas => Pedidos.Sum(p => p.TotalCordobas);

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Productos = await _api.ProductosTodosAsync();
        Pedidos = await _api.PedidosAsync();
        Categorias = await _api.CategoriasAsync();
        Error = _api.UltimoError;
    }

    public string NombreCategoria(int id) =>
        Categorias.FirstOrDefault(c => c.Id == id)?.Nombre ?? "Sin categoría";

    public async Task<IActionResult> OnPostAprobarAsync(string id)
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");
        if (!await _api.CambiarEstadoProductoAsync(id, "Disponible"))
            TempData["Error"] = _api.UltimoError ?? "No se pudo aprobar.";
        else
            TempData["Mensaje"] = "Publicación aprobada y visible en el catálogo.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRechazarAsync(string id)
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");
        if (!await _api.CambiarEstadoProductoAsync(id, "Rechazado"))
            TempData["Error"] = _api.UltimoError ?? "No se pudo rechazar.";
        else
            TempData["Mensaje"] = "Publicación rechazada.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBorrarAsync(string id)
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");
        if (!await _api.BorrarProductoAsync(id))
            TempData["Error"] = _api.UltimoError ?? "No se pudo borrar.";
        else
            TempData["Mensaje"] = "Producto eliminado.";
        return RedirectToPage();
    }
}
