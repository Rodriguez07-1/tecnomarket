using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages.Admin;

public sealed class AnaliticaModel : PageModel
{
    private readonly ApiCliente _api;

    public AnaliticaModel(ApiCliente api) => _api = api;

    public ResumenDw? Resumen { get; private set; }
    public List<VentaCategoria> VentasCategoria { get; private set; } = new();
    public List<ProductoPopular> ProductosPopulares { get; private set; } = new();
    public List<VentaMes> VentasMes { get; private set; } = new();
    public List<EtlBitacora> Bitacora { get; private set; } = new();
    public string? Error { get; private set; }
    public string? Mensaje { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");
        await CargarAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostActualizarAsync()
    {
        if (!_api.EsAdmin) return RedirectToPage("/Index");

        var correcto = await _api.EjecutarEtlAsync();
        Mensaje = correcto
            ? "Datos actualizados correctamente."
            : "No se pudo actualizar: " + (_api.UltimoError ?? "error desconocido.");

        await CargarAsync();
        return Page();
    }

    private async Task CargarAsync()
    {
        Resumen = await _api.ResumenDwAsync();
        VentasCategoria = await _api.VentasCategoriaAsync();
        ProductosPopulares = await _api.ProductosPopularesAsync();
        VentasMes = await _api.VentasMesAsync();
        Bitacora = await _api.EtlBitacoraAsync();
        Error = _api.UltimoError;
    }
}
