using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio.Configuracion;

namespace OpinionesETL.Negocio;

/// <summary>Resumen de lo ocurrido con una fuente durante una carga.</summary>
public record ResumenFuente(string Fuente, int Extraidos, int Cargados, bool Exitosa, TimeSpan Duracion, string? Mensaje);

/// <summary>
/// Orquesta la extracción: ejecuta cada fuente habilitada (en paralelo si así se configura),
/// la guarda en staging y registra el resultado. Si una fuente falla, las demás continúan.
/// </summary>
public class ExtractionOrchestrator
{
    private readonly IExtractor<EncuestaDto> _extractorCsv;
    private readonly IExtractor<ResenaDto> _extractorBd;
    private readonly IExtractor<ComentarioDto> _extractorApi;
    private readonly IStagingLoader _loader;
    private readonly ExtraccionOptions _opciones;
    private readonly ILogger<ExtractionOrchestrator> _logger;

    public ExtractionOrchestrator(
        IExtractor<EncuestaDto> extractorCsv,
        IExtractor<ResenaDto> extractorBd,
        IExtractor<ComentarioDto> extractorApi,
        IStagingLoader loader,
        IOptions<ExtraccionOptions> opciones,
        ILogger<ExtractionOrchestrator> logger)
    {
        _extractorCsv = extractorCsv;
        _extractorBd = extractorBd;
        _extractorApi = extractorApi;
        _loader = loader;
        _opciones = opciones.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResumenFuente>> EjecutarAsync(CancellationToken cancellationToken)
    {
        var idCarga = Guid.NewGuid();
        var total = Stopwatch.StartNew();
        var fuentes = _opciones.Fuentes;

        _logger.LogInformation("=== Inicio de extracción | IdCarga={IdCarga} | Paralelo={Paralelo} ===",
            idCarga, _opciones.EjecucionParalela);

        var tareas = new List<Func<Task<ResumenFuente>>>();

        if (fuentes.Csv.Habilitada)
            tareas.Add(() => ProcesarAsync(_extractorCsv,
                datos => _loader.CargarEncuestasAsync(idCarga, Path.GetFullPath(fuentes.Csv.RutaArchivo), datos, cancellationToken),
                idCarga, cancellationToken));

        if (fuentes.BaseDatos.Habilitada)
            tareas.Add(() => ProcesarAsync(_extractorBd,
                datos => _loader.CargarResenasAsync(idCarga, datos, cancellationToken),
                idCarga, cancellationToken));

        if (fuentes.Api.Habilitada)
            tareas.Add(() => ProcesarAsync(_extractorApi,
                datos => _loader.CargarComentariosAsync(idCarga, datos, cancellationToken),
                idCarga, cancellationToken));

        var resumenes = new List<ResumenFuente>();

        if (_opciones.EjecucionParalela)
        {
            var iniciadas = tareas.Select(t => t()).ToList();
            resumenes.AddRange(await Task.WhenAll(iniciadas));
        }
        else
        {
            foreach (var tarea in tareas)
                resumenes.Add(await tarea());
        }

        total.Stop();

        foreach (var r in resumenes)
        {
            _logger.LogInformation("Resumen [{Fuente}] extraídos={Extraidos} cargados={Cargados} estado={Estado} duración={Ms} ms {Mensaje}",
                r.Fuente, r.Extraidos, r.Cargados, r.Exitosa ? "Exitosa" : "Fallida",
                (long)r.Duracion.TotalMilliseconds, r.Mensaje);
        }

        _logger.LogInformation("=== Fin de extracción | Total {Ms} ms | Fuentes OK: {Ok}/{Total} ===",
            total.ElapsedMilliseconds, resumenes.Count(r => r.Exitosa), resumenes.Count);

        return resumenes;
    }

    private async Task<ResumenFuente> ProcesarAsync<T>(
        IExtractor<T> extractor,
        Func<IReadOnlyList<T>, Task<int>> cargar,
        Guid idCarga,
        CancellationToken cancellationToken)
    {
        var resultado = await extractor.ExtraerAsync(cancellationToken);

        var exitosa = resultado.Exitosa;
        var mensaje = resultado.Mensaje;
        var cargados = 0;

        if (resultado.Exitosa)
        {
            try
            {
                cargados = await cargar(resultado.Registros);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                exitosa = false;
                mensaje = $"Error al cargar a staging: {ex.Message}";
                _logger.LogError(ex, "[{Fuente}] Falló la carga a staging", extractor.NombreFuente);
            }
        }

        try
        {
            await _loader.RegistrarControlAsync(idCarga, extractor.NombreFuente, cargados,
                (long)resultado.Duracion.TotalMilliseconds, exitosa, mensaje, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{Fuente}] No se pudo registrar en Extraccion_Control", extractor.NombreFuente);
        }

        return new ResumenFuente(extractor.NombreFuente, resultado.Registros.Count, cargados,
            exitosa, resultado.Duracion, mensaje);
    }
}
