namespace OpinionesETL.Entidades;

/// <summary>
/// Fila cruda del CSV de encuestas. Todo se guarda como texto a propósito:
/// la validación y conversión de tipos corresponde a la fase de Transformación.
/// </summary>
public class EncuestaDto
{
    public int NumeroFila { get; set; }
    public string? Fecha { get; set; }
    public string? EmailCliente { get; set; }
    public string? Producto { get; set; }
    public string? CanalEncuesta { get; set; }
    public string? NotaGeneral { get; set; }
    public string? Comentario { get; set; }
}
