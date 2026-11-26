using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class FuenteDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Fuente";
    protected override string TablaDestino => "Dim_Fuente";

    protected override string SqlCarga => @"
INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Encuesta', 'Encuesta Interna', 'CSV'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'CSV'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Encuesta')
HAVING COUNT(*) > 0;

INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Reseña Web', 'Reseña Web', 'Web'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'Web'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Reseña Web')
HAVING COUNT(*) > 0;

INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Red Social', 'Red Social', 'Red Social'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'Red Social'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Red Social')
HAVING COUNT(*) > 0;";

    public FuenteDimensionLoader(IOptions<DwLoadOptions> options, ILogger<FuenteDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }
}
