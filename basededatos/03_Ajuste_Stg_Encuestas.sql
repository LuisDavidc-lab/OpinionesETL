/* =====================================================================
   03_Ajuste_Stg_Encuestas.sql
   Recrea Stg_Encuestas para que coincida con las columnas reales del CSV:
   fecha, email_cliente, producto, canal_encuesta, nota_general, comentario
   (la tabla aún está vacía, por eso se puede recrear sin perder datos).
   ===================================================================== */
USE OpinionesAnalitica;
GO

DROP TABLE IF EXISTS dbo.Stg_Encuestas;
GO

CREATE TABLE dbo.Stg_Encuestas (
    IdStg           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Stg_Encuestas PRIMARY KEY,
    IdCarga         UNIQUEIDENTIFIER  NOT NULL,
    FechaExtraccion DATETIME2(0)      NOT NULL CONSTRAINT DF_StgEnc_Fecha DEFAULT (SYSDATETIME()),
    ArchivoOrigen   NVARCHAR(260)     NULL,
    NumeroFila      INT               NULL,
    Fecha           NVARCHAR(50)      NULL,
    EmailCliente    NVARCHAR(200)     NULL,
    Producto        NVARCHAR(200)     NULL,
    CanalEncuesta   NVARCHAR(100)     NULL,
    NotaGeneral     NVARCHAR(50)      NULL,
    Comentario      NVARCHAR(2000)    NULL
);
GO

SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'Stg_Encuestas'
ORDER BY ORDINAL_POSITION;
GO
