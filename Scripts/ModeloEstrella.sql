IF DB_ID('SistemaAnalisisOpinionesDW') IS NULL
    CREATE DATABASE SistemaAnalisisOpinionesDW;
GO

USE SistemaAnalisisOpinionesDW;
GO

IF OBJECT_ID('dbo.Dim_Tiempo', 'U') IS NULL
CREATE TABLE dbo.Dim_Tiempo (
    TiempoKey  INT NOT NULL PRIMARY KEY,
    Fecha      DATE NOT NULL,
    Anio       INT NOT NULL,
    Trimestre  INT NOT NULL,
    Mes        INT NOT NULL,
    NombreMes  VARCHAR(20) NOT NULL,
    Dia        INT NOT NULL,
    DiaSemana  VARCHAR(20) NOT NULL
);
GO

IF OBJECT_ID('dbo.Dim_Cliente', 'U') IS NULL
CREATE TABLE dbo.Dim_Cliente (
    ClienteKey      INT IDENTITY(1,1) PRIMARY KEY,
    IdClienteOrigen VARCHAR(50) NOT NULL,
    Nombre          VARCHAR(150),
    Email           VARCHAR(150),
    Pais            VARCHAR(50) NULL,
    Edad            INT NULL,
    Segmento        VARCHAR(30) NULL,
    CONSTRAINT UQ_Cliente_Origen UNIQUE (IdClienteOrigen),
    CONSTRAINT CK_Cliente_Edad CHECK (Edad IS NULL OR Edad BETWEEN 0 AND 120)
);
GO

IF OBJECT_ID('dbo.Dim_Producto', 'U') IS NULL
CREATE TABLE dbo.Dim_Producto (
    ProductoKey      INT IDENTITY(1,1) PRIMARY KEY,
    IdProductoOrigen VARCHAR(50) NOT NULL,
    Nombre           VARCHAR(255),
    Categoria        VARCHAR(100),
    CONSTRAINT UQ_Producto_Origen UNIQUE (IdProductoOrigen)
);
GO

IF OBJECT_ID('dbo.Dim_Fuente', 'U') IS NULL
CREATE TABLE dbo.Dim_Fuente (
    FuenteKey      INT IDENTITY(1,1) PRIMARY KEY,
    IdFuenteOrigen VARCHAR(50) NULL,
    Canal          VARCHAR(50) NOT NULL,
    NombreFuente   VARCHAR(50) NULL,
    TipoFuente     VARCHAR(50) NULL,
    CONSTRAINT UQ_Fuente_Canal UNIQUE (Canal)
);
GO

IF OBJECT_ID('dbo.Dim_Sentimiento', 'U') IS NULL
CREATE TABLE dbo.Dim_Sentimiento (
    SentimientoKey INT NOT NULL PRIMARY KEY,
    Clasificacion  VARCHAR(20) NOT NULL,
    CONSTRAINT UQ_Sentimiento_Clasificacion UNIQUE (Clasificacion)
);
GO

IF OBJECT_ID('dbo.Fact_Opiniones', 'U') IS NULL
CREATE TABLE dbo.Fact_Opiniones (
    OpinionKey         BIGINT IDENTITY(1,1) PRIMARY KEY,
    IdOpinionOrigen    VARCHAR(50) NOT NULL,
    TiempoKey          INT NOT NULL,
    ClienteKey         INT NULL,
    ProductoKey        INT NOT NULL,
    FuenteKey          INT NOT NULL,
    SentimientoKey     INT NOT NULL,
    Puntaje            DECIMAL(3,1) NULL,
    Cantidad           INT NOT NULL CONSTRAINT DF_Fact_Cantidad DEFAULT (1),
    LongitudComentario INT NULL,
    Comentario         NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_Fact_Origen UNIQUE (FuenteKey, IdOpinionOrigen),
    CONSTRAINT CK_Fact_Puntaje CHECK (Puntaje IS NULL OR Puntaje BETWEEN 1 AND 5),
    CONSTRAINT FK_Fact_Tiempo      FOREIGN KEY (TiempoKey)      REFERENCES dbo.Dim_Tiempo(TiempoKey),
    CONSTRAINT FK_Fact_Cliente     FOREIGN KEY (ClienteKey)     REFERENCES dbo.Dim_Cliente(ClienteKey),
    CONSTRAINT FK_Fact_Producto    FOREIGN KEY (ProductoKey)    REFERENCES dbo.Dim_Producto(ProductoKey),
    CONSTRAINT FK_Fact_Fuente      FOREIGN KEY (FuenteKey)      REFERENCES dbo.Dim_Fuente(FuenteKey),
    CONSTRAINT FK_Fact_Sentimiento FOREIGN KEY (SentimientoKey) REFERENCES dbo.Dim_Sentimiento(SentimientoKey)
);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Dim_Sentimiento)
INSERT INTO dbo.Dim_Sentimiento (SentimientoKey, Clasificacion) VALUES
    (1, 'Positiva'),
    (2, 'Neutra'),
    (3, 'Negativa'),
    (4, 'No clasificado');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Tiempo' AND object_id = OBJECT_ID('dbo.Fact_Opiniones'))
    CREATE INDEX IX_Fact_Tiempo ON dbo.Fact_Opiniones (TiempoKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Cliente' AND object_id = OBJECT_ID('dbo.Fact_Opiniones'))
    CREATE INDEX IX_Fact_Cliente ON dbo.Fact_Opiniones (ClienteKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Producto' AND object_id = OBJECT_ID('dbo.Fact_Opiniones'))
    CREATE INDEX IX_Fact_Producto ON dbo.Fact_Opiniones (ProductoKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Fuente' AND object_id = OBJECT_ID('dbo.Fact_Opiniones'))
    CREATE INDEX IX_Fact_Fuente ON dbo.Fact_Opiniones (FuenteKey);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Fact_Sentimiento' AND object_id = OBJECT_ID('dbo.Fact_Opiniones'))
    CREATE INDEX IX_Fact_Sentimiento ON dbo.Fact_Opiniones (SentimientoKey);
GO
