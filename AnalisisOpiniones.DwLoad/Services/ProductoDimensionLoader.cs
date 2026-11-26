using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class ProductoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Producto";
    protected override string TablaDestino => "Dim_Producto";

    protected override string SqlCarga => @"
INSERT INTO dbo.Dim_Producto (IdProductoOrigen, Nombre, Categoria)
SELECT p.IdProducto, p.Nombre, c.NombreCategoria
FROM SistemaAnalisisOpiniones.dbo.Productos p
LEFT JOIN SistemaAnalisisOpiniones.dbo.Categorias c ON c.IdCategoria = p.IdCategoria
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Producto d WHERE d.IdProductoOrigen = p.IdProducto);";

    public ProductoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<ProductoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }
}
