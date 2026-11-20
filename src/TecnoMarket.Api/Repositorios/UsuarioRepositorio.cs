using Npgsql;
using TecnoMarket.Api.Data;
using TecnoMarket.Shared;

namespace TecnoMarket.Api.Repositorios;

public sealed class UsuarioRepositorio
{
    private readonly SupabaseDb _db;
    public UsuarioRepositorio(SupabaseDb db) => _db = db;

    private const string Columnas = @"
        select id, nombre, apellido,
               coalesce(email, '') as email,
               coalesce(telefono, '') as telefono,
               coalesce(ciudad, '') as ciudad,
               coalesce(fecha_registro::text, '') as fecha_registro,
               coalesce(rol, 'usuario') as rol
        from public.usuarios";

    public async Task<List<Usuario>> ListarAsync()
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = Columnas + "\norder by fecha_registro desc nulls last, nombre";
        await using var lector = await cmd.ExecuteReaderAsync();
        var lista = new List<Usuario>();
        while (await lector.ReadAsync()) lista.Add(Leer(lector));
        return lista;
    }

    public async Task<Usuario?> PorIdAsync(string id)
    {
        await using var cn = await _db.AbrirAsync();
        await using var cmd = cn.CreateCommand();
        cmd.CommandText = Columnas + "\nwhere id = @id limit 1";
        cmd.Parameters.AddWithValue("id", id);
        await using var lector = await cmd.ExecuteReaderAsync();
        return await lector.ReadAsync() ? Leer(lector) : null;
    }

    private static Usuario Leer(NpgsqlDataReader lector) => new()
    {
        Id = lector.Texto("id"),
        Nombre = lector.Texto("nombre"),
        Apellido = lector.Texto("apellido"),
        Email = lector.TextoNulo("email"),
        Telefono = lector.TextoNulo("telefono"),
        Ciudad = lector.TextoNulo("ciudad"),
        FechaRegistro = lector.TextoNulo("fecha_registro"),
        Rol = lector.Texto("rol")
    };
}
