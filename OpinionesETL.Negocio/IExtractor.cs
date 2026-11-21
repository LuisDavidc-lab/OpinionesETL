using OpinionesETL.Entidades;

namespace OpinionesETL.Negocio;

/// <summary>
/// Contrato común para toda fuente de datos. Agregar una fuente nueva = crear una clase
/// que implemente esta interfaz y registrarla; el orquestador no cambia (escalabilidad).
/// </summary>
public interface IExtractor<T>
{
    string NombreFuente { get; }
    Task<ResultadoExtraccion<T>> ExtraerAsync(CancellationToken cancellationToken);
}
