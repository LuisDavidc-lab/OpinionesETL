using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpinionesETL.Entidades;
using OpinionesETL.Negocio;

namespace OpinionesETL.Datos.Staging;

/// <summary>Guarda los datos crudos en las tablas Stg_* de la BD analítica usando SqlBulkCopy
/// (carga masiva: mucho más rápido que insertar fila por fila).</summary>
public class StagingLoader : IStagingLoader
{
    private readonly string _connectionString;
    private readonly ILogger<StagingLoader> _logger;

    public StagingLoader(IConfiguration configuration, ILogger<StagingLoader> logger)
    {
        _connectionString = configuration.GetConnectionString("AnaliticaDb") ?? string.Empty;
        _logger = logger;
    }

    public Task<int> CargarEncuestasAsync(Guid idCarga, string archivoOrigen, IReadOnlyList<EncuestaDto> datos, CancellationToken cancellationToken)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("IdCarga", typeof(Guid));
        tabla.Columns.Add("ArchivoOrigen", typeof(string));
        tabla.Columns.Add("NumeroFila", typeof(int));
        tabla.Columns.Add("Fecha", typeof(string));
        tabla.Columns.Add("EmailCliente", typeof(string));
        tabla.Columns.Add("Producto", typeof(string));
        tabla.Columns.Add("CanalEncuesta", typeof(string));
        tabla.Columns.Add("NotaGeneral", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));

        foreach (var d in datos)
            tabla.Rows.Add(idCarga, archivoOrigen, d.NumeroFila, Nulo(d.Fecha), Nulo(d.EmailCliente),
                Nulo(d.Producto), Nulo(d.CanalEncuesta), Nulo(d.NotaGeneral), Nulo(d.Comentario));

        return CargarMasivaAsync("dbo.Stg_Encuestas", tabla, cancellationToken);
    }

    public Task<int> CargarResenasAsync(Guid idCarga, IReadOnlyList<ResenaDto> datos, CancellationToken cancellationToken)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("IdCarga", typeof(Guid));
        tabla.Columns.Add("IdResenaOrigen", typeof(int));
        tabla.Columns.Add("Cliente", typeof(string));
        tabla.Columns.Add("Producto", typeof(string));
        tabla.Columns.Add("Titulo", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("Estrellas", typeof(string));
        tabla.Columns.Add("CompraVerificada", typeof(string));
        tabla.Columns.Add("Fecha", typeof(string));

        foreach (var d in datos)
            tabla.Rows.Add(idCarga, d.IdResenaOrigen, Nulo(d.Cliente), Nulo(d.Producto), Nulo(d.Titulo),
                Nulo(d.Comentario), d.Estrellas.ToString(), d.CompraVerificada ? "1" : "0",
                d.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));

        return CargarMasivaAsync("dbo.Stg_ResenasWeb", tabla, cancellationToken);
    }

    public Task<int> CargarComentariosAsync(Guid idCarga, IReadOnlyList<ComentarioDto> datos, CancellationToken cancellationToken)
    {
        var tabla = new DataTable();
        tabla.Columns.Add("IdCarga", typeof(Guid));
        tabla.Columns.Add("IdExterno", typeof(string));
        tabla.Columns.Add("Plataforma", typeof(string));
        tabla.Columns.Add("UsuarioSocial", typeof(string));
        tabla.Columns.Add("Producto", typeof(string));
        tabla.Columns.Add("Comentario", typeof(string));
        tabla.Columns.Add("Likes", typeof(string));
        tabla.Columns.Add("UrlPublicacion", typeof(string));
        tabla.Columns.Add("Fecha", typeof(string));

        foreach (var d in datos)
            tabla.Rows.Add(idCarga, Nulo(d.Id), Nulo(d.Plataforma), Nulo(d.Usuario), Nulo(d.Producto),
                Nulo(d.Texto), d.Likes.ToString(), Nulo(d.Url), d.Fecha.ToString("yyyy-MM-dd HH:mm:ss"));

        return CargarMasivaAsync("dbo.Stg_ComentariosSociales", tabla, cancellationToken);
    }

    public async Task RegistrarControlAsync(Guid idCarga, string fuente, int registros, long duracionMs,
        bool exitosa, string? mensaje, CancellationToken cancellationToken)
    {
        ValidarConexion();

        const string sql = @"INSERT INTO dbo.Extraccion_Control
            (IdCarga, Fuente, RegistrosExtraidos, DuracionMs, Estado, Mensaje)
            VALUES (@IdCarga, @Fuente, @Registros, @Duracion, @Estado, @Mensaje);";

        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync(cancellationToken);
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.AddWithValue("@IdCarga", idCarga);
        comando.Parameters.AddWithValue("@Fuente", fuente);
        comando.Parameters.AddWithValue("@Registros", registros);
        comando.Parameters.AddWithValue("@Duracion", duracionMs);
        comando.Parameters.AddWithValue("@Estado", exitosa ? "Exitosa" : "Fallida");
        comando.Parameters.AddWithValue("@Mensaje",
            string.IsNullOrEmpty(mensaje) ? DBNull.Value : (mensaje.Length > 1000 ? mensaje[..1000] : mensaje));
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<int> CargarMasivaAsync(string tablaDestino, DataTable tabla, CancellationToken cancellationToken)
    {
        if (tabla.Rows.Count == 0) return 0;
        ValidarConexion();

        await using var conexion = new SqlConnection(_connectionString);
        await conexion.OpenAsync(cancellationToken);

        using var bulk = new SqlBulkCopy(conexion)
        {
            DestinationTableName = tablaDestino,
            BatchSize = 1000,
            BulkCopyTimeout = 60
        };
        foreach (DataColumn columna in tabla.Columns)
            bulk.ColumnMappings.Add(columna.ColumnName, columna.ColumnName);

        await bulk.WriteToServerAsync(tabla, cancellationToken);
        _logger.LogInformation("[Staging] {Filas} filas cargadas en {Tabla}", tabla.Rows.Count, tablaDestino);
        return tabla.Rows.Count;
    }

    private void ValidarConexion()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
            throw new InvalidOperationException("Falta la cadena de conexión 'AnaliticaDb' (configúrala con User Secrets).");
    }

    private static object Nulo(string? valor) => valor is null ? DBNull.Value : valor;
}
