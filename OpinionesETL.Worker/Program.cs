using Microsoft.Extensions.Options;
using OpinionesETL.Datos.Extractores;
using OpinionesETL.Datos.Staging;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio;
using OpinionesETL.Negocio.Configuracion;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// La configuración se carga desde la carpeta del ejecutable, sin depender de desde dónde se lance el proceso.
builder.Configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
    optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile(
    Path.Combine(AppContext.BaseDirectory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
    optional: true, reloadOnChange: false);

// Credenciales (cadenas de conexión y API key): User Secrets, nunca el repositorio.
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

// Verificación temprana: si la sección Extraccion no cargó, se detiene con un mensaje claro.
if (string.IsNullOrWhiteSpace(builder.Configuration["Extraccion:Fuentes:Csv:RutaArchivo"]))
{
    Console.Error.WriteLine("ERROR: no se cargó la sección 'Extraccion' de appsettings.json.");
    Console.Error.WriteLine($"Carpeta donde se buscó: {AppContext.BaseDirectory}");
    return 1;
}

// Logging con Serilog (consola + archivo), configurado desde appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

// Configuración modular de las fuentes (sección "Extraccion")
builder.Services.Configure<ExtraccionOptions>(builder.Configuration.GetSection(ExtraccionOptions.Seccion));

// HttpClient nombrado para la API: URL base, timeout y clave en un solo lugar
builder.Services.AddHttpClient(ApiExtractor.NombreCliente, (sp, cliente) =>
{
    var api = sp.GetRequiredService<IOptions<ExtraccionOptions>>().Value.Fuentes.Api;
    cliente.BaseAddress = new Uri(api.UrlBase);
    cliente.Timeout = TimeSpan.FromSeconds(api.TimeoutSegundos);
    cliente.DefaultRequestHeaders.Add("X-Api-Key", api.ApiKey);
});

// Extractores: cada fuente implementa IExtractor<T>
builder.Services.AddTransient<IExtractor<EncuestaDto>, CsvExtractor>();
builder.Services.AddTransient<IExtractor<ResenaDto>, DatabaseExtractor>();
builder.Services.AddTransient<IExtractor<ComentarioDto>, ApiExtractor>();

// Carga a staging y orquestador
builder.Services.AddSingleton<IStagingLoader, StagingLoader>();
builder.Services.AddSingleton<ExtractionOrchestrator>();

builder.Services.AddHostedService<ExtractionWorker>();

var host = builder.Build();

try
{
    await host.RunAsync();
}
finally
{
    Log.CloseAndFlush();
}

return 0;