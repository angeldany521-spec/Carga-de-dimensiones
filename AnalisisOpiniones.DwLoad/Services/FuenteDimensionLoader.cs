using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class FuenteDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Fuente";
    protected override string TablaDestino => "Dim_Fuente";

    public FuenteDimensionLoader(IOptions<DwLoadOptions> options, ILogger<FuenteDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }

    protected override async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        var canalesExistentes = await ObtenerClavesExistentesAsync("Canal", ct);
        var fuentes = new List<(string? IdOrigen, string Canal, string Nombre, string Tipo)>();

        await using (var src = new SqlConnection(SourceConnection))
        {
            await src.OpenAsync(ct);
            await using var cmd = new SqlCommand(
                "SELECT IdFuente, TipoFuente FROM dbo.Fuentes_Datos", src);
            await using var rd = await cmd.ExecuteReaderAsync(ct);

            var porTipo = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (await rd.ReadAsync(ct))
            {
                var id = rd.GetValue(0).ToString();
                var tipo = rd.IsDBNull(1) ? "" : rd.GetString(1);
                if (!porTipo.ContainsKey(tipo) && id != null)
                    porTipo[tipo] = id;
            }

            if (porTipo.TryGetValue("CSV", out var idCsv) && !canalesExistentes.Contains("Encuesta"))
                fuentes.Add((idCsv, "Encuesta", "Encuesta Interna", "CSV"));
            if (porTipo.TryGetValue("Web", out var idWeb) && !canalesExistentes.Contains("Reseña Web"))
                fuentes.Add((idWeb, "Reseña Web", "Reseña Web", "Web"));
            if (porTipo.TryGetValue("Red Social", out var idRed) && !canalesExistentes.Contains("Red Social"))
                fuentes.Add((idRed, "Red Social", "Red Social", "Red Social"));
        }

        if (fuentes.Count == 0) return 0;

        await using var dw = new SqlConnection(DwConnection);
        await dw.OpenAsync(ct);
        int afectados = 0;
        foreach (var f in fuentes)
        {
            await using var ins = new SqlCommand(
                "INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente) VALUES (@id, @canal, @nombre, @tipo)", dw);
            ins.Parameters.AddWithValue("@id", (object?)f.IdOrigen ?? DBNull.Value);
            ins.Parameters.AddWithValue("@canal", f.Canal);
            ins.Parameters.AddWithValue("@nombre", f.Nombre);
            ins.Parameters.AddWithValue("@tipo", f.Tipo);
            afectados += await ins.ExecuteNonQueryAsync(ct);
        }

        return afectados;
    }
}
