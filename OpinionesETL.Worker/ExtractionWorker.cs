using OpinionesETL.Negocio;

/// <summary>
/// Worker Service: al arrancar ejecuta el proceso de extracción una vez y luego detiene la aplicación.
/// (Si se quisiera ejecutar periódicamente, bastaría con un ciclo con PeriodicTimer.)
/// </summary>
public class ExtractionWorker : BackgroundService
{
    private readonly ExtractionOrchestrator _orquestador;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(
        ExtractionOrchestrator orquestador,
        IHostApplicationLifetime lifetime,
        ILogger<ExtractionWorker> logger)
    {
        _orquestador = orquestador;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Worker Service iniciado: ejecutando el proceso de extracción");

            var resumenes = await _orquestador.EjecutarAsync(stoppingToken);

            var fallidas = resumenes.Where(r => !r.Exitosa).Select(r => r.Fuente).ToList();
            if (fallidas.Count > 0)
                _logger.LogWarning("Extracción terminada con fuentes fallidas: {Fuentes}", string.Join(", ", fallidas));
            else
                _logger.LogInformation("Extracción terminada correctamente en todas las fuentes");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("La extracción fue cancelada");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error no controlado en el proceso de extracción");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
