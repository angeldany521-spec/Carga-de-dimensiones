using AnalisisOpiniones.DwLoad.Abstractions;
using AnalisisOpiniones.DwLoad.Options;
using AnalisisOpiniones.DwLoad.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Services.Configure<DwLoadOptions>(builder.Configuration.GetSection("DwLoad"));

builder.Services.AddSingleton<IDimensionLoader, SentimientoDimensionLoader>();
builder.Services.AddSingleton<IDimensionLoader, TiempoDimensionLoader>();
builder.Services.AddSingleton<IDimensionLoader, ClienteDimensionLoader>();
builder.Services.AddSingleton<IDimensionLoader, ProductoDimensionLoader>();
builder.Services.AddSingleton<IDimensionLoader, FuenteDimensionLoader>();
builder.Services.AddSingleton<DimensionLoadRunner>();

using var host = builder.Build();

var runner = host.Services.GetRequiredService<DimensionLoadRunner>();
var codigo = await runner.RunAsync();
Environment.ExitCode = codigo;
