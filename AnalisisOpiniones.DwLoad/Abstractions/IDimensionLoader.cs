namespace AnalisisOpiniones.DwLoad.Abstractions;

public interface IDimensionLoader
{
    string Nombre { get; }
    Task<DimensionLoadResult> LoadAsync(CancellationToken cancellationToken = default);
}

public sealed class DimensionLoadResult
{
    public string Dimension { get; init; } = string.Empty;
    public bool Exitoso { get; init; }
    public int RegistrosAfectados { get; init; }
    public int TotalEnTabla { get; init; }
    public long TiempoMs { get; init; }
    public string? Error { get; init; }
}
