/* =====================================================================
   02_OpinionesAnalitica_Staging.sql
   Tablas STAGING y de control en la BD analítica.
   El staging guarda los datos CRUDOS tal como se extraen (todo en texto),
   sin validar ni transformar: eso se hace en la fase T del ETL.
   ===================================================================== */
IF DB_ID(N'OpinionesAnalitica') IS NULL
    CREATE DATABASE OpinionesAnalitica;
GO

USE OpinionesAnalitica;
GO

/* ---------- Control de extracciones (trazabilidad y métricas) ---------- */
IF OBJECT_ID(N'dbo.Extraccion_Control', N'U') IS NULL
CREATE TABLE dbo.Extraccion_Control (
    IdControl          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Extraccion_Control PRIMARY KEY,
    IdCarga            UNIQUEIDENTIFIER  NOT NULL,
    Fuente             NVARCHAR(30)      NOT NULL,   -- CSV | BaseDatos | Api
    RegistrosExtraidos INT               NOT NULL CONSTRAINT DF_EC_Registros DEFAULT (0),
    DuracionMs         BIGINT            NOT NULL CONSTRAINT DF_EC_Duracion DEFAULT (0),
    Estado             NVARCHAR(20)      NOT NULL,   -- Exitosa | Fallida
    Mensaje            NVARCHAR(1000)    NULL,
    FechaInicio        DATETIME2(0)      NOT NULL CONSTRAINT DF_EC_Fecha DEFAULT (SYSDATETIME())
);
GO

/* ---------- Staging: encuestas (CSV) ---------- */
IF OBJECT_ID(N'dbo.Stg_Encuestas', N'U') IS NULL
CREATE TABLE dbo.Stg_Encuestas (
    IdStg           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stg_Encuestas PRIMARY KEY,
    IdCarga         UNIQUEIDENTIFIER  NOT NULL,
    FechaExtraccion DATETIME2(0)      NOT NULL CONSTRAINT DF_StgEnc_Fecha DEFAULT (SYSDATETIME()),
    ArchivoOrigen   NVARCHAR(260)     NULL,
    NumeroFila      INT               NULL,
    Cliente         NVARCHAR(200)     NULL,
    Email           NVARCHAR(200)     NULL,
    Producto        NVARCHAR(200)     NULL,
    Fecha           NVARCHAR(50)      NULL,
    Comentario      NVARCHAR(2000)    NULL,
    CanalEncuesta   NVARCHAR(100)     NULL,
    NotaGeneral     NVARCHAR(50)      NULL
);
GO

/* ---------- Staging: reseñas web (BD relacional) ---------- */
IF OBJECT_ID(N'dbo.Stg_ResenasWeb', N'U') IS NULL
CREATE TABLE dbo.Stg_ResenasWeb (
    IdStg            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stg_ResenasWeb PRIMARY KEY,
    IdCarga          UNIQUEIDENTIFIER  NOT NULL,
    FechaExtraccion  DATETIME2(0)      NOT NULL CONSTRAINT DF_StgRes_Fecha DEFAULT (SYSDATETIME()),
    IdResenaOrigen   INT               NULL,
    Cliente          NVARCHAR(200)     NULL,
    Producto         NVARCHAR(200)     NULL,
    Titulo           NVARCHAR(300)     NULL,
    Comentario       NVARCHAR(2000)    NULL,
    Estrellas        NVARCHAR(20)      NULL,
    CompraVerificada NVARCHAR(20)      NULL,
    Fecha            NVARCHAR(50)      NULL
);
GO

/* ---------- Staging: comentarios sociales (API REST) ---------- */
IF OBJECT_ID(N'dbo.Stg_ComentariosSociales', N'U') IS NULL
CREATE TABLE dbo.Stg_ComentariosSociales (
    IdStg            INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stg_ComentariosSociales PRIMARY KEY,
    IdCarga          UNIQUEIDENTIFIER  NOT NULL,
    FechaExtraccion  DATETIME2(0)      NOT NULL CONSTRAINT DF_StgCom_Fecha DEFAULT (SYSDATETIME()),
    IdExterno        NVARCHAR(100)     NULL,
    Plataforma       NVARCHAR(100)     NULL,
    UsuarioSocial    NVARCHAR(200)     NULL,
    Producto         NVARCHAR(200)     NULL,
    Comentario       NVARCHAR(2000)    NULL,
    Likes            NVARCHAR(20)      NULL,
    UrlPublicacion   NVARCHAR(500)     NULL,
    Fecha            NVARCHAR(50)      NULL
);
GO

SELECT name AS TablaCreada FROM sys.tables
WHERE name IN (N'Extraccion_Control', N'Stg_Encuestas', N'Stg_ResenasWeb', N'Stg_ComentariosSociales');
GO
