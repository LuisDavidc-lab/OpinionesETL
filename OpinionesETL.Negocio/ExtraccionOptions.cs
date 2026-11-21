namespace OpinionesETL.Negocio.Configuracion;

/// <summary>Mapea la sección "Extraccion" de appsettings.json (patrón Options).</summary>
public class ExtraccionOptions
{
    public const string Seccion = "Extraccion";

    public bool EjecucionParalela { get; set; } = true;
    public FuentesOptions Fuentes { get; set; } = new();
}

public class FuentesOptions
{
    public CsvOptions Csv { get; set; } = new();
    public BaseDatosOptions BaseDatos { get; set; } = new();
    public ApiOptions Api { get; set; } = new();
}

public class CsvOptions
{
    public bool Habilitada { get; set; } = true;
    public string RutaArchivo { get; set; } = string.Empty;
    public string Delimitador { get; set; } = ",";
}

public class BaseDatosOptions
{
    public bool Habilitada { get; set; } = true;
    public string Consulta { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 60;
}

public class ApiOptions
{
    public bool Habilitada { get; set; } = true;
    public string UrlBase { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
    public int MaximoReintentos { get; set; } = 3;
    public string ApiKey { get; set; } = string.Empty;   // el valor real va en User Secrets
}
