using System.Diagnostics;
using AnalisisOpiniones.DwLoad.Abstractions;
using Microsoft.Extensions.Logging;

namespace AnalisisOpiniones.DwLoad.Services;

public sealed class DimensionLoadRunner
{
    private readonly IEnumerable<IDimensionLoader> _loaders;
    private readonly ILogger<DimensionLoadRunner> _logger;

    public DimensionLoadRunner(IEnumerable<IDimensionLoader> loaders, ILogger<DimensionLoadRunner> logger)
    {
        _loaders = loaders;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Iniciando carga de dimensiones del Data Warehouse");
        var reloj = Stopwatch.StartNew();
        var resultados = new List<DimensionLoadResult>();

        foreach (var loader in _loaders)
        {
            _logger.LogInformation("Cargando {Dimension}...", loader.Nombre);
            var resultado = await loader.LoadAsync(ct);
            resultados.Add(resultado);
        }

        reloj.Stop();

        Console.WriteLine();
        Console.WriteLine("=== RESUMEN CARGA DE DIMENSIONES ===");
        Console.WriteLine($"{"Dimension",-20} {"Estado",-10} {"Afectados",10} {"Total",10} {"Tiempo ms",12}");
        Console.WriteLine(new string('-', 66));

        foreach (var r in resultados)
        {
            var estado = r.Exitoso ? "OK" : "ERROR";
            Console.WriteLine($"{r.Dimension,-20} {estado,-10} {r.RegistrosAfectados,10} {r.TotalEnTabla,10} {r.TiempoMs,12}");
            if (!r.Exitoso && !string.IsNullOrWhiteSpace(r.Error))
                Console.WriteLine($"  -> {r.Error}");
        }

        Console.WriteLine(new string('-', 66));
        Console.WriteLine($"Total: {resultados.Count(r => r.Exitoso)}/{resultados.Count} dimensiones cargadas en {reloj.ElapsedMilliseconds} ms");

        return resultados.All(r => r.Exitoso) ? 0 : 1;
    }
}
