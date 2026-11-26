using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class ClienteDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Cliente";
    protected override string TablaDestino => "Dim_Cliente";

    protected override string SqlCarga => @"
INSERT INTO dbo.Dim_Cliente (IdClienteOrigen, Nombre, Email)
SELECT c.IdCliente, c.Nombre, c.Email
FROM SistemaAnalisisOpiniones.dbo.Clientes c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Cliente d WHERE d.IdClienteOrigen = c.IdCliente);";

    public ClienteDimensionLoader(IOptions<DwLoadOptions> options, ILogger<ClienteDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }
}
