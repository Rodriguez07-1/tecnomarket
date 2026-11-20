using Npgsql;
using TecnoMarket.Api.Data;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Repositorios;

public sealed class FavoritoRepositorio
{
    private readonly SupabaseDb _db;
    public FavoritoRepositorio(SupabaseDb db) => _db = db;

    public async Task<List<Favorito>> ListarAsync(string usuarioId)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"select id, usuario_id, producto_id,
                                   coalesce(fecha_guardado::text, '') as fecha_guardado
                            from public.favoritos where usuario_id = @usuario";
        cmd.Parameters.AddWithValue("usuario", usuarioId);
        await using var lector = await cmd.ExecuteReaderAsync();

        var lista = new List<Favorito>();
        while (await lector.ReadAsync())
        {
            lista.Add(new Favorito
            {
                Id = lector.Texto("id"),
                UsuarioId = lector.Texto("usuario_id"),
                ProductoId = lector.Texto("producto_id"),
                FechaGuardado = lector.TextoNulo("fecha_guardado")
            });
        }
        return lista;
    }

    public async Task<string?> CrearAsync(string usuarioId, string productoId)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = @"
            insert into public.favoritos (usuario_id, producto_id)
            values (@usuario, @producto) returning id";
        cmd.Parameters.AddWithValue("usuario", usuarioId);
        cmd.Parameters.AddWithValue("producto", productoId);
        return (await cmd.ExecuteScalarAsync())?.ToString();
    }

    public async Task<bool> EliminarAsync(string id)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = "delete from public.favoritos where id = @id";
        cmd.Parameters.AddWithValue("id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
