using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class PublicarModel : PageModel
{
    private readonly ApiCliente _api;
    public PublicarModel(ApiCliente api) => _api = api;

    public List<Categoria> Categorias { get; private set; } = new();
    public List<Producto> MisProductos { get; private set; } = new();
    public string? Error { get; private set; }

    [BindProperty] public string Titulo { get; set; } = "";
    [BindProperty] public string Descripcion { get; set; } = "";
    [BindProperty] public string Precio { get; set; } = "";
    [BindProperty] public string Stock { get; set; } = "1";
    [BindProperty] public int CategoriaId { get; set; }
    [BindProperty] public string Condicion { get; set; } = "Nuevo";
    [BindProperty] public string ModalidadEntrega { get; set; } = "Retiro en Masaya";
    [BindProperty] public IFormFile? Imagen { get; set; }

    public static readonly string[] Condiciones = { "Nuevo", "Open Box", "Reacondicionado" };
    public static readonly string[] Entregas = { "Retiro en Masaya", "Delivery Managua" };

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");
        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Categorias = await _api.CategoriasAsync();
        MisProductos = await _api.ProductosPorVendedorAsync(_api.Uid ?? "");
        Error = _api.UltimoError;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");

        if (string.IsNullOrWhiteSpace(Titulo) || string.IsNullOrWhiteSpace(Descripcion) ||
            !decimal.TryParse(Precio, out var precio) || precio <= 0 ||
            !int.TryParse(Stock, out var stock) || stock < 1 || CategoriaId <= 0)
        {
            TempData["Error"] = "Completá título, descripción, precio, stock y categoría.";
            await CargarAsync();
            return Page();
        }

        string? urlFoto = null;
        if (Imagen is { Length: > 0 })
        {
            if (Imagen.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "La foto debe pesar menos de 10 MB.";
                await CargarAsync();
                return Page();
            }
            urlFoto = await _api.SubirImagenAsync(_api.Uid!, Imagen);
            if (urlFoto is null)
            {
                TempData["Error"] = _api.UltimoError ?? "No se pudo subir la foto.";
                await CargarAsync();
                return Page();
            }
        }

        var creado = await _api.PublicarProductoAsync(new NuevoProductoWeb
        {
            Titulo = Titulo.Trim(),
            Descripcion = Descripcion.Trim(),
            PrecioCordobas = precio,
            CategoriaId = CategoriaId,
            Condicion = Condicion,
            ModalidadEntrega = ModalidadEntrega,
            Estado = "Pendiente",
            VendedorId = _api.Uid!,
            ImagenUrl = urlFoto,
            Stock = stock
        });

        if (creado is null)
        {
            TempData["Error"] = _api.UltimoError ?? "No se pudo publicar el producto.";
        }
        else
        {
            TempData["Mensaje"] = "Producto enviado. Queda en estado Pendiente hasta que el administrador lo apruebe.";
            Titulo = Descripcion = Precio = "";
            Stock = "1";
            Imagen = null;
        }

        await CargarAsync();
        return Page();
    }
}
