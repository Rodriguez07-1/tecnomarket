using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ApiCliente _api;

    public IndexModel(ApiCliente api) => _api = api;

    public List<ProductoItem> Productos { get; private set; } = new();
    public List<Categoria> Categorias { get; private set; } = new();
    public string? Error { get; private set; }

    [BindProperty(SupportsGet = true)] public int? CategoriaId { get; set; }
    [BindProperty(SupportsGet = true)] public string? Busqueda { get; set; }

    public async Task OnGetAsync()
    {
        Categorias = await _api.CategoriasAsync();
        Productos = await _api.ProductosAsync(CategoriaId, Busqueda);
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
        return RedirectToPage(new { CategoriaId, Busqueda });
    }
}
