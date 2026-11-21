/* =====================================================================
   01_OpinionesOrigen.sql
   BD relacional de ORIGEN (fuente de reseñas web) para el proceso de extracción.
   ===================================================================== */
IF DB_ID(N'OpinionesOrigen') IS NULL
    CREATE DATABASE OpinionesOrigen;
GO

USE OpinionesOrigen;
GO

IF OBJECT_ID(N'dbo.ResenasOrigen', N'U') IS NOT NULL
    DROP TABLE dbo.ResenasOrigen;
GO

CREATE TABLE dbo.ResenasOrigen (
    IdResena         INT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_ResenasOrigen PRIMARY KEY,
    Cliente          NVARCHAR(100)  NULL,
    Producto         NVARCHAR(100)  NOT NULL,
    Titulo           NVARCHAR(150)  NULL,
    Comentario       NVARCHAR(1000) NULL,
    Estrellas        TINYINT        NOT NULL
        CONSTRAINT CK_ResenasOrigen_Estrellas CHECK (Estrellas BETWEEN 1 AND 5),
    CompraVerificada BIT            NOT NULL
        CONSTRAINT DF_ResenasOrigen_CompraVerificada DEFAULT (0),
    Fecha            DATETIME2(0)   NOT NULL
);
GO

INSERT INTO dbo.ResenasOrigen (Cliente, Producto, Titulo, Comentario, Estrellas, CompraVerificada, Fecha) VALUES
(N'María Pérez',      N'Laptop Pro 14',       N'Excelente rendimiento',   N'Muy rápida y la batería dura todo el día. Totalmente recomendada.', 5, 1, '2026-10-02 09:15:00'),
(N'Juan Rodríguez',   N'Laptop Pro 14',       N'Buena pero cara',         N'Funciona bien, aunque el precio me parece alto para lo que ofrece.', 3, 1, '2026-10-03 14:40:00'),
(N'Ana Martínez',     N'Audífonos Sonic X',   N'Sonido increíble',        N'La cancelación de ruido es muy buena y son cómodos.', 5, 1, '2026-10-04 18:05:00'),
(N'Carlos Gómez',     N'Audífonos Sonic X',   N'Se dañaron rápido',       N'A las dos semanas dejó de funcionar el lado izquierdo.', 1, 1, '2026-10-05 11:20:00'),
(N'Laura Santos',     N'Teléfono Nova 5G',    N'Cámara espectacular',     N'Las fotos de noche salen muy bien. La batería podría ser mejor.', 4, 1, '2026-10-06 16:30:00'),
(N'Pedro Díaz',       N'Teléfono Nova 5G',    N'Se calienta mucho',       N'Al jugar se calienta demasiado y el rendimiento baja.', 2, 0, '2026-10-07 20:10:00'),
(N'Sofía Ramírez',    N'Teclado Mecánico K2', N'Muy cómodo',              N'Buen tacto y construcción sólida. Perfecto para programar.', 5, 1, '2026-10-08 08:45:00'),
(N'Luis Fernández',   N'Teclado Mecánico K2', N'Ruidoso',                 N'Es muy ruidoso para la oficina, pero escribir se siente genial.', 3, 1, '2026-10-09 10:00:00'),
(N'Carmen Núñez',     N'Monitor Vista 27',    N'Colores muy fieles',      N'Ideal para diseño. La calibración de fábrica es muy buena.', 5, 1, '2026-10-10 13:25:00'),
(N'Miguel Castillo',  N'Monitor Vista 27',    N'Pixel muerto',            N'Llegó con un pixel muerto y el soporte tardó en responder.', 2, 1, '2026-10-11 15:50:00'),
(N'Rosa Medina',      N'Mouse Ergo Plus',     N'Alivió mi muñeca',        N'Después de usarlo una semana noté la diferencia. Muy bueno.', 5, 1, '2026-10-12 09:30:00'),
(N'Jorge Peña',       N'Mouse Ergo Plus',     N'Regular',                 N'Cumple, nada especial.', 3, 0, '2026-10-13 17:15:00'),
(N'Elena Vargas',     N'Tablet Slate 10',     N'Perfecta para estudiar',  N'Ligera, buena pantalla y suficiente batería para las clases.', 4, 1, '2026-10-14 12:00:00'),
(N'Ricardo Torres',   N'Tablet Slate 10',     N'Lenta',                   N'Se traba al abrir varias aplicaciones. Esperaba más.', 2, 1, '2026-10-15 19:40:00'),
(N'Patricia Luna',    N'Bocina Boom Mini',    N'Pequeña pero potente',    N'Suena mucho más fuerte de lo que parece por su tamaño.', 4, 1, '2026-10-16 21:05:00'),
(N'Andrés Flores',    N'Bocina Boom Mini',    N'No conecta bien',         N'El Bluetooth se desconecta cada pocos minutos.', 1, 1, '2026-10-17 10:35:00'),
(NULL,                N'Laptop Pro 14',       N'Sin nombre',              N'Reseña anónima: llegó rápido y bien embalada.', 4, 0, '2026-10-18 11:10:00'),
(N'Daniela Rojas',    N'Audífonos Sonic X',   NULL,                       NULL, 3, 1, '2026-10-19 14:20:00'),
(N'Héctor Mejía',     N'Monitor Vista 27',    N'Muy recomendado',         N'Excelente relación calidad precio.', 5, 1, '2026-10-20 08:55:00'),
(N'María Pérez',      N'Laptop Pro 14',       N'Excelente rendimiento',   N'Muy rápida y la batería dura todo el día. Totalmente recomendada.', 5, 1, '2026-10-02 09:15:00');
GO

SELECT COUNT(*) AS TotalResenas FROM dbo.ResenasOrigen;
GO