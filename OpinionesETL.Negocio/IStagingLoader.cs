using OpinionesETL.Entidades;

namespace OpinionesETL.Negocio;

/// <summary>Contrato para guardar los datos extraídos en las tablas staging de la BD analítica.</summary>
public interface IStagingLoader
{
    Task<int> CargarEncuestasAsync(Guid idCarga, string archivoOrigen, IReadOnlyList<EncuestaDto> datos, CancellationToken cancellationToken);
    Task<int> CargarResenasAsync(Guid idCarga, IReadOnlyList<ResenaDto> datos, CancellationToken cancellationToken);
    Task<int> CargarComentariosAsync(Guid idCarga, IReadOnlyList<ComentarioDto> datos, CancellationToken cancellationToken);

    Task RegistrarControlAsync(Guid idCarga, string fuente, int registros, long duracionMs, bool exitosa, string? mensaje, CancellationToken cancellationToken);
}
