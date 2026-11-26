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
    protected abstract string SqlCarga { get; }

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

    protected virtual async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(SqlCarga, cn);
        cmd.CommandTimeout = 120;
        var result = await cmd.ExecuteNonQueryAsync(ct);
        return result < 0 ? 0 : result;
    }

    protected async Task<int> ContarTablaAsync(CancellationToken ct)
    {
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand($"SELECT COUNT(*) FROM dbo.{TablaDestino}", cn);
        var obj = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt32(obj);
    }

    protected async Task<int> EjecutarEnDwAsync(string sql, CancellationToken ct)
    {
        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.CommandTimeout = 120;
        var result = await cmd.ExecuteNonQueryAsync(ct);
        return result < 0 ? 0 : result;
    }
}
