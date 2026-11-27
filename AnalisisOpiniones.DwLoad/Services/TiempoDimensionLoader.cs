using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class TiempoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Tiempo";
    protected override string TablaDestino => "Dim_Tiempo";

    public TiempoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<TiempoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }

    protected override async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        var existentes = await ObtenerClavesIntExistentesAsync("TiempoKey", ct);
        var fechas = new HashSet<DateTime>();

        await using (var src = new SqlConnection(SourceConnection))
        {
            await src.OpenAsync(ct);
            var sql = @"
SELECT Fecha FROM dbo.Comentarios_Sociales WHERE Fecha IS NOT NULL
UNION
SELECT Fecha FROM dbo.Resenas_Web WHERE Fecha IS NOT NULL
UNION
SELECT Fecha FROM dbo.Encuestas WHERE Fecha IS NOT NULL";
            await using var cmd = new SqlCommand(sql, src);
            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
                fechas.Add(rd.GetDateTime(0).Date);
        }

        var nuevos = fechas
            .Select(f =>
            {
                int key = f.Year * 10000 + f.Month * 100 + f.Day;
                return (Key: key, Fecha: f);
            })
            .Where(x => !existentes.Contains(x.Key))
            .ToList();

        if (nuevos.Count == 0) return 0;

        await using var dw = new SqlConnection(DwConnection);
        await dw.OpenAsync(ct);
        int afectados = 0;

        foreach (var n in nuevos)
        {
            var f = n.Fecha;
            int trimestre = (f.Month - 1) / 3 + 1;
            string nombreMes = f.ToString("MMMM", new System.Globalization.CultureInfo("es-ES"));
            nombreMes = char.ToUpper(nombreMes[0]) + nombreMes[1..];
            string diaSemana = f.ToString("dddd", new System.Globalization.CultureInfo("es-ES"));
            diaSemana = char.ToUpper(diaSemana[0]) + diaSemana[1..];

            await using var ins = new SqlCommand(@"
INSERT INTO dbo.Dim_Tiempo (TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana)
VALUES (@key, @fecha, @anio, @tri, @mes, @nombreMes, @dia, @diaSemana)", dw);
            ins.Parameters.AddWithValue("@key", n.Key);
            ins.Parameters.AddWithValue("@fecha", f);
            ins.Parameters.AddWithValue("@anio", f.Year);
            ins.Parameters.AddWithValue("@tri", trimestre);
            ins.Parameters.AddWithValue("@mes", f.Month);
            ins.Parameters.AddWithValue("@nombreMes", nombreMes);
            ins.Parameters.AddWithValue("@dia", f.Day);
            ins.Parameters.AddWithValue("@diaSemana", diaSemana);
            afectados += await ins.ExecuteNonQueryAsync(ct);
        }

        return afectados;
    }
}
