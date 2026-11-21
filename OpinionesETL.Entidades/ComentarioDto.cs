namespace OpinionesETL.Entidades;

/// <summary>Comentario de red social extraído de la API REST.
/// Los nombres coinciden con el JSON de la API (se deserializa sin distinguir mayúsculas).</summary>
public class ComentarioDto
{
    public string Id { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string? Texto { get; set; }
    public int Likes { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}
