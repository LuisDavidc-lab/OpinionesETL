namespace OpinionesETL.Entidades;

/// <summary>Reseña web extraída de la BD relacional de origen (tabla ResenasOrigen).</summary>
public class ResenaDto
{
    public int IdResenaOrigen { get; set; }
    public string? Cliente { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string? Titulo { get; set; }
    public string? Comentario { get; set; }
    public int Estrellas { get; set; }
    public bool CompraVerificada { get; set; }
    public DateTime Fecha { get; set; }
}
