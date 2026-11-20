# TecnoMarket en Render (plan Free)

Dos Web Services en la misma cuenta de Render:

| Servicio | Dockerfile | URL final |
|---|---|---|
| `tecnomarket-api` | `Dockerfile.api` | `https://tecnomarket-api.onrender.com` |
| `tecnomarket-web` | `Dockerfile.web` | `https://tecnomarket-web.onrender.com` |

## Puntos clave

- Runtime: imágenes oficiales `mcr.microsoft.com/dotnet/sdk:11.0` y `aspnet:11.0`.
- Cada servicio escucha en el puerto que Render entrega en `PORT`.
- La API no necesita secretos nuevos: usa la URL y la clave `publishable` de Supabase que ya están en `appsettings.json`.
- La Web necesita esta variable de entorno:
  - `Api__BaseUrl = https://tecnomarket-api.onrender.com/`
- Health checks:
  - API: `/swagger/index.html` (no `/api/health`, porque ese endpoint devuelve 503 sin contraseña PostgreSQL).
  - Web: `/Arquitectura`.
- Swagger público: `https://tecnomarket-api.onrender.com/swagger`.

## Limitaciones reales del plan Free

- El servicio se duerme a los 15 minutos sin tráfico y tarda ~1 minuto en volver.
- 750 horas/mes para todo el workspace.
- Para la presentación: hacer un ping a ambas URLs unos minutos antes.

La base de datos sigue siendo el proyecto de Supabase de siempre; Render solo hospeda los procesos C#.
