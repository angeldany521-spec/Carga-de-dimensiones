using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class ProductoDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Producto";
    protected override string TablaDestino => "Dim_Producto";

    public ProductoDimensionLoader(IOptions<DwLoadOptions> options, ILogger<ProductoDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }

    protected override async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        var existentes = await ObtenerClavesExistentesAsync("IdProductoOrigen", ct);
        var productos = new List<(string Id, string? Nombre, string? Categoria)>();

        await using (var src = new SqlConnection(SourceConnection))
        {
            await src.OpenAsync(ct);
            await using var cmd = new SqlCommand(@"
SELECT p.IdProducto, p.Nombre, c.NombreCategoria
FROM dbo.Productos p
LEFT JOIN dbo.Categorias c ON c.IdCategoria = p.IdCategoria", src);
            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var id = rd.GetValue(0).ToString() ?? "";
                if (existentes.Contains(id)) continue;
                productos.Add((
                    id,
                    rd.IsDBNull(1) ? null : rd.GetString(1),
                    rd.IsDBNull(2) ? null : rd.GetString(2)));
            }
        }

        if (productos.Count == 0) return 0;

        await using var dw = new SqlConnection(DwConnection);
        await dw.OpenAsync(ct);
        int afectados = 0;
        foreach (var p in productos)
        {
            await using var ins = new SqlCommand(
                "INSERT INTO dbo.Dim_Producto (IdProductoOrigen, Nombre, Categoria) VALUES (@id, @nombre, @cat)", dw);
            ins.Parameters.AddWithValue("@id", p.Id);
            ins.Parameters.AddWithValue("@nombre", (object?)p.Nombre ?? DBNull.Value);
            ins.Parameters.AddWithValue("@cat", (object?)p.Categoria ?? DBNull.Value);
            afectados += await ins.ExecuteNonQueryAsync(ct);
        }

        return afectados;
    }
}
