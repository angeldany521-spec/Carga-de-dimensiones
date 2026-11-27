using System.Diagnostics;
using AnalisisOpiniones.DwLoad.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.DwLoad.Services;

public abstract class SqlDimensionLoader : IDimensionLoader
{
    protected readonly string SourceConnection;
    protected readonly string DwConnection;
    protected readonly ILogger Logger;

    public abstract string Nombre { get; }
    protected abstract string TablaDestino { get; }

    protected SqlDimensionLoader(string sourceConnection, string dwConnection, ILogger logger)
    {
        SourceConnection = sourceConnection;
        DwConnection = dwConnection;
        Logger = logger;
    }

    public async Task<DimensionLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        var reloj = Stopwatch.StartNew();
        try
        {
            int afectados = await EjecutarCargaAsync(cancellationToken);
            int total = await ContarTablaAsync(cancellationToken);
            reloj.Stop();

            Logger.LogInformation("{Dimension}: {Afectados} registros afectados, {Total} registros en tabla, {Tiempo} ms",
                Nombre, afectados, total, reloj.ElapsedMilliseconds);

            return new DimensionLoadResult
            {
                Dimension = Nombre,
                Exitoso = true,
                RegistrosAfectados = afectados,
                TotalEnTabla = total,
                TiempoMs = reloj.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            reloj.Stop();
            Logger.LogError(ex, "Error al cargar {Dimension}", Nombre);
            return new DimensionLoadResult
            {
                Dimension = Nombre,
                Exitoso = false,
                TiempoMs = reloj.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    protected abstract Task<int> EjecutarCargaAsync(CancellationToken ct);

    protected async Task<int> ContarTablaAsync(CancellationToken ct)
    {
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.{TablaDestino}", cn);
        var obj = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(obj);
    }

    protected async Task<HashSet<string>> ObtenerClavesExistentesAsync(string columna, CancellationToken ct)
    {
        var claves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT CAST({columna} AS NVARCHAR(100)) FROM dbo.{TablaDestino}", cn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            claves.Add(reader.GetString(0));
        return claves;
    }

    protected async Task<HashSet<int>> ObtenerClavesIntExistentesAsync(string columna, CancellationToken ct)
    {
        var claves = new HashSet<int>();
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT {columna} FROM dbo.{TablaDestino}", cn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            claves.Add(reader.GetInt32(0));
        return claves;
    }
}
