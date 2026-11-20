using Npgsql;
using TecnoMarket.Api.Data;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Repositorios;

public sealed class PedidoRepositorio
{
    private readonly SupabaseDb _db;
    public PedidoRepositorio(SupabaseDb db) => _db = db;

    public async Task<List<Pedido>> ListarAsync(string? usuarioId = null)
    {
        var sql = @"
            select id, usuario_id, producto_id, producto_titulo, cantidad,
                   precio_unitario_cordobas, total_cordobas, estado,
                   coalesce(modalidad_entrega,'') as modalidad_entrega,
                   coalesce(fecha_pedido::text,'') as fecha_pedido
            from public.pedidos";
        if (!string.IsNullOrWhiteSpace(usuarioId)) sql += " where usuario_id = @usuario";
        sql += " order by fecha_pedido desc";

        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = sql;
        if (!string.IsNullOrWhiteSpace(usuarioId)) cmd.Parameters.AddWithValue("usuario", usuarioId);

        await using var lector = await cmd.ExecuteReaderAsync();
        var lista = new List<Pedido>();
        while (await lector.ReadAsync())
        {
            lista.Add(new Pedido
            {
                Id = lector.Texto("id"),
                UsuarioId = lector.Texto("usuario_id"),
                ProductoId = lector.Texto("producto_id"),
                ProductoTitulo = lector.Texto("producto_titulo"),
                Cantidad = lector.GetInt32(lector.GetOrdinal("cantidad")),
                PrecioUnitarioCordobas = lector.GetDecimal(lector.GetOrdinal("precio_unitario_cordobas")),
                TotalCordobas = lector.GetDecimal(lector.GetOrdinal("total_cordobas")),
                Estado = lector.Texto("estado"),
                ModalidadEntrega = lector.TextoNulo("modalidad_entrega"),
                FechaPedido = lector.TextoNulo("fecha_pedido")
            });
        }
        return lista;
    }

    public async Task<bool> ExisteAsync(string usuarioId, string productoId)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"select 1 from public.pedidos
                            where usuario_id = @usuario and producto_id = @producto limit 1";
        cmd.Parameters.AddWithValue("usuario", usuarioId);
        cmd.Parameters.AddWithValue("producto", productoId);
        return await cmd.ExecuteScalarAsync() is not null;
    }

    public async Task<bool> CrearAsync(Pedido pedido)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            insert into public.pedidos
                (usuario_id, producto_id, producto_titulo, cantidad,
                 precio_unitario_cordobas, total_cordobas, estado, modalidad_entrega)
            values
                (@usuario, @producto, @titulo, @cantidad, @precio, @total, 'Confirmado', @entrega)";
        cmd.Parameters.AddWithValue("usuario", pedido.UsuarioId);
        cmd.Parameters.AddWithValue("producto", pedido.ProductoId);
        cmd.Parameters.AddWithValue("titulo", pedido.ProductoTitulo);
        cmd.Parameters.AddWithValue("cantidad", pedido.Cantidad);
        cmd.Parameters.AddWithValue("precio", pedido.PrecioUnitarioCordobas);
        cmd.Parameters.AddWithValue("total", pedido.TotalCordobas);
        cmd.Parameters.AddWithValue("entrega", (object?)pedido.ModalidadEntrega ?? DBNull.Value);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
