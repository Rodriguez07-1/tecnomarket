using System.Net;
using System.Text.Json;
using TecnoMarket.Api.Seguridad;

namespace TecnoMarket.Api;

/// <summary>
/// Una sola puerta de entrada para Android y Web: valida el JWT de Supabase
/// y separa operaciones públicas, de usuario y de administrador.
/// </summary>
public static class SeguridadMiddleware
{
    public static IApplicationBuilder UseSesionTecnoMarket(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            // La app concatenationa API_BASE_URL (termina en /) con "/ruta",
            // lo que genera "//auth/...". Normalizamos para que funcione igual.
            var ruta = (context.Request.Path.Value ?? "").TrimEnd('/');
            while (ruta.StartsWith("//", StringComparison.Ordinal)) ruta = ruta[1..];
            var metodo = context.Request.Method;

            if (EsPublica(ruta, metodo, context.Request.Query))
            {
                await next();
                return;
            }

            var validador = context.RequestServices.GetRequiredService<SesionSupabase>();
            var sesion = await validador.ValidarAsync(context.Request.Headers.Authorization.ToString());
            if (sesion is null)
            {
                await Responder(context, HttpStatusCode.Unauthorized, "Inicia sesión para continuar.");
                return;
            }

            context.Items["sesion"] = sesion;

            var admin = RequiereAdmin(ruta, metodo, context.Request.Query);
            if (admin && !sesion.EsAdmin)
            {
                await Responder(context, HttpStatusCode.Forbidden, "Esta acción es solo para administradores.");
                return;
            }

            var vendedor = context.Request.Query["vendedor_id"].ToString();
            if (!sesion.EsAdmin && vendedor.StartsWith("eq.") && vendedor[3..] != sesion.Id)
            {
                await Responder(context, HttpStatusCode.Forbidden, "Solo puedes ver tus propios artículos.");
                return;
            }

            var favorito = context.Request.Query["usuario_id"].ToString();
            if (!sesion.EsAdmin && favorito.StartsWith("eq.") && favorito[3..] != sesion.Id)
            {
                await Responder(context, HttpStatusCode.Forbidden, "Solo puedes ver tus propios favoritos.");
                return;
            }

            await next();
        });

    private static bool EsPublica(string ruta, string metodo, IQueryCollection query)
    {
        if (ruta.StartsWith("/auth/", StringComparison.Ordinal)) return true;
        if (ruta == "/api/health") return true;
        // La documentación interactiva es pública para poder demostrarla.
        if (metodo == HttpMethods.Get &&
            (ruta == "/swagger" || ruta.StartsWith("/swagger/", StringComparison.Ordinal))) return true;
        if (ruta.StartsWith("/rest/v1/rpc/", StringComparison.Ordinal))
            return !ruta.EndsWith("/rpc/ejecutar_etl", StringComparison.Ordinal);

        if (metodo != HttpMethods.Get) return false;

        if (ruta.StartsWith("/api/catalogo", StringComparison.Ordinal)) return true;
        if (ruta == "/rest/v1/categorias") return true;

        // Agregados del data warehouse: solo lectura, aptos para el panel público.
        if (ruta.StartsWith("/rest/v1/vw_dw_", StringComparison.Ordinal)) return true;

        // El catálogo normal siempre trae un filtro de estado.
        return ruta == "/rest/v1/productos" && query.ContainsKey("estado");
    }

    private static bool RequiereAdmin(string ruta, string metodo, IQueryCollection query)
    {
        if (metodo == HttpMethods.Get && ruta == "/rest/v1/usuarios") return true;
        // Disparar el ETL es operación de administrador.
        if (ruta.EndsWith("/rpc/ejecutar_etl", StringComparison.Ordinal)) return true;
        // Borrar artículos es solo de admin. Marcar como "Vendido" lo hace
        // cualquier usuario autenticado al confirmar una compra (se valida abajo).
        if (metodo == HttpMethods.Delete && ruta.StartsWith("/rest/v1/productos", StringComparison.Ordinal)) return true;

        if (metodo == HttpMethods.Get && ruta == "/rest/v1/productos")
        {
            var filtros = new[] { "estado", "id", "categoria_id", "vendedor_id", "titulo" };
            return !filtros.Any(query.ContainsKey);
        }
        return false;
    }

    private static Task Responder(HttpContext context, HttpStatusCode codigo, string mensaje)
    {
        context.Response.StatusCode = (int)codigo;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { mensaje }));
    }
}
