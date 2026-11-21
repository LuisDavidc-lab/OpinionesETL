namespace OpinionesETL.Entidades;

/// <summary>Resultado de extraer una fuente: datos, tiempo y estado (para logs y la tabla Extraccion_Control).</summary>
public class ResultadoExtraccion<T>
{
    public string Fuente { get; set; } = string.Empty;
    public IReadOnlyList<T> Registros { get; set; } = Array.Empty<T>();
    public TimeSpan Duracion { get; set; }
    public bool Exitosa { get; set; }
    public string? Mensaje { get; set; }
}
