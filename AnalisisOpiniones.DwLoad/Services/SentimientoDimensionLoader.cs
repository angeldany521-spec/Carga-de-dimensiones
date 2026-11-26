using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class SentimientoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Sentimiento";
    protected override string TablaDestino => "Dim_Sentimiento";

    protected override string SqlCarga => @"
IF NOT EXISTS (SELECT 1 FROM dbo.Dim_Sentimiento)
INSERT INTO dbo.Dim_Sentimiento (SentimientoKey, Clasificacion) VALUES
    (1, 'Positiva'),
    (2, 'Neutra'),
    (3, 'Negativa'),
    (4, 'No clasificado');";

    public SentimientoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<SentimientoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }
}
