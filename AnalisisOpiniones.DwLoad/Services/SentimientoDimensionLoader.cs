using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class SentimientoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Sentimiento";
    protected override string TablaDestino => "Dim_Sentimiento";

    private static readonly (int Key, string Clasificacion)[] Semillas =
    [
        (1, "Positiva"),
        (2, "Neutra"),
        (3, "Negativa"),
        (4, "No clasificado")
    ];

    public SentimientoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<SentimientoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }

    protected override async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        var existentes = await ObtenerClavesIntExistentesAsync("SentimientoKey", ct);
        var nuevos = Semillas.Where(s => !existentes.Contains(s.Key)).ToList();
        if (nuevos.Count == 0) return 0;

        await using var cn = new SqlConnection(DwConnection);
        await cn.OpenAsync(ct);

        int afectados = 0;
        foreach (var s in nuevos)
        {
            await using var cmd = new SqlCommand(
                "INSERT INTO dbo.Dim_Sentimiento (SentimientoKey, Clasificacion) VALUES (@k, @c)", cn);
            cmd.Parameters.AddWithValue("@k", s.Key);
            cmd.Parameters.AddWithValue("@c", s.Clasificacion);
            afectados += await cmd.ExecuteNonQueryAsync(ct);
        }

        return afectados;
    }
}
