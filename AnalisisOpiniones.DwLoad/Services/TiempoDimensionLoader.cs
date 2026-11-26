using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class TiempoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Tiempo";
    protected override string TablaDestino => "Dim_Tiempo";

    protected override string SqlCarga => @"
INSERT INTO dbo.Dim_Tiempo (TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana)
SELECT x.FechaKey, x.Fecha, x.Anio, x.Trimestre, x.Mes,
       CASE x.Mes
           WHEN 1 THEN 'Enero' WHEN 2 THEN 'Febrero' WHEN 3 THEN 'Marzo'
           WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo' WHEN 6 THEN 'Junio'
           WHEN 7 THEN 'Julio' WHEN 8 THEN 'Agosto' WHEN 9 THEN 'Septiembre'
           WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre' ELSE 'Diciembre'
       END,
       x.Dia,
       CASE x.DiaSemana
           WHEN 1 THEN 'Lunes' WHEN 2 THEN 'Martes' WHEN 3 THEN 'Miércoles'
           WHEN 4 THEN 'Jueves' WHEN 5 THEN 'Viernes' WHEN 6 THEN 'Sábado'
           ELSE 'Domingo'
       END
FROM (
    SELECT DISTINCT
        YEAR(f.Fecha) * 10000 + MONTH(f.Fecha) * 100 + DAY(f.Fecha) AS FechaKey,
        f.Fecha,
        YEAR(f.Fecha) AS Anio,
        DATEPART(QUARTER, f.Fecha) AS Trimestre,
        MONTH(f.Fecha) AS Mes,
        DAY(f.Fecha) AS Dia,
        ((DATEPART(WEEKDAY, f.Fecha) + @@DATEFIRST - 2) % 7) + 1 AS DiaSemana
    FROM (
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Comentarios_Sociales WHERE Fecha IS NOT NULL
        UNION
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Resenas_Web WHERE Fecha IS NOT NULL
        UNION
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Encuestas WHERE Fecha IS NOT NULL
    ) f
) x
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Tiempo t WHERE t.TiempoKey = x.FechaKey);";

    public TiempoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<TiempoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }
}
