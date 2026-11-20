# TecnoMarket — una sola API para Android y Web

```
📱 Android (Kotlin/Compose) ──┐
                               ├──▶ TecnoMarket.Api (C#) ──▶ Supabase (Auth + PostgreSQL + Storage)
🌐 Web (Razor Pages) ─────────┘                                  │
                                                                 └──▶ esquema dw (Data Warehouse)
```

La app Android y la página web consumen **la misma API C#**. No hay tablas duplicadas: todo vive en el proyecto de Supabase de siempre. El data warehouse es un esquema separado (`dw`) dentro de esa misma BD.

## Estado actual

| Elemento | Estado |
|---|---|
| Solución C# (`TecnoMarket.slnx`) | ✅ Compila, 0 errores, 0 advertencias |
| API con rutas `/rest/v1/*` y `/auth/v1/*` | ✅ |
| Swagger UI `/swagger` (29 rutas documentadas) | ✅ Público, con soporte para JWT |
| Rediseño visual Web (navegación, tarjetas, KPIs, tablas y formularios) | ✅ |
| CSS incrustado en HTML5 (`tm-core.css` + `site.css`) | ✅ Se ve aunque el proxy descarte CSS externo |
| Chat vendedor/cliente en Web y Android | ✅ Misma API y tabla `public.mensajes` |
| Web con catálogo, búsqueda, categorías y login | ✅ |
| Web: favoritos, carrito/checkout, publicar con foto y panel admin | ✅ Verificado de punta a punta |
| Panel de analítica `/Admin/Analitica` (solo admin) | ✅ |
| Restricción `productos_estado_check` en Supabase | ✅ Corregida: acepta `Pendiente` |
| App Android apuntando a la API | ✅ Compila (`BUILD SUCCESSFUL`) e instalada en `R5CR20PDBSR` |
| Publicar desde Android nace `Pendiente` | ✅ Corregido (`encodeDefaults`) y verificado en el celular |
| Modo proxy con clave publishable | ✅ Funciona sin contraseña de BD |
| Modo Npgsql (lectura/escritura directa) | ⏳ Falta contraseña de BD |
| Bucket `productos` + políticas RLS | ✅ Creado y verificado |
| Catálogo público sin login (`anon`) | ✅ Políticas `tm_*_select_publico` aplicadas en Supabase |
| Data Warehouse `dw` + carga inicial | ✅ C$ 2.100 en ventas, 4 pedidos |
| ETL diario automático (pg_cron, 03:00 UTC) | ✅ Programado y verificado |
| Despliegue permanente | ✅ Render (plan Free), API + Web activas sin depender de la PC |

## URLs públicas activas (Render)

- **API:** https://tecnomarket-api.onrender.com
- **Swagger:** https://tecnomarket-api.onrender.com/swagger
- **Web:** https://tecnomarket-web.onrender.com
- **Arquitectura (dentro de la Web):** https://tecnomarket-web.onrender.com/Arquitectura
- **Chat (dentro de la Web):** https://tecnomarket-web.onrender.com/Chat

> Render duerme los servicios Free tras ~15 min sin tráfico; la primera visita tras dormir tarda ~1 min. Para la presentación, hacer ping a ambas URLs unos minutos antes. Las URLs también se guardan en `URLS.txt`.

Si cambia la URL de la API, actualiza:
1. `app\src\main\java\com\tecnomarket\app\data\ApiConfig.kt` → `API_BASE_URL`
2. `src\TecnoMarket.Web\appsettings.json` → `Api:BaseUrl`
3. La variable `Api__BaseUrl` del servicio Web en Render (ver `DEPLOY_RENDER.md`).

## Cómo funciona la API

- Si `ConnectionStrings:Supabase` tiene contraseña → usa **Npgsql** directo a PostgreSQL.
- Si no la tiene → usa **modo proxy** contra Supabase REST/Auth con la clave `publishable` y el JWT del usuario (igual que funcionaba antes la app). Por eso la web y la app ya pueden mostrar categorías y catálogo.

La validación de sesión se hace contra `/auth/v1/user` de Supabase. Administrador = correo `rr4439210@gmail.com` (configurable en `Admin:Email`).

### Funciones disponibles en la Web

| Página | Qué hace |
|---|---|
| `/` | Catálogo, búsqueda, categorías y botón **Agregar** al carrito |
| `/Arquitectura` | Página explicativa del sistema: clientes, API, BD, DW y enlace a Swagger |
| `/Producto?id=<id>` | Detalle, **Agregar al carrito**, **Favoritos** y **Chat** |
| `/Favoritos` | Lista de productos guardados (misma tabla que Android) |
| `/Carrito` | Carrito local en sesión, cantidades, total y **Confirmar compra** |
| `/Pedidos` | Pedidos del usuario (todos si es admin) |
| `/Chat` | Conversaciones y mensajes privados vendedor ↔ comprador (sondeo cada 4 s) |
| `/Publicar` | Publicar con foto real; el producto nace `Pendiente` |
| `/Admin/Productos` | Aprobar, rechazar y borrar publicaciones; KPIs y pedidos |
| `/Admin/Analitica` | Panel del data warehouse |

La compra crea un registro en `public.pedidos`, descuenta el stock y marca el producto como `Vendido`, igual que la app Android. El carrito no crea tablas nuevas: vive en la sesión de ASP.NET.

### Chat vendedor ↔ cliente (2026-11-20)

- Tabla `public.mensajes` en la **misma BD**: `producto_id`, `comprador_id`, `vendedor_id`, `remitente_id`, `contenido`, `leido`, `fecha_envio`.
- RLS: solo leen/escriben los dos participantes; nadie puede escribir por otro.
- API: `GET/POST/PATCH /rest/v1/mensajes` documentados en Swagger (29 rutas en total).
- Web: `/Chat` con lista de conversaciones, burbujas y sondeo ligero cada 4 segundos (sin SignalR ni WebSockets, para que funcione tras Cloudflare).
- Android: pantalla `ChatScreen` + `ChatRepository`, botón **Chat con el vendedor** en el detalle y pestaña **Chat** en la barra inferior.
- Migración ejecutada: `supabase\migrations\20261120000600_chat_vendedor_cliente.sql`.
- Migración ejecutada: `supabase\migrations\20261120180000_catalogo_publico_anon.sql` (catálogo visible para visitantes).

### Diseño Web: por qué ahora se ve siempre

El túnel de Cloudflare y la vista previa del navegador devolvían los `.css` externos con `Content-Length: 0`, así que la página cargaba sin estilos. `_Layout.cshtml` ahora incrusta `wwwroot/css/tm-core.css` (utilidades compactas, reemplazo liviano de Bootstrap) y `wwwroot/css/site.css` dentro de un `<style>` en el propio HTML5. Ya no hay dependencia de archivos CSS ni de Bootstrap JS: el menú responsive usa 8 líneas de JavaScript inline. La página pública pasó de ~6 KB sin estilos a ~35 KB con el diseño completo.

> **Corrección de BD (2026-11-20):** la tabla `productos` tenía un `CHECK` que solo permitía `Disponible`, `Vendido` y `Pausado`, mientras la política RLS de inserción exige `Pendiente`. Eso impedía publicar desde Android y desde la Web. Se ejecutó `supabase\migrations\20261120000500_corregir_estados_productos.sql`, que ahora permite `Pendiente`, `Disponible`, `Vendido`, `Rechazado` y `Pausado`.

> **Corrección Android (2026-11-20):** `SupabaseApi.kt` usa `encodeDefaults = false`, así que el valor por defecto `estado = "Pendiente"` de `NuevoProducto` no se enviaba y la BD aplicaba su default `Disponible`. Se cambió el default del DTO a `""` y `PublicarViewModel` envía `estado = "Pendiente"` explícitamente. Verificado en el celular: el producto aparece en el panel admin con **1 pendiente** y estado `Pendiente`.

## Data Warehouse (`dw`)

Modelo estrella separado del esquema operativo (`public`):

```
public.usuarios ──┐        ┌── dim_tiempo
public.categorias ┼─ ETL ─▶├── dim_categoria      ┌── fact_publicaciones
public.productos ─┤        ├── dim_usuario        ├── fact_pedidos
public.pedidos ───┤        ├── dim_producto       └── fact_favoritos
public.favoritos ─┘        └── etl_bitacora
```

- Migraciones (ejecutadas en Supabase SQL Editor):
  - `supabase\migrations\20261120000300_data_warehouse.sql`
  - `supabase\migrations\20261120000301_etl_fix_huerfanos.sql` (miembro desconocido para productos borrados)
  - `supabase\migrations\20261120000400_programar_etl.sql` (pg_cron)
   - `supabase\migrations\20261120000500_corregir_estados_productos.sql` (CHECK de estados)
- `dw.etl_cargar()` → recarga completa transaccional. Función `public.ejecutar_etl()` para dispararla.
- El job `tecnomarket_etl_diario` corre todos los días a las **03:00 UTC** y también se puede forzar con el botón **Actualizar datos** del panel.
- Las vistas `public.vw_dw_*` son de solo lectura y están expuestas por la API con la clave publishable.
- La API marca `/rest/v1/vw_dw_*` como pública (solo GET) y `/rest/v1/rpc/ejecutar_etl` como admin.

## Proyectos

| Proyecto | Descripción |
|---|---|
| `TecnoMarket.Shared` | Modelos compartidos (snake_case, igual que Supabase) |
| `TecnoMarket.Api` | Web API REST |
| `TecnoMarket.Web` | Razor Pages (catálogo, login, favoritos, carrito, publicar, admin y analítica) |

## Ejecutar en local

```powershell
dotnet run --project src\TecnoMarket.Api
dotnet run --project src\TecnoMarket.Web
```
- API: http://localhost:5204 → **Swagger:** http://localhost:5204/swagger
- Web: http://localhost:5000 → **Arquitectura:** http://localhost:5000/Arquitectura

## Endpoints principales

| Método | Ruta | Uso |
|---|---|---|
| GET | `/api/health` | Estado de la BD |
| POST | `/auth/v1/token?grant_type=password` | Login (Android y Web) |
| POST | `/auth/v1/signup` | Registro |
| POST | `/auth/v1/logout` | Cerrar sesión |
| POST | `/rest/v1/rpc/registrar_usuario` | Registro en tabla `usuarios` |
| GET | `/rest/v1/categorias` | Categorías |
| GET | `/rest/v1/productos?...` | Catálogo (PostgREST compatible) |
| POST | `/rest/v1/productos` | Publicar → nace `Pendiente` |
| PATCH | `/rest/v1/productos?id=eq.<id>` | Aprobar / rechazar / vender |
| DELETE | `/rest/v1/productos?id=eq.<id>` | Borrar (admin) |
| GET/POST/DELETE | `/rest/v1/favoritos` | Favoritos |
| GET/POST | `/rest/v1/pedidos` | Pedidos |
| PUT | `/storage/v1/object/productos/<uid>/<archivo>` | Subir foto al bucket |
| GET | `/api/catalogo/*` | Rutas amigables que usa la web |
| GET | `/rest/v1/vw_dw_resumen` | KPIs del DW |
| GET | `/rest/v1/vw_dw_ventas_categoria` | Ventas por categoría |
| GET | `/rest/v1/vw_dw_productos_populares` | Ranking de productos |
| GET | `/rest/v1/vw_dw_ventas_mes` | Serie mensual |
| GET | `/rest/v1/vw_dw_etl_bitacora` | Historial de cargas |
| POST | `/rest/v1/rpc/ejecutar_etl` | Recargar el DW (admin) |

## Despliegue permanente (cuando quieras)

Ya están los `Dockerfile` y `docker-compose.yml`. En Render/Railway/Fly.io:

1. Crear servicio desde el Dockerfile de `src/TecnoMarket.Api`.
2. Variable de entorno: `ConnectionStrings__Supabase` con la connection string real.
3. Crear segundo servicio desde `src/TecnoMarket.Web`.
4. Variable de entorno: `Api__BaseUrl` = URL pública de la API.
5. Actualizar `ApiConfig.kt` con la URL definitiva y recompilar la app.

## Copias de seguridad

En `C:\Users\Ruben\Documents\TecnoMarket_Backups\`:
- `TecnoMarket_Backup_20261119_185434\` (original)
- `Snapshot_antes_seguridad_20261119_191125\`
- `Snapshot_estado_final_20261119_200405\`
- `Snapshot_dw_analitica_20261120\` (DW + panel de analítica)
- `Snapshot_web_funciones_20261120\` (favoritos, carrito, publicar y admin en la Web)
- `Snapshot_android_pendiente_20261120\` (fix de estado Pendiente + APK instalada)

Cada snapshot incluye `RESTAURAR.ps1` y `MANIFEST.txt`.

## Pendiente

1. Instalar el APK recompilado cuando se conecte el celular:
   `adb install -r "C:\Users\Ruben\Documents\Proyecto predeterminado\app\build\outputs\apk\debug\app-debug.apk"`
2. Elegir proveedor permanente y actualizar las URLs definitivas en `ApiConfig.kt` y `appsettings.json`.
3. Si se quiere modo Npgsql, configurar la contraseña con `.\configurar-db.ps1`.
4. Fotos de prueba ya eliminadas del bucket `productos` tras verificar la subida.
