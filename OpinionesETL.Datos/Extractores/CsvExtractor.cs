using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio;
using OpinionesETL.Negocio.Configuracion;

namespace OpinionesETL.Datos.Extractores;

/// <summary>Extrae las encuestas internas desde un archivo CSV usando CsvHelper.
/// Valida la estructura (columnas requeridas) y entrega los datos crudos sin transformar.</summary>
public class CsvExtractor : IExtractor<EncuestaDto>
{
    private static readonly string[] ColumnasRequeridas =
        { "fecha", "email_cliente", "producto", "canal_encuesta", "nota_general", "comentario" };

    private readonly CsvOptions _opciones;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<ExtraccionOptions> opciones, ILogger<CsvExtractor> logger)
    {
        _opciones = opciones.Value.Fuentes.Csv;
        _logger = logger;
    }

    public string NombreFuente => "CSV";

    public async Task<ResultadoExtraccion<EncuestaDto>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion<EncuestaDto> { Fuente = NombreFuente };

        try
        {
            var ruta = Path.GetFullPath(_opciones.RutaArchivo);
            _logger.LogInformation("[{Fuente}] Iniciando extracción desde {Ruta}", NombreFuente, ruta);

            if (!File.Exists(ruta))
                throw new FileNotFoundException($"No se encontró el archivo CSV en: {ruta}", ruta);

            var configuracion = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = _opciones.Delimitador,
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,
                HeaderValidated = null,
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
                ShouldSkipRecord = args => args.Row.Parser.Record is null
                                           || args.Row.Parser.Record.All(string.IsNullOrWhiteSpace),
                BadDataFound = args => _logger.LogWarning(
                    "[{Fuente}] Dato mal formado cerca de la fila {Fila}", NombreFuente, args.Context.Parser?.Row)
            };

            using var lector = new StreamReader(ruta, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(lector, configuracion);

            await csv.ReadAsync();
            csv.ReadHeader();

            var encabezados = (csv.HeaderRecord ?? Array.Empty<string>())
                .Select(h => h.Trim().ToLowerInvariant()).ToHashSet();
            var faltantes = ColumnasRequeridas.Where(c => !encabezados.Contains(c)).ToList();
            if (faltantes.Count > 0)
                throw new InvalidDataException($"Estructura inválida: faltan las columnas {string.Join(", ", faltantes)}.");

            var lista = new List<EncuestaDto>();
            while (await csv.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                lista.Add(new EncuestaDto
                {
                    NumeroFila = csv.Parser.Row,
                    Fecha = csv.GetField("fecha"),
                    EmailCliente = csv.GetField("email_cliente"),
                    Producto = csv.GetField("producto"),
                    CanalEncuesta = csv.GetField("canal_encuesta"),
                    NotaGeneral = csv.GetField("nota_general"),
                    Comentario = csv.GetField("comentario")
                });
            }

            resultado.Registros = lista;
            resultado.Exitosa = true;
            _logger.LogInformation("[{Fuente}] {Cantidad} registros extraídos", NombreFuente, lista.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            resultado.Exitosa = false;
            resultado.Mensaje = ex.Message;
            _logger.LogError(ex, "[{Fuente}] Error durante la extracción", NombreFuente);
        }
        finally
        {
            sw.Stop();
            resultado.Duracion = sw.Elapsed;
            _logger.LogInformation("[{Fuente}] Duración: {Ms} ms", NombreFuente, sw.ElapsedMilliseconds);
        }

        return resultado;
    }
}
