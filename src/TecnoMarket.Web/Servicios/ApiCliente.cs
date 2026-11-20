using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TecnoMarket.Shared;

namespace TecnoMarket.Web.Servicios;

public sealed record AuthResultado(string Token, string Refresh, string Id, string Email);

/// <summary>Cliente HTTP que consume la API C# (la misma que usa la app Android).</summary>
public sealed class ApiCliente
{
    private readonly HttpClient _http;
    private readonly IHttpContextAccessor? _contexto;

    public ApiCliente(HttpClient http, IHttpContextAccessor contexto)
    {
        _http = http;
        _contexto = contexto;
        var token = contexto.HttpContext?.Request.Cookies["tm_token"];
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
    }

    /// <summary>Último mensaje de error devuelto por la API.</summary>
    public string? UltimoError { get; private set; }

    public bool SesionIniciada =>
        _http.DefaultRequestHeaders.Contains("Authorization");

    public string? Correo => HttpContextActual?.Request.Cookies["tm_email"];
    public bool EsAdmin => HttpContextActual?.Request.Cookies["tm_admin"] == "1";

    private HttpContext? HttpContextActual => _contexto?.HttpContext;

    public async Task<List<Categoria>> CategoriasAsync() =>
        await GetAsync<List<Categoria>>("api/catalogo/categorias") ?? new();

    public async Task<List<ProductoItem>> ProductosAsync(int? categoriaId = null, string? busqueda = null)
    {
        var partes = new List<string>();
        if (categoriaId.HasValue) partes.Add($"categoriaId={categoriaId.Value}");
        if (!string.IsNullOrWhiteSpace(busqueda)) partes.Add($"busqueda={Uri.EscapeDataString(busqueda.Trim())}");
        var ruta = "api/catalogo/productos" + (partes.Count > 0 ? "?" + string.Join("&", partes) : "");
        return await GetAsync<List<ProductoItem>>(ruta) ?? new();
    }

    public async Task<ProductoItem?> ProductoAsync(string id) =>
        await GetAsync<ProductoItem>($"api/catalogo/productos/{Uri.EscapeDataString(id)}");

    // ---------- Data warehouse (vistas de agregación) ----------

    private async Task<T?> PrimeroAsync<T>(string ruta)
    {
        var lista = await GetAsync<List<T>>(ruta);
        return lista is { Count: > 0 } ? lista[0] : default;
    }

    public Task<ResumenDw?> ResumenDwAsync() =>
        PrimeroAsync<ResumenDw>("rest/v1/vw_dw_resumen?select=*");

    public async Task<List<VentaCategoria>> VentasCategoriaAsync() =>
        await GetAsync<List<VentaCategoria>>("rest/v1/vw_dw_ventas_categoria?select=*") ?? new();

    public async Task<List<ProductoPopular>> ProductosPopularesAsync() =>
        await GetAsync<List<ProductoPopular>>("rest/v1/vw_dw_productos_populares?select=*") ?? new();

    public async Task<List<VentaMes>> VentasMesAsync() =>
        await GetAsync<List<VentaMes>>("rest/v1/vw_dw_ventas_mes?select=*") ?? new();

    public async Task<List<EtlBitacora>> EtlBitacoraAsync() =>
        await GetAsync<List<EtlBitacora>>("rest/v1/vw_dw_etl_bitacora?select=*") ?? new();

    /// <summary>Dispara una recarga completa del DW (requiere admin en la API).</summary>
    public async Task<bool> EjecutarEtlAsync()
    {
        UltimoError = null;
        try
        {
            using var cuerpo = JsonContent.Create(new { });
            var respuesta = await _http.PostAsync("rest/v1/rpc/ejecutar_etl", cuerpo);
            if (!respuesta.IsSuccessStatusCode)
            {
                var texto = await respuesta.Content.ReadAsStringAsync();
                UltimoError = ExtraerMensaje(texto) ?? $"Error {(int)respuesta.StatusCode} al recargar los datos.";
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            UltimoError = "No se pudo conectar con la API. " + e.Message;
            return false;
        }
    }

    public async Task<AuthResultado?> IniciarSesionAsync(string email, string password)
    {
        UltimoError = null;
        try
        {
            using var cuerpo = JsonContent.Create(new { email, password });
            var respuesta = await _http.PostAsync("auth/v1/token?grant_type=password", cuerpo);
            var texto = await respuesta.Content.ReadAsStringAsync();
            if (!respuesta.IsSuccessStatusCode)
            {
                UltimoError = ExtraerMensaje(texto) ?? $"Error {(int)respuesta.StatusCode} al iniciar sesión.";
                return null;
            }
            using var documento = JsonDocument.Parse(texto);
            var raiz = documento.RootElement;
            var token = raiz.GetProperty("access_token").GetString() ?? "";
            var refresh = raiz.TryGetProperty("refresh_token", out var r) ? r.GetString() ?? "" : "";
            var user = raiz.TryGetProperty("user", out var u) ? u : default;
            var id = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("id", out var i)
                ? i.GetString() ?? "" : "";
            var correo = user.ValueKind == JsonValueKind.Object && user.TryGetProperty("email", out var e)
                ? e.GetString() ?? email : email;
            return new AuthResultado(token, refresh, id, correo);
        }
        catch (Exception e)
        {
            UltimoError = "No se pudo conectar con la API. " + e.Message;
            return null;
        }
    }

    public string? Uid => HttpContextActual?.Request.Cookies["tm_uid"];

    // ---------- Envíos (favoritos, carrito, publicar, administración) ----------

    // PostgREST trata un campo null explícito como "poner NULL en la columna".
    // Por eso omitimos los nulos (por ejemplo, el stock en un PATCH de estado).
    private static readonly JsonSerializerOptions OpcionesEnvio = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task<(bool Ok, string Texto)> EnviarAsync(
        HttpMethod metodo, string ruta, object? cuerpo = null, string? prefer = null)
    {
        UltimoError = null;
        try
        {
            using var mensaje = new HttpRequestMessage(metodo, ruta);
            if (cuerpo is not null) mensaje.Content = JsonContent.Create(cuerpo, options: OpcionesEnvio);
            if (prefer is not null) mensaje.Headers.TryAddWithoutValidation("Prefer", prefer);

            using var respuesta = await _http.SendAsync(mensaje);
            var texto = await respuesta.Content.ReadAsStringAsync();
            if (!respuesta.IsSuccessStatusCode)
            {
                UltimoError = ExtraerMensaje(texto) ?? $"Error {(int)respuesta.StatusCode} en la API.";
                return (false, texto);
            }
            return (true, texto);
        }
        catch (Exception e)
        {
            UltimoError = "No se pudo conectar con la API. " + e.Message;
            return (false, "");
        }
    }

    private async Task<bool> EnviarOkAsync(
        HttpMethod metodo, string ruta, object? cuerpo = null, string? prefer = null) =>
        (await EnviarAsync(metodo, ruta, cuerpo, prefer)).Ok;

    public async Task<List<Favorito>> FavoritosAsync(string uid) =>
        await GetAsync<List<Favorito>>(
            $"rest/v1/favoritos?usuario_id=eq.{Uri.EscapeDataString(uid)}&select=*") ?? new();

    public Task<bool> AgregarFavoritoAsync(string uid, string productoId) =>
        EnviarOkAsync(HttpMethod.Post, "rest/v1/favoritos",
            new NuevoFavorito { UsuarioId = uid, ProductoId = productoId }, "return=minimal");

    public Task<bool> BorrarFavoritoAsync(string id) =>
        EnviarOkAsync(HttpMethod.Delete, $"rest/v1/favoritos?id=eq.{Uri.EscapeDataString(id)}");

    public async Task<string?> SubirImagenAsync(string uid, IFormFile archivo)
    {
        UltimoError = null;
        try
        {
            var extension = Path.GetExtension(archivo.FileName).TrimStart('.').ToLowerInvariant();
            if (extension is not ("png" or "jpg" or "jpeg" or "webp")) extension = "jpg";

            var ruta = $"storage/v1/object/productos/{Uri.EscapeDataString(uid)}/{Guid.NewGuid():N}.{extension}";
            using var memoria = new MemoryStream();
            await archivo.CopyToAsync(memoria);
            if (memoria.Length == 0)
            {
                UltimoError = "La imagen está vacía.";
                return null;
            }

            using var contenido = new ByteArrayContent(memoria.ToArray());
            contenido.Headers.ContentType = new MediaTypeHeaderValue(
                string.IsNullOrWhiteSpace(archivo.ContentType) ? "image/jpeg" : archivo.ContentType);
            using var mensaje = new HttpRequestMessage(HttpMethod.Put, ruta) { Content = contenido };
            mensaje.Headers.TryAddWithoutValidation("x-upsert", "true");

            using var respuesta = await _http.SendAsync(mensaje);
            var texto = await respuesta.Content.ReadAsStringAsync();
            if (!respuesta.IsSuccessStatusCode)
            {
                UltimoError = ExtraerMensaje(texto) ?? $"Error {(int)respuesta.StatusCode} al subir la foto.";
                return null;
            }
            return texto.Trim().Trim('"');
        }
        catch (Exception e)
        {
            UltimoError = "No se pudo subir la foto. " + e.Message;
            return null;
        }
    }

    public async Task<Producto?> PublicarProductoAsync(NuevoProductoWeb producto)
    {
        var resultado = await EnviarAsync(
            HttpMethod.Post, "rest/v1/productos", producto, "return=representation");
        if (!resultado.Ok) return null;
        try
        {
            return JsonSerializer.Deserialize<List<Producto>>(resultado.Texto)?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<Producto>> ProductosTodosAsync() =>
        await GetAsync<List<Producto>>("rest/v1/productos?select=*&order=fecha_publicacion.desc") ?? new();

    public async Task<List<Producto>> ProductosPorIdsAsync(IEnumerable<string> ids)
    {
        var lista = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (lista.Count == 0) return new();
        var filtro = "id=in.(" + string.Join(",", lista.Select(Uri.EscapeDataString)) + ")";
        return await GetAsync<List<Producto>>($"rest/v1/productos?select=*&{filtro}") ?? new();
    }

    public Task<bool> ActualizarProductoAsync(string id, ProductoPatch patch) =>
        EnviarOkAsync(HttpMethod.Patch, $"rest/v1/productos?id=eq.{Uri.EscapeDataString(id)}",
            patch, "return=minimal");

    public Task<bool> CambiarEstadoProductoAsync(string id, string estado) =>
        ActualizarProductoAsync(id, new ProductoPatch { Estado = estado });

    public async Task<List<Producto>> ProductosPorVendedorAsync(string uid) =>
        await GetAsync<List<Producto>>(
            $"rest/v1/productos?select=*&vendedor_id=eq.{Uri.EscapeDataString(uid)}&order=fecha_publicacion.desc") ?? new();

    public Task<bool> BorrarProductoAsync(string id) =>
        EnviarOkAsync(HttpMethod.Delete, $"rest/v1/productos?id=eq.{Uri.EscapeDataString(id)}");

    public async Task<List<Pedido>> PedidosAsync() =>
        await GetAsync<List<Pedido>>("rest/v1/pedidos?select=*&order=fecha_pedido.desc") ?? new();

    public Task<bool> CrearPedidoAsync(NuevoPedidoWeb pedido) =>
        EnviarOkAsync(HttpMethod.Post, "rest/v1/pedidos", pedido, "return=minimal");

    // ---------- Chat vendedor <-> cliente (misma API y misma BD) ----------

    /// <summary>Mensajes de una conversación (producto + comprador).</summary>
    public async Task<List<MensajeWeb>> MensajesAsync(string productoId, string compradorId) =>
        await GetAsync<List<MensajeWeb>>(
            $"rest/v1/mensajes?producto_id=eq.{Uri.EscapeDataString(productoId)}" +
            $"&comprador_id=eq.{Uri.EscapeDataString(compradorId)}" +
            "&select=*&order=fecha_envio.asc") ?? new();

    /// <summary>Último mensaje de cada conversación en la que participa el usuario.</summary>
    public async Task<List<MensajeWeb>> ConversacionesAsync(string uid)
    {
        var filtro = $"or=(comprador_id.eq.{uid},vendedor_id.eq.{uid})";
        return await GetAsync<List<MensajeWeb>>(
            $"rest/v1/mensajes?{Uri.EscapeDataString(filtro)}&select=*&order=fecha_envio.desc") ?? new();
    }

    public Task<bool> EnviarMensajeAsync(NuevoMensajeWeb mensaje) =>
        EnviarOkAsync(HttpMethod.Post, "rest/v1/mensajes", mensaje, "return=representation");

    public Task<bool> MarcarMensajeLeidoAsync(string id) =>
        EnviarOkAsync(HttpMethod.Patch, $"rest/v1/mensajes?id=eq.{Uri.EscapeDataString(id)}",
            new MensajeLeidoPatch(), "return=minimal");

    public async Task CerrarSesionAsync()
    {
        try { await _http.PostAsync("auth/v1/logout", null); }
        catch { /* se cierra la sesión local de todos modos */ }
    }

    private async Task<T?> GetAsync<T>(string ruta)
    {
        UltimoError = null;
        try
        {
            var respuesta = await _http.GetAsync(ruta);
            if (!respuesta.IsSuccessStatusCode)
            {
                var cuerpo = await respuesta.Content.ReadAsStringAsync();
                UltimoError = ExtraerMensaje(cuerpo) ?? $"Error {(int)respuesta.StatusCode} al consultar la API.";
                return default;
            }
            return await respuesta.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception e)
        {
            UltimoError = "No se pudo conectar con la API. " + e.Message;
            return default;
        }
    }

    private static string? ExtraerMensaje(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var documento = JsonDocument.Parse(json);
            foreach (var clave in new[] { "mensaje", "message", "msg", "error_description" })
                if (documento.RootElement.TryGetProperty(clave, out var valor))
                    return valor.GetString();
        }
        catch { /* cuerpo no JSON */ }
        return null;
    }
}
