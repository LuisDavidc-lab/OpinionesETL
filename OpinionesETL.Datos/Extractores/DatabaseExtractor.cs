using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio;
using OpinionesETL.Negocio.Configuracion;

namespace OpinionesETL.Datos.Extractores;

/// <summary>Extrae las reseñas web desde la BD relacional de origen (ADO.NET, lectura asíncrona).</summary>
public class DatabaseExtractor : IExtractor<ResenaDto>
{
    private readonly BaseDatosOptions _opciones;
    private readonly string _connectionString;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(
        IOptions<ExtraccionOptions> opciones,
        IConfiguration configuration,
        ILogger<DatabaseExtractor> logger)
    {
        _opciones = opciones.Value.Fuentes.BaseDatos;
        _connectionString = configuration.GetConnectionString("OrigenDb") ?? string.Empty;
        _logger = logger;
    }

    public string NombreFuente => "BaseDatos";

    public async Task<ResultadoExtraccion<ResenaDto>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion<ResenaDto> { Fuente = NombreFuente };

        try
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Falta la cadena de conexión 'OrigenDb' (configúrala con User Secrets).");

            _logger.LogInformation("[{Fuente}] Iniciando extracción desde la base de datos de origen", NombreFuente);

            var lista = new List<ResenaDto>();

            await using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync(cancellationToken);

            await using var comando = new SqlCommand(_opciones.Consulta, conexion)
            {
                CommandTimeout = _opciones.TimeoutSegundos
            };
            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

            int oId = lector.GetOrdinal("IdResena");
            int oCliente = lector.GetOrdinal("Cliente");
            int oProducto = lector.GetOrdinal("Producto");
            int oTitulo = lector.GetOrdinal("Titulo");
            int oComentario = lector.GetOrdinal("Comentario");
            int oEstrellas = lector.GetOrdinal("Estrellas");
            int oVerificada = lector.GetOrdinal("CompraVerificada");
            int oFecha = lector.GetOrdinal("Fecha");

            while (await lector.ReadAsync(cancellationToken))
            {
                lista.Add(new ResenaDto
                {
                    IdResenaOrigen = lector.GetInt32(oId),
                    Cliente = lector.IsDBNull(oCliente) ? null : lector.GetString(oCliente),
                    Producto = lector.GetString(oProducto),
                    Titulo = lector.IsDBNull(oTitulo) ? null : lector.GetString(oTitulo),
                    Comentario = lector.IsDBNull(oComentario) ? null : lector.GetString(oComentario),
                    Estrellas = lector.GetByte(oEstrellas),
                    CompraVerificada = lector.GetBoolean(oVerificada),
                    Fecha = lector.GetDateTime(oFecha)
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
