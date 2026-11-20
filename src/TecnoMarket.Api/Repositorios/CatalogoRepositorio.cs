using Npgsql;
using TecnoMarket.Api.Data;
using TecnoMarket.Api.Servicios;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Repositorios;

public sealed class CatalogoRepositorio
{
    private readonly SupabaseDb _db;
    private readonly SupabaseRest _rest;

    public CatalogoRepositorio(SupabaseDb db, SupabaseRest rest)
    {
        _db = db;
        _rest = rest;
    }

    private const string SelectProducto = @"
        select p.id, p.titulo, p.descripcion, p.precio_cordobas, p.categoria_id,
               p.condicion, p.modalidad_entrega, p.estado, p.vendedor_id,
               coalesce(p.fecha_publicacion::text, '') as fecha_publicacion,
               coalesce(p.fecha_actualizacion::text, '') as fecha_actualizacion,
               coalesce(p.imagen_url, '') as imagen_url, p.stock,
               coalesce(c.nombre, '') as categoria_nombre,
               coalesce(trim(u.nombre || ' ' || u.apellido), '') as vendedor_nombre
        from public.productos p
        left join public.categorias c on c.id = p.categoria_id
        left join public.usuarios u on u.id = p.vendedor_id";

    public async Task<List<Categoria>> CategoriasAsync()
    {
        if (!_db.Configurada)
            return Normalizar(await _rest.ListarAsync<Categoria>("categorias?select=*&order=id.asc"));

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "select id, nombre, coalesce(descripcion,'') as descripcion, coalesce(icono,'') as icono from public.categorias order by id";
        await using var lector = await cmd.ExecuteReaderAsync();

        var lista = new List<Categoria>();
        while (await lector.ReadAsync())
        {
            lista.Add(new Categoria
            {
                Id = lector.GetInt32(0),
                Nombre = lector.Texto("nombre"),
                Descripcion = lector.Texto("descripcion"),
                Icono = lector.Texto("icono")
            });
        }
        return Normalizar(lista);
    }

    private static string Limpiar(string? texto) => (texto ?? "")
        .Replace("Ã¡", "á").Replace("Ã©", "é").Replace("Ã­", "í")
        .Replace("Ã³", "ó").Replace("Ãº", "ú").Replace("Ã±", "ñ");

    private static List<Categoria> Normalizar(List<Categoria> categorias) => categorias
        .Select(c => new Categoria
        {
            Id = c.Id,
            Nombre = Limpiar(c.Nombre),
            Descripcion = Limpiar(c.Descripcion),
            Icono = c.Icono
        })
        .ToList();

    public async Task<List<ProductoItem>> ProductosAsync(
        int? categoriaId = null,
        string? busqueda = null,
        bool soloDisponibles = false,
        bool incluirPendientes = false)
    {
        if (!_db.Configurada)
            return await ProductosProxyAsync(categoriaId, busqueda, soloDisponibles, incluirPendientes);

        var where = new List<string>();
        if (!incluirPendientes) where.Add("p.estado <> 'Pendiente'");
        if (soloDisponibles) where.Add("p.estado = 'Disponible'");
        if (categoriaId.HasValue) where.Add("p.categoria_id = @categoria");
        if (!string.IsNullOrWhiteSpace(busqueda))
            where.Add("(p.titulo ilike @busqueda or p.descripcion ilike @busqueda)");

        var sql = SelectProducto;
        if (where.Count > 0) sql += "\nwhere " + string.Join(" and ", where);
        sql += "\norder by p.fecha_publicacion desc nulls last";

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        if (categoriaId.HasValue) cmd.Parameters.AddWithValue("categoria", categoriaId.Value);
        if (!string.IsNullOrWhiteSpace(busqueda)) cmd.Parameters.AddWithValue("busqueda", $"%{busqueda.Trim()}%");

        await using var lector = await cmd.ExecuteReaderAsync();
        var lista = new List<ProductoItem>();
        while (await lector.ReadAsync()) lista.Add(LeerItem(lector));
        return lista;
    }

    public async Task<ProductoItem?> ProductoPorIdAsync(string id)
    {
        if (!_db.Configurada)
        {
            var producto = await _rest.UnoAsync<Producto>(
                "productos?id=eq." + Uri.EscapeDataString(id) + "&select=*");
            return producto is null
                ? null
                : (await EnriquecerAsync(new List<Producto> { producto })).FirstOrDefault();
        }

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = SelectProducto + "\nwhere p.id = @id limit 1";
        cmd.Parameters.AddWithValue("id", id);
        await using var lector = await cmd.ExecuteReaderAsync();
        return await lector.ReadAsync() ? LeerItem(lector) : null;
    }

    public async Task<bool> CrearAsync(Producto producto)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            insert into public.productos
                (titulo, descripcion, precio_cordobas, categoria_id, condicion,
                 modalidad_entrega, estado, vendedor_id, imagen_url, stock)
            values
                (@titulo, @descripcion, @precio, @categoria, @condicion,
                 @entrega, 'Pendiente', @vendedor, @imagen, @stock)";
        cmd.Parameters.AddWithValue("titulo", producto.Titulo);
        cmd.Parameters.AddWithValue("descripcion", producto.Descripcion);
        cmd.Parameters.AddWithValue("precio", producto.PrecioCordobas);
        cmd.Parameters.AddWithValue("categoria", producto.CategoriaId);
        cmd.Parameters.AddWithValue("condicion", producto.Condicion);
        cmd.Parameters.AddWithValue("entrega", producto.ModalidadEntrega);
        cmd.Parameters.AddWithValue("vendedor", producto.VendedorId);
        cmd.Parameters.AddWithValue("imagen", (object?)producto.ImagenUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("stock", producto.Stock);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> CambiarEstadoAsync(string id, string estado)
    {
        var permitidos = new[] { "Pendiente", "Disponible", "Rechazado", "Vendido" };
        if (!permitidos.Contains(estado)) return false;

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "update public.productos set estado = @estado, fecha_actualizacion = now() where id = @id";
        cmd.Parameters.AddWithValue("estado", estado);
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> EliminarAsync(string id)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "delete from public.productos where id = @id";
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private async Task<List<ProductoItem>> ProductosProxyAsync(
        int? categoriaId,
        string? busqueda,
        bool soloDisponibles,
        bool incluirPendientes)
    {
        var filtros = new List<string> { "select=*" };
        if (soloDisponibles) filtros.Add("estado=eq.Disponible");
        else if (!incluirPendientes)
        {
            filtros.Add("estado=neq.Pendiente");
            filtros.Add("estado=neq.Rechazado");
        }
        if (categoriaId.HasValue) filtros.Add("categoria_id=eq." + categoriaId.Value);
        if (!string.IsNullOrWhiteSpace(busqueda))
            filtros.Add("titulo=ilike.*" + Uri.EscapeDataString(busqueda.Trim()) + "*");
        filtros.Add("order=fecha_publicacion.desc");

        var productos = await _rest.ListarAsync<Producto>("productos?" + string.Join("&", filtros));
        return await EnriquecerAsync(productos);
    }

    private async Task<List<ProductoItem>> EnriquecerAsync(List<Producto> productos)
    {
        var categorias = await CategoriasAsync();
        var usuarios = await _rest.ListarAsync<Usuario>("usuarios?select=id,nombre,apellido");
        var nombresCategorias = categorias.ToDictionary(c => c.Id, c => c.Nombre);
        var nombresUsuarios = usuarios.ToDictionary(
            u => u.Id,
            u => string.Join(" ", new[] { u.Nombre, u.Apellido }.Where(x => !string.IsNullOrWhiteSpace(x))));

        return productos.Select(p => new ProductoItem
        {
            Producto = p,
            CategoriaNombre = nombresCategorias.TryGetValue(p.CategoriaId, out var nc) ? nc : "Sin categoría",
            VendedorNombre = nombresUsuarios.TryGetValue(p.VendedorId, out var nv) && nv.Length > 0
                ? nv
                : "Vendedor TecnoMarket"
        }).ToList();
    }

    // -----------------------------------------------------------------------
    // Rutas compatibles con PostgREST (/rest/v1/productos?...) para que la app
    // Android use exactamente la misma API que la página web.
    // -----------------------------------------------------------------------

    private const string SelectProductoRaw = @"
        select id, titulo, descripcion, precio_cordobas, categoria_id, condicion,
               modalidad_entrega, estado, vendedor_id,
               coalesce(fecha_publicacion::text, '') as fecha_publicacion,
               coalesce(fecha_actualizacion::text, '') as fecha_actualizacion,
               coalesce(imagen_url, '') as imagen_url, stock
        from public.productos";

    public async Task<List<Producto>> ProductosPorQueryAsync(string query)
    {
        var where = new List<string>();
        var parametros = new Dictionary<string, object>();
        var n = 0;

        foreach (var parte in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var trozos = parte.Split('=', 2);
            if (trozos.Length != 2) continue;
            var campo = trozos[0];
            var valor = Uri.UnescapeDataString(trozos[1]);

            if (campo is "select" or "order") continue;

            if (campo == "estado" && valor.StartsWith("eq.", StringComparison.Ordinal))
            {
                var clave = "estado_eq" + n++;
                where.Add("estado = @" + clave);
                parametros[clave] = valor[3..];
            }
            else if (campo == "estado" && valor.StartsWith("neq.", StringComparison.Ordinal))
            {
                var clave = "estado_neq" + n++;
                where.Add("estado <> @" + clave);
                parametros[clave] = valor[4..];
            }
            else if (campo == "categoria_id" && valor.StartsWith("eq.", StringComparison.Ordinal))
            {
                var clave = "categoria" + n++;
                where.Add("categoria_id = @" + clave);
                parametros[clave] = int.Parse(valor[3..]);
            }
            else if (campo == "vendedor_id" && valor.StartsWith("eq.", StringComparison.Ordinal))
            {
                var clave = "vendedor" + n++;
                where.Add("vendedor_id = @" + clave);
                parametros[clave] = valor[3..];
            }
            else if (campo == "id" && valor.StartsWith("eq.", StringComparison.Ordinal))
            {
                var clave = "id" + n++;
                where.Add("id = @" + clave);
                parametros[clave] = valor[3..];
            }
            else if (campo == "id" && valor.StartsWith("in.(", StringComparison.Ordinal) && valor.EndsWith(')'))
            {
                var clave = "ids" + n++;
                where.Add("id = any(@" + clave + ")");
                parametros[clave] = valor[4..^1]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToArray();
            }
            else if (campo == "titulo" && valor.StartsWith("ilike.*", StringComparison.Ordinal) && valor.EndsWith('*'))
            {
                var clave = "busqueda" + n++;
                where.Add("titulo ilike @" + clave);
                parametros[clave] = "%" + valor[7..^1] + "%";
            }
        }

        var sql = SelectProductoRaw;
        if (where.Count > 0) sql += "\nwhere " + string.Join(" and ", where);
        sql += "\norder by fecha_publicacion desc nulls last";

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (clave, valor) in parametros) cmd.Parameters.AddWithValue(clave, valor);

        await using var lector = await cmd.ExecuteReaderAsync();
        var lista = new List<Producto>();
        while (await lector.ReadAsync()) lista.Add(LeerProducto(lector));
        return lista;
    }

    public async Task<Producto?> CrearYDevolverAsync(Producto producto)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            insert into public.productos
                (titulo, descripcion, precio_cordobas, categoria_id, condicion,
                 modalidad_entrega, estado, vendedor_id, imagen_url, stock)
            values
                (@titulo, @descripcion, @precio, @categoria, @condicion,
                 @entrega, 'Pendiente', @vendedor, @imagen, @stock)
            returning id";
        cmd.Parameters.AddWithValue("titulo", producto.Titulo);
        cmd.Parameters.AddWithValue("descripcion", producto.Descripcion);
        cmd.Parameters.AddWithValue("precio", producto.PrecioCordobas);
        cmd.Parameters.AddWithValue("categoria", producto.CategoriaId);
        cmd.Parameters.AddWithValue("condicion", producto.Condicion);
        cmd.Parameters.AddWithValue("entrega", producto.ModalidadEntrega);
        cmd.Parameters.AddWithValue("vendedor", producto.VendedorId);
        cmd.Parameters.AddWithValue("imagen", (object?)producto.ImagenUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("stock", producto.Stock);

        var id = (await cmd.ExecuteScalarAsync())?.ToString();
        return id is null ? null : await ProductoRawPorIdAsync(id);
    }

    public async Task<Producto?> ProductoRawPorIdAsync(string id)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = SelectProductoRaw + "\nwhere id = @id limit 1";
        cmd.Parameters.AddWithValue("id", id);
        await using var lector = await cmd.ExecuteReaderAsync();
        return await lector.ReadAsync() ? LeerProducto(lector) : null;
    }

    public async Task<bool> ActualizarAsync(string id, ProductoPatch patch)
    {
        var cambios = new List<string>();
        if (patch.Estado is not null) cambios.Add("estado = @estado");
        if (patch.Stock.HasValue) cambios.Add("stock = @stock");
        if (cambios.Count == 0) return false;
        cambios.Add("fecha_actualizacion = now()");

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "update public.productos set " + string.Join(", ", cambios) + " where id = @id";
        cmd.Parameters.AddWithValue("id", id);
        if (patch.Estado is not null) cmd.Parameters.AddWithValue("estado", patch.Estado);
        if (patch.Stock.HasValue) cmd.Parameters.AddWithValue("stock", patch.Stock.Value);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    private static Producto LeerProducto(NpgsqlDataReader lector) => new()
    {
        Id = lector.Texto("id"),
        Titulo = lector.Texto("titulo"),
        Descripcion = lector.Texto("descripcion"),
        PrecioCordobas = lector.GetDecimal(lector.GetOrdinal("precio_cordobas")),
        CategoriaId = lector.GetInt32(lector.GetOrdinal("categoria_id")),
        Condicion = lector.Texto("condicion"),
        ModalidadEntrega = lector.Texto("modalidad_entrega"),
        Estado = lector.Texto("estado"),
        VendedorId = lector.Texto("vendedor_id"),
        FechaPublicacion = lector.TextoNulo("fecha_publicacion"),
        FechaActualizacion = lector.TextoNulo("fecha_actualizacion"),
        ImagenUrl = lector.TextoNulo("imagen_url"),
        Stock = lector.GetInt32(lector.GetOrdinal("stock"))
    };

    private static ProductoItem LeerItem(NpgsqlDataReader lector) => new()
    {
        Producto = new Producto
        {
            Id = lector.Texto("id"),
            Titulo = lector.Texto("titulo"),
            Descripcion = lector.Texto("descripcion"),
            PrecioCordobas = lector.GetDecimal(lector.GetOrdinal("precio_cordobas")),
            CategoriaId = lector.GetInt32(lector.GetOrdinal("categoria_id")),
            Condicion = lector.Texto("condicion"),
            ModalidadEntrega = lector.Texto("modalidad_entrega"),
            Estado = lector.Texto("estado"),
            VendedorId = lector.Texto("vendedor_id"),
            FechaPublicacion = lector.TextoNulo("fecha_publicacion"),
            FechaActualizacion = lector.TextoNulo("fecha_actualizacion"),
            ImagenUrl = lector.TextoNulo("imagen_url"),
            Stock = lector.GetInt32(lector.GetOrdinal("stock"))
        },
        CategoriaNombre = lector.Texto("categoria_nombre"),
        VendedorNombre = lector.Texto("vendedor_nombre")
    };
}
