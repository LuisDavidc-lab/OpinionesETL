/* =====================================================================
   04_DataWarehouse_Dimensiones.sql
   Crea la BD DataWarehouse, sus 4 dimensiones y los procedimientos de carga.
   Las dimensiones se cargan desde las tablas Stg_* de OpinionesAnalitica
   (la misma instancia de SQL Server). El script es re-ejecutable:
   los registros ya cargados no se duplican.
   ===================================================================== */
IF DB_ID(N'DataWarehouse') IS NULL
    CREATE DATABASE DataWarehouse;
GO

USE DataWarehouse;
GO

/* ---------- Bitácora de cargas ---------- */
IF OBJECT_ID(N'dbo.DW_LogCarga', N'U') IS NULL
CREATE TABLE dbo.DW_LogCarga (
    IdLog       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DW_LogCarga PRIMARY KEY,
    Dimension   NVARCHAR(50)  NOT NULL,
    Insertados  INT           NOT NULL,
    FechaCarga  DATETIME2(0)  NOT NULL CONSTRAINT DF_DW_LogCarga_Fecha DEFAULT (SYSDATETIME())
);
GO

/* ---------- DimFuente ---------- */
IF OBJECT_ID(N'dbo.DimFuente', N'U') IS NULL
CREATE TABLE dbo.DimFuente (
    SK_Fuente   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimFuente PRIMARY KEY,
    TipoFuente  NVARCHAR(50)  NOT NULL CONSTRAINT UQ_DimFuente_Tipo UNIQUE,
    Origen      NVARCHAR(50)  NOT NULL,
    Descripcion NVARCHAR(200) NULL,
    FechaCarga  DATETIME2(0)  NOT NULL CONSTRAINT DF_DimFuente_Fecha DEFAULT (SYSDATETIME())
);
GO

/* ---------- DimProducto ---------- */
IF OBJECT_ID(N'dbo.DimProducto', N'U') IS NULL
CREATE TABLE dbo.DimProducto (
    SK_Producto INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimProducto PRIMARY KEY,
    Producto    NVARCHAR(200) NOT NULL CONSTRAINT UQ_DimProducto UNIQUE,
    Categoria   NVARCHAR(100) NOT NULL CONSTRAINT DF_DimProducto_Cat DEFAULT (N'Otros'),
    FechaCarga  DATETIME2(0)  NOT NULL CONSTRAINT DF_DimProducto_Fecha DEFAULT (SYSDATETIME())
);
GO

/* ---------- DimCliente ---------- */
IF OBJECT_ID(N'dbo.DimCliente', N'U') IS NULL
CREATE TABLE dbo.DimCliente (
    SK_Cliente        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DimCliente PRIMARY KEY,
    Identificador     NVARCHAR(200) NOT NULL,   -- email, nombre o usuario social, según la fuente
    TipoIdentificador NVARCHAR(30)  NOT NULL,   -- Email | Nombre | UsuarioSocial | N/A
    Nombre            NVARCHAR(200) NULL,
    Email             NVARCHAR(200) NULL,
    Dominio           NVARCHAR(100) NULL,
    FechaCarga        DATETIME2(0)  NOT NULL CONSTRAINT DF_DimCliente_Fecha DEFAULT (SYSDATETIME()),
    CONSTRAINT UQ_DimCliente UNIQUE (Identificador, TipoIdentificador)
);
GO

/* ---------- DimFecha ---------- */
IF OBJECT_ID(N'dbo.DimFecha', N'U') IS NULL
CREATE TABLE dbo.DimFecha (
    SK_Fecha      INT          NOT NULL CONSTRAINT PK_DimFecha PRIMARY KEY,   -- formato yyyymmdd
    Fecha         DATE         NOT NULL CONSTRAINT UQ_DimFecha_Fecha UNIQUE,
    Anio          SMALLINT     NOT NULL,
    Trimestre     TINYINT      NOT NULL,
    Mes           TINYINT      NOT NULL,
    NombreMes     NVARCHAR(20) NOT NULL,
    Dia           TINYINT      NOT NULL,
    NombreDia     NVARCHAR(20) NOT NULL,
    EsFinDeSemana BIT          NOT NULL
);
GO

/* ---------- Registros "Desconocido" (clave 0) para datos sin producto o cliente ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.DimProducto WHERE SK_Producto = 0)
BEGIN
    SET IDENTITY_INSERT dbo.DimProducto ON;
    INSERT INTO dbo.DimProducto (SK_Producto, Producto, Categoria) VALUES (0, N'Desconocido', N'Desconocido');
    SET IDENTITY_INSERT dbo.DimProducto OFF;
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DimCliente WHERE SK_Cliente = 0)
BEGIN
    SET IDENTITY_INSERT dbo.DimCliente ON;
    INSERT INTO dbo.DimCliente (SK_Cliente, Identificador, TipoIdentificador) VALUES (0, N'Desconocido', N'N/A');
    SET IDENTITY_INSERT dbo.DimCliente OFF;
END
GO

/* =====================================================================
   PROCEDIMIENTOS DE CARGA
   ===================================================================== */

CREATE OR ALTER PROCEDURE dbo.usp_CargarDimFuente
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DimFuente (TipoFuente, Origen, Descripcion)
    SELECT v.TipoFuente, v.Origen, v.Descripcion
    FROM (VALUES
        (N'Encuesta',         N'CSV',       N'Encuestas internas en archivo CSV'),
        (N'ResenaWeb',        N'BaseDatos', N'Reseñas web en base de datos relacional'),
        (N'ComentarioSocial', N'API REST',  N'Comentarios de redes sociales vía API')
    ) AS v (TipoFuente, Origen, Descripcion)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimFuente d WHERE d.TipoFuente = v.TipoFuente);

    DECLARE @n INT = @@ROWCOUNT;
    INSERT INTO dbo.DW_LogCarga (Dimension, Insertados) VALUES (N'DimFuente', @n);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CargarDimProducto
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DimProducto (Producto, Categoria)
    SELECT p.Producto,
           CASE
               WHEN n.N LIKE N'%teclado%' OR n.N LIKE N'%mouse%'                               THEN N'Periféricos'
               WHEN n.N LIKE N'%laptop%'                                                        THEN N'Computadoras'
               WHEN n.N LIKE N'%tablet%'                                                        THEN N'Tablets'
               WHEN n.N LIKE N'%iphone%' OR n.N LIKE N'%telefono%' OR n.N LIKE N'%nova%'        THEN N'Teléfonos'
               WHEN n.N LIKE N'%monitor%'                                                       THEN N'Monitores'
               WHEN n.N LIKE N'%audifonos%' OR n.N LIKE N'%bocina%'                             THEN N'Audio'
               WHEN n.N LIKE N'%impresora%'                                                     THEN N'Impresoras'
               WHEN n.N LIKE N'%cargador%'                                                      THEN N'Accesorios'
               WHEN n.N LIKE N'% tv %' OR n.N LIKE N'smart tv%'                                 THEN N'Televisores'
               ELSE N'Otros'
           END
    FROM (
        SELECT LTRIM(RTRIM(Producto)) AS Producto FROM OpinionesAnalitica.dbo.Stg_Encuestas
        UNION
        SELECT LTRIM(RTRIM(Producto)) FROM OpinionesAnalitica.dbo.Stg_ResenasWeb
        UNION
        SELECT LTRIM(RTRIM(Producto)) FROM OpinionesAnalitica.dbo.Stg_ComentariosSociales
    ) AS p
    CROSS APPLY (SELECT p.Producto COLLATE Latin1_General_CI_AI AS N) AS n
    WHERE p.Producto IS NOT NULL
      AND p.Producto <> N''
      AND NOT EXISTS (SELECT 1 FROM dbo.DimProducto d WHERE d.Producto = p.Producto);

    DECLARE @n INT = @@ROWCOUNT;
    INSERT INTO dbo.DW_LogCarga (Dimension, Insertados) VALUES (N'DimProducto', @n);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CargarDimCliente
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.DimCliente (Identificador, TipoIdentificador, Nombre, Email, Dominio)
    SELECT x.Identificador, x.Tipo, x.Nombre, x.Email, x.Dominio
    FROM (
        -- Encuestas (CSV): se identifican por email (solo emails con formato válido)
        SELECT LOWER(LTRIM(RTRIM(EmailCliente))) AS Identificador,
               N'Email' AS Tipo,
               CAST(NULL AS NVARCHAR(200)) AS Nombre,
               LOWER(LTRIM(RTRIM(EmailCliente))) AS Email,
               CAST(SUBSTRING(LOWER(LTRIM(RTRIM(EmailCliente))),
                    CHARINDEX(N'@', LTRIM(RTRIM(EmailCliente))) + 1, 100) AS NVARCHAR(100)) AS Dominio
        FROM OpinionesAnalitica.dbo.Stg_Encuestas
        WHERE LTRIM(RTRIM(EmailCliente)) LIKE N'%_@_%._%'
        UNION
        -- Reseñas web (BD): se identifican por nombre
        SELECT LTRIM(RTRIM(Cliente)), N'Nombre', LTRIM(RTRIM(Cliente)),
               CAST(NULL AS NVARCHAR(200)), CAST(NULL AS NVARCHAR(100))
        FROM OpinionesAnalitica.dbo.Stg_ResenasWeb
        WHERE NULLIF(LTRIM(RTRIM(Cliente)), N'') IS NOT NULL
        UNION
        -- Comentarios sociales (API): se identifican por usuario
        SELECT LTRIM(RTRIM(UsuarioSocial)), N'UsuarioSocial', CAST(NULL AS NVARCHAR(200)),
               CAST(NULL AS NVARCHAR(200)), CAST(NULL AS NVARCHAR(100))
        FROM OpinionesAnalitica.dbo.Stg_ComentariosSociales
        WHERE NULLIF(LTRIM(RTRIM(UsuarioSocial)), N'') IS NOT NULL
    ) AS x
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.DimCliente d
        WHERE d.Identificador = x.Identificador AND d.TipoIdentificador = x.Tipo);

    DECLARE @n INT = @@ROWCOUNT;
    INSERT INTO dbo.DW_LogCarga (Dimension, Insertados) VALUES (N'DimCliente', @n);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CargarDimFecha
    @Desde DATE = '2025-01-01',
    @Hasta DATE = '2026-12-31'
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Fechas AS (
        SELECT @Desde AS Fecha
        UNION ALL
        SELECT DATEADD(DAY, 1, Fecha) FROM Fechas WHERE Fecha < @Hasta
    )
    INSERT INTO dbo.DimFecha (SK_Fecha, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, NombreDia, EsFinDeSemana)
    SELECT CONVERT(INT, CONVERT(CHAR(8), f.Fecha, 112)),
           f.Fecha,
           YEAR(f.Fecha),
           DATEPART(QUARTER, f.Fecha),
           MONTH(f.Fecha),
           CHOOSE(MONTH(f.Fecha), N'Enero', N'Febrero', N'Marzo', N'Abril', N'Mayo', N'Junio',
                                  N'Julio', N'Agosto', N'Septiembre', N'Octubre', N'Noviembre', N'Diciembre'),
           DAY(f.Fecha),
           CHOOSE((DATEDIFF(DAY, '19000101', f.Fecha) % 7) + 1,
                  N'Lunes', N'Martes', N'Miércoles', N'Jueves', N'Viernes', N'Sábado', N'Domingo'),
           CASE WHEN (DATEDIFF(DAY, '19000101', f.Fecha) % 7) >= 5 THEN 1 ELSE 0 END
    FROM Fechas f
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DimFecha d WHERE d.Fecha = f.Fecha)
    OPTION (MAXRECURSION 0);

    DECLARE @n INT = @@ROWCOUNT;
    INSERT INTO dbo.DW_LogCarga (Dimension, Insertados) VALUES (N'DimFecha', @n);
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_CargarDimensiones
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM OpinionesAnalitica.dbo.Stg_Encuestas)
       AND NOT EXISTS (SELECT 1 FROM OpinionesAnalitica.dbo.Stg_ResenasWeb)
       AND NOT EXISTS (SELECT 1 FROM OpinionesAnalitica.dbo.Stg_ComentariosSociales)
        THROW 50001, N'No hay datos en las tablas staging. Ejecuta primero el Worker Service de extracción.', 1;

    EXEC dbo.usp_CargarDimFuente;
    EXEC dbo.usp_CargarDimProducto;
    EXEC dbo.usp_CargarDimCliente;
    EXEC dbo.usp_CargarDimFecha;
END
GO

/* =====================================================================
   EJECUCIÓN Y VERIFICACIÓN (capturas para las evidencias)
   ===================================================================== */
EXEC dbo.usp_CargarDimensiones;
GO

SELECT 'DimFuente'   AS Dimension, COUNT(*) AS Filas FROM dbo.DimFuente
UNION ALL SELECT 'DimProducto', COUNT(*) FROM dbo.DimProducto
UNION ALL SELECT 'DimCliente',  COUNT(*) FROM dbo.DimCliente
UNION ALL SELECT 'DimFecha',    COUNT(*) FROM dbo.DimFecha;

SELECT * FROM dbo.DW_LogCarga ORDER BY IdLog;
SELECT * FROM dbo.DimFuente ORDER BY SK_Fuente;
SELECT * FROM dbo.DimProducto ORDER BY SK_Producto;
SELECT TOP 20 * FROM dbo.DimCliente ORDER BY SK_Cliente;
SELECT TOP 20 * FROM dbo.DimFecha ORDER BY SK_Fecha;
GO
