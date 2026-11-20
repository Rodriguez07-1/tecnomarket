using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

public sealed class CarritoModel : PageModel
{
    private readonly ApiCliente _api;
    public CarritoModel(ApiCliente api) => _api = api;

    public sealed class Fila
    {
        public Producto Producto { get; set; } = new();
        public int Cantidad { get; set; } = 1;
        public decimal Subtotal => Producto.PrecioCordobas * Cantidad;
    }

    public List<Fila> Items { get; private set; } = new();
    public decimal Total => Items.Sum(x => x.Subtotal);
    public int Articulos => Items.Sum(x => x.Cantidad);
    public string? Error { get; private set; }

    public async Task OnGetAsync() => await CargarAsync();

    private async Task CargarAsync()
    {
        var carrito = HttpContext.Session.Obtener();
        var productos = await _api.ProductosPorIdsAsync(carrito.Select(x => x.ProductoId));
        var porId = productos.ToDictionary(p => p.Id);

        Items = carrito
            .Where(x => porId.ContainsKey(x.ProductoId))
            .Select(x => new Fila { Producto = porId[x.ProductoId], Cantidad = x.Cantidad })
            .ToList();
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
        return RedirectToPage();
    }

    public IActionResult OnPostCambiar(string id, int delta)
    {
        var carrito = HttpContext.Session.Obtener();
        var item = carrito.FirstOrDefault(x => x.ProductoId == id);
        if (item is not null)
        {
            item.Cantidad += delta;
            if (item.Cantidad <= 0)
                carrito.Remove(item);
            HttpContext.Session.Guardar(carrito);
        }
        return RedirectToPage();
    }

    public IActionResult OnPostQuitar(string id)
    {
        var carrito = HttpContext.Session.Obtener();
        HttpContext.Session.Guardar(carrito.Where(x => x.ProductoId != id).ToList());
        return RedirectToPage();
    }

    public IActionResult OnPostVaciar()
    {
        HttpContext.Session.Guardar(new());
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostConfirmarAsync()
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(_api.Uid))
            return RedirectToPage("/Login");

        await CargarAsync();
        if (Items.Count == 0)
        {
            TempData["Error"] = "El carrito está vacío.";
            return RedirectToPage();
        }

        var uid = _api.Uid!;
        var todoBien = true;
        foreach (var fila in Items)
        {
            var pedido = new NuevoPedidoWeb
            {
                UsuarioId = uid,
                ProductoId = fila.Producto.Id,
                ProductoTitulo = fila.Producto.Titulo,
                Cantidad = fila.Cantidad,
                PrecioUnitarioCordobas = fila.Producto.PrecioCordobas,
                TotalCordobas = fila.Subtotal,
                Estado = "Confirmado",
                ModalidadEntrega = fila.Producto.ModalidadEntrega
            };

            if (!await _api.CrearPedidoAsync(pedido))
            {
                todoBien = false;
                break;
            }

            var stock = Math.Max(0, fila.Producto.Stock - fila.Cantidad);
            if (!await _api.ActualizarProductoAsync(fila.Producto.Id,
                    new ProductoPatch { Estado = "Vendido", Stock = stock }))
            {
                todoBien = false;
                break;
            }
        }

        if (todoBien)
        {
            HttpContext.Session.Guardar(new());
            TempData["Mensaje"] = "¡Compra confirmada! Ya podés ver tus pedidos.";
            return RedirectToPage("/Pedidos");
        }

        TempData["Error"] = _api.UltimoError ?? "No se pudo confirmar la compra.";
        return RedirectToPage();
    }
}
