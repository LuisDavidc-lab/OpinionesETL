// OpinionesETL.ApiMock - API REST simulada de comentarios en redes sociales.
// Requiere el encabezado X-Api-Key para responder (simula autenticación de una API real).

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string ApiKeyHeader = "X-Api-Key";
var apiKeyEsperada = app.Configuration["ApiKey"] ?? "clave-de-prueba-123";

var comentarios = new List<ComentarioSocial>
{
    new("tw-1001", "X",         "@maria_tech",   "Laptop Pro 14",       "La Laptop Pro 14 vuela, la mejor compra del año.",                  124, "https://social.example/x/1001",  new DateTime(2026, 10, 21, 9, 12, 0)),
    new("ig-2001", "Instagram", "@luis.gadgets", "Audífonos Sonic X",   "Los Sonic X suenan brutal, pero el estuche es frágil.",              87,  "https://social.example/ig/2001", new DateTime(2026, 10, 21, 18, 40, 0)),
    new("fb-3001", "Facebook",  "Ana Martínez",  "Teléfono Nova 5G",    "El Nova 5G me llegó con la pantalla rayada. Pésimo servicio.",       15,  "https://social.example/fb/3001", new DateTime(2026, 10, 22, 11, 5, 0)),
    new("tt-4001", "TikTok",    "@dev_carlos",   "Teclado Mecánico K2", "Probé el K2 y el sonido es adictivo, 10/10.",                        560, "https://social.example/tt/4001", new DateTime(2026, 10, 22, 20, 30, 0)),
    new("tw-1002", "X",         "@sofi_designs", "Monitor Vista 27",    "El Vista 27 tiene colores increíbles para diseño.",                  64,  "https://social.example/x/1002",  new DateTime(2026, 10, 23, 8, 55, 0)),
    new("ig-2002", "Instagram", "@pedro.review", "Mouse Ergo Plus",     "El Ergo Plus me salvó la muñeca, lo recomiendo.",                    210, "https://social.example/ig/2002", new DateTime(2026, 10, 23, 15, 20, 0)),
    new("fb-3002", "Facebook",  "Laura Santos",  "Tablet Slate 10",     "La Slate 10 se pone lenta con varias apps abiertas.",                9,   "https://social.example/fb/3002", new DateTime(2026, 10, 24, 10, 10, 0)),
    new("tt-4002", "TikTok",    "@tecno_rosa",   "Bocina Boom Mini",    "La Boom Mini es chiquita pero suena durísimo.",                      342, "https://social.example/tt/4002", new DateTime(2026, 10, 24, 19, 45, 0)),
    new("tw-1003", "X",         "@jorge_p",      "Laptop Pro 14",       "Ya van dos semanas con la Pro 14 y se calienta bastante.",           31,  "https://social.example/x/1003",  new DateTime(2026, 10, 25, 7, 30, 0)),
    new("ig-2003", "Instagram", "@elena.studies","Tablet Slate 10",     "Perfecta para tomar apuntes, la batería rinde todo el día.",         76,  "https://social.example/ig/2003", new DateTime(2026, 10, 25, 14, 0, 0)),
    new("fb-3003", "Facebook",  "Ricardo Torres","Audífonos Sonic X",   "Se me dañó un lado a las dos semanas. No los compren.",              22,  "https://social.example/fb/3003", new DateTime(2026, 10, 26, 12, 25, 0)),
    new("tt-4003", "TikTok",    "@hector_m",     "Monitor Vista 27",    "Relación calidad precio excelente, mi setup quedó brutal.",          418, "https://social.example/tt/4003", new DateTime(2026, 10, 26, 21, 10, 0)),
    new("tw-1004", "X",         "@patri_luna",   "Bocina Boom Mini",    "Se desconecta cada rato por Bluetooth, qué decepción.",              18,  "https://social.example/x/1004",  new DateTime(2026, 10, 27, 9, 50, 0)),
    new("tw-1004", "X",         "@patri_luna",   "Bocina Boom Mini",    "Se desconecta cada rato por Bluetooth, qué decepción.",              18,  "https://social.example/x/1004",  new DateTime(2026, 10, 27, 9, 50, 0)), // duplicado intencional
    new("ig-2004", "Instagram", "@anonimo_99",   "Teléfono Nova 5G",    null,                                                                  0,   "https://social.example/ig/2004", new DateTime(2026, 10, 28, 16, 15, 0)) // sin texto intencional
};

app.MapGet("/api/comentarios", (HttpContext ctx) =>
{
    if (!ctx.Request.Headers.TryGetValue(ApiKeyHeader, out var key) || key != apiKeyEsperada)
        return Results.Unauthorized();

    return Results.Ok(comentarios);
});

app.Run();

record ComentarioSocial(
    string Id,
    string Plataforma,
    string Usuario,
    string Producto,
    string? Texto,
    int Likes,
    string Url,
    DateTime Fecha);