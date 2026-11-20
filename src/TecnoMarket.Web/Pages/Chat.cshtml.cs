using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TecnoMarket.Shared;
using TecnoMarket.Web.Servicios;

namespace TecnoMarket.Web.Pages;

/// <summary>
/// Chat ligero entre el vendedor y el cliente. Usa la misma API y la tabla
/// public.mensajes de Supabase; no agrega SignalR ni servicios nuevos.
/// </summary>
public sealed class ChatModel : PageModel
{
    private readonly ApiCliente _api;
    public ChatModel(ApiCliente api) => _api = api;

    public sealed class ConversacionVm
    {
        public string ProductoId { get; set; } = "";
        public string Titulo { get; set; } = "Producto";
        public string Imagen { get; set; } = "";
        public string CompradorId { get; set; } = "";
        public string VendedorId { get; set; } = "";
        public string Ultimo { get; set; } = "";
        public string Fecha { get; set; } = "";
        public int SinLeer { get; set; }
    }

    public string? MiId => _api.Uid;
    public string? ProductoId { get; private set; }
    public string? CompradorId { get; private set; }
    public string? VendedorId { get; private set; }
    public Producto? Producto { get; private set; }
    public List<MensajeWeb> Mensajes { get; private set; } = new();
    public List<ConversacionVm> Conversaciones { get; private set; } = new();
    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(string? producto = null, string? comprador = null)
    {
        if (!_api.SesionIniciada) return RedirectToPage("/Login");
        if (string.IsNullOrWhiteSpace(MiId)) return RedirectToPage("/Login");

        if (!string.IsNullOrWhiteSpace(producto))
        {
            ProductoId = producto;
            var item = await _api.ProductoAsync(producto);
            if (item is null)
            {
                Error = "No se encontró el producto de esta conversación.";
                return Page();
            }

            Producto = item.Producto;
            VendedorId = Producto.VendedorId;

            // El vendedor no puede iniciar una conversación "consigo mismo":
            // si entra desde el detalle del producto, lo llevamos a la conversación
            // real del cliente (o le avisamos de que todavía no hay una).
            if (string.IsNullOrWhiteSpace(comprador) && MiId == VendedorId)
            {
                var conversaciones = await _api.ConversacionesAsync(MiId);
                var existente = conversaciones
                    .FirstOrDefault(m => m.ProductoId == producto && m.CompradorId != MiId);
                if (existente is not null)
                    return RedirectToPage("/Chat", new { producto, comprador = existente.CompradorId });

                Error = "Aún no hay conversaciones para este producto. Cuando un cliente escriba, aparecerán aquí.";
                return Page();
            }

            CompradorId = !string.IsNullOrWhiteSpace(comprador) ? comprador : MiId;

            if (MiId != CompradorId && MiId != VendedorId)
            {
                Error = "Esta conversación no es tuya.";
                return Page();
            }

            Mensajes = await _api.MensajesAsync(producto, CompradorId);
            Error = _api.UltimoError;

            // Marcar como leídos los mensajes recibidos (tope 20 para no saturar).
            var pendientes = Mensajes
                .Where(m => m.RemitenteId != MiId && !m.Leido)
                .Take(20)
                .Select(m => _api.MarcarMensajeLeidoAsync(m.Id))
                .ToList();
            if (pendientes.Count > 0)
            {
                await Task.WhenAll(pendientes);
                foreach (var m in Mensajes.Where(m => m.RemitenteId != MiId)) m.Leido = true;
            }
        }
        else
        {
            var todos = await _api.ConversacionesAsync(MiId);
            Error = _api.UltimoError;

            var grupos = todos
                .GroupBy(m => m.ProductoId + "|" + m.CompradorId)
                .Select(g => g.OrderByDescending(m => m.FechaEnvio).ToList())
                .OrderByDescending(g => g[0].FechaEnvio)
                .Take(40)
                .ToList();

            var productos = await _api.ProductosPorIdsAsync(grupos.Select(g => g[0].ProductoId));
            var porId = productos.ToDictionary(p => p.Id, p => p);

            Conversaciones = grupos.Select(g =>
            {
                var ultimo = g[0];
                var prod = porId.GetValueOrDefault(ultimo.ProductoId);
                return new ConversacionVm
                {
                    ProductoId = ultimo.ProductoId,
                    Titulo = prod?.Titulo ?? "Producto",
                    Imagen = string.IsNullOrWhiteSpace(prod?.ImagenUrl) ? "/img/sin-imagen.svg" : prod!.ImagenUrl!,
                    CompradorId = ultimo.CompradorId,
                    VendedorId = ultimo.VendedorId,
                    Ultimo = ultimo.Contenido,
                    Fecha = ultimo.FechaEnvio,
                    SinLeer = g.Count(m => m.RemitenteId != MiId && !m.Leido)
                };
            }).ToList();
        }

        return Page();
    }

    /// <summary>Devuelve los mensajes en JSON para el sondeo ligero del navegador.</summary>
    public async Task<IActionResult> OnGetMensajesAsync(string producto, string comprador)
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(MiId))
            return new JsonResult(new { error = "Sesión expirada" });

        var mensajes = await _api.MensajesAsync(producto, comprador);
        return new JsonResult(new
        {
            mensajes = mensajes.Select(m => new
            {
                id = m.Id,
                remitente = m.RemitenteId,
                contenido = m.Contenido,
                fecha = m.FechaEnvio,
                leido = m.Leido
            })
        });
    }

    public async Task<IActionResult> OnPostEnviarAsync(
        string producto, string comprador, string vendedor, string contenido)
    {
        if (!_api.SesionIniciada || string.IsNullOrWhiteSpace(MiId))
            return RedirectToPage("/Login");

        contenido = (contenido ?? "").Trim();
        if (contenido.Length is < 1 or > 2000)
            TempData["Error"] = "El mensaje debe tener entre 1 y 2000 caracteres.";
        else
        {
            var ok = await _api.EnviarMensajeAsync(new NuevoMensajeWeb
            {
                ProductoId = producto,
                CompradorId = comprador,
                VendedorId = vendedor,
                RemitenteId = MiId,
                Contenido = contenido
            });
            if (!ok) TempData["Error"] = _api.UltimoError ?? "No se pudo enviar el mensaje.";
        }

        return RedirectToPage("/Chat", new { producto, comprador });
    }
}
