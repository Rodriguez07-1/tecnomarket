using Npgsql;

namespace TecnoMarket.Api.Data;

/// <summary>Conexión a la misma base de datos PostgreSQL que usa Supabase.</summary>
public sealed class SupabaseDb
{
    private readonly string _cadena;

    public SupabaseDb(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("Supabase") ?? string.Empty;
    }

    public bool Configurada =>
        !string.IsNullOrWhiteSpace(_cadena) &&
        !_cadena.Contains("COLOCA_AQUI", StringComparison.OrdinalIgnoreCase);

    public async Task<NpgsqlConnection> AbrirAsync()
    {
        if (!Configurada)
        {
            throw new InvalidOperationException(
                "Falta la contraseña de Supabase. Edita ConnectionStrings:Supabase en appsettings.json " +
                "(Supabase > Project Settings > Database > Connection string).");
        }

        var conexion = new NpgsqlConnection(_cadena);
        await conexion.OpenAsync();
        return conexion;
    }
}

public static class LectorSeguro
{
    public static string Texto(this NpgsqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        return lector.IsDBNull(indice) ? string.Empty : lector.GetString(indice);
    }

    public static string? TextoNulo(this NpgsqlDataReader lector, string columna)
    {
        var indice = lector.GetOrdinal(columna);
        return lector.IsDBNull(indice) ? null : lector.GetString(indice);
    }
}
