using AnalisisOpiniones.DwLoad.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class ClienteDimensionLoader : SqlDimensionLoader
{
    public override string Nombre => "Dim_Cliente";
    protected override string TablaDestino => "Dim_Cliente";

    public ClienteDimensionLoader(IOptions<DwLoadOptions> options, ILogger<ClienteDimensionLoader> logger)
        : base(options.Value.SourceConnectionString, options.Value.DwConnectionString, logger) { }

    protected override async Task<int> EjecutarCargaAsync(CancellationToken ct)
    {
        var existentes = await ObtenerClavesExistentesAsync("IdClienteOrigen", ct);
        var clientes = new List<(string Id, string? Nombre, string? Email)>();

        await using (var src = new SqlConnection(SourceConnection))
        {
            await src.OpenAsync(ct);
            await using var cmd = new SqlCommand(
                "SELECT IdCliente, Nombre, Email FROM dbo.Clientes", src);
            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var id = rd.GetValue(0).ToString() ?? "";
                if (existentes.Contains(id)) continue;
                clientes.Add((id, rd.IsDBNull(1) ? null : rd.GetString(1), rd.IsDBNull(2) ? null : rd.GetString(2)));
            }
        }

        if (clientes.Count == 0) return 0;

        await using var dw = new SqlConnection(DwConnection);
        await dw.OpenAsync(ct);
        int afectados = 0;
        foreach (var c in clientes)
        {
            await using var ins = new SqlCommand(
                "INSERT INTO dbo.Dim_Cliente (IdClienteOrigen, Nombre, Email) VALUES (@id, @nombre, @email)", dw);
            ins.Parameters.AddWithValue("@id", c.Id);
            ins.Parameters.AddWithValue("@nombre", (object?)c.Nombre ?? DBNull.Value);
            ins.Parameters.AddWithValue("@email", (object?)c.Email ?? DBNull.Value);
            afectados += await ins.ExecuteNonQueryAsync(ct);
        }

        return afectados;
    }
}
