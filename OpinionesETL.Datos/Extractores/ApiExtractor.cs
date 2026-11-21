using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio;
using OpinionesETL.Negocio.Configuracion;

namespace OpinionesETL.Datos.Extractores;

/// <summary>Consume los comentarios de redes sociales desde la API REST (IHttpClientFactory + reintentos).</summary>
public class ApiExtractor : IExtractor<ComentarioDto>
{
    /// <summary>Nombre del HttpClient configurado en Program.cs (URL base, timeout y X-Api-Key).</summary>
    public const string NombreCliente = "ApiComentarios";

    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ApiOptions _opciones;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(
        IHttpClientFactory httpClientFactory,
        IOptions<ExtraccionOptions> opciones,
        ILogger<ApiExtractor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _opciones = opciones.Value.Fuentes.Api;
        _logger = logger;
    }

    public string NombreFuente => "Api";

    public async Task<ResultadoExtraccion<ComentarioDto>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion<ComentarioDto> { Fuente = NombreFuente };

        _logger.LogInformation("[{Fuente}] Iniciando extracción desde {Url}{Ruta}",
            NombreFuente, _opciones.UrlBase, _opciones.Ruta);

        for (int intento = 1; intento <= _opciones.MaximoReintentos; intento++)
        {
            try
            {
                var cliente = _httpClientFactory.CreateClient(NombreCliente);
                using var respuesta = await cliente.GetAsync(_opciones.Ruta, cancellationToken);
                respuesta.EnsureSuccessStatusCode();

                var datos = await respuesta.Content
                    .ReadFromJsonAsync<List<ComentarioDto>>(OpcionesJson, cancellationToken)
                    ?? new List<ComentarioDto>();

                resultado.Registros = datos;
                resultado.Exitosa = true;
                _logger.LogInformation("[{Fuente}] {Cantidad} registros extraídos (intento {Intento})",
                    NombreFuente, datos.Count, intento);
                break;
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Clave inválida: reintentar no sirve de nada.
                resultado.Exitosa = false;
                resultado.Mensaje = "La API rechazó la clave (401 No autorizado).";
                _logger.LogError("[{Fuente}] {Mensaje}", NombreFuente, resultado.Mensaje);
                break;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException
                                       && !cancellationToken.IsCancellationRequested)
            {
                resultado.Exitosa = false;
                resultado.Mensaje = ex.Message;
                _logger.LogWarning("[{Fuente}] Intento {Intento}/{Max} falló: {Mensaje}",
                    NombreFuente, intento, _opciones.MaximoReintentos, ex.Message);

                if (intento < _opciones.MaximoReintentos)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, intento - 1)), cancellationToken);
                else
                    _logger.LogError(ex, "[{Fuente}] Se agotaron los reintentos", NombreFuente);
            }
        }

        sw.Stop();
        resultado.Duracion = sw.Elapsed;
        _logger.LogInformation("[{Fuente}] Duración: {Ms} ms", NombreFuente, sw.ElapsedMilliseconds);
        return resultado;
    }
}
