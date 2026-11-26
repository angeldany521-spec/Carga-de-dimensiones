USE SistemaAnalisisOpinionesDW;
GO

INSERT INTO dbo.Dim_Tiempo (TiempoKey, Fecha, Anio, Trimestre, Mes, NombreMes, Dia, DiaSemana)
SELECT x.FechaKey, x.Fecha, x.Anio, x.Trimestre, x.Mes,
       CASE x.Mes
           WHEN 1 THEN 'Enero' WHEN 2 THEN 'Febrero' WHEN 3 THEN 'Marzo'
           WHEN 4 THEN 'Abril' WHEN 5 THEN 'Mayo' WHEN 6 THEN 'Junio'
           WHEN 7 THEN 'Julio' WHEN 8 THEN 'Agosto' WHEN 9 THEN 'Septiembre'
           WHEN 10 THEN 'Octubre' WHEN 11 THEN 'Noviembre' ELSE 'Diciembre'
       END,
       x.Dia,
       CASE x.DiaSemana
           WHEN 1 THEN 'Lunes' WHEN 2 THEN 'Martes' WHEN 3 THEN 'Miércoles'
           WHEN 4 THEN 'Jueves' WHEN 5 THEN 'Viernes' WHEN 6 THEN 'Sábado'
           ELSE 'Domingo'
       END
FROM (
    SELECT DISTINCT
        YEAR(f.Fecha) * 10000 + MONTH(f.Fecha) * 100 + DAY(f.Fecha) AS FechaKey,
        f.Fecha,
        YEAR(f.Fecha) AS Anio,
        DATEPART(QUARTER, f.Fecha) AS Trimestre,
        MONTH(f.Fecha) AS Mes,
        DAY(f.Fecha) AS Dia,
        ((DATEPART(WEEKDAY, f.Fecha) + @@DATEFIRST - 2) % 7) + 1 AS DiaSemana
    FROM (
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Comentarios_Sociales WHERE Fecha IS NOT NULL
        UNION
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Resenas_Web WHERE Fecha IS NOT NULL
        UNION
        SELECT Fecha FROM SistemaAnalisisOpiniones.dbo.Encuestas WHERE Fecha IS NOT NULL
    ) f
) x
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Tiempo t WHERE t.TiempoKey = x.FechaKey);
GO

INSERT INTO dbo.Dim_Cliente (IdClienteOrigen, Nombre, Email)
SELECT c.IdCliente, c.Nombre, c.Email
FROM SistemaAnalisisOpiniones.dbo.Clientes c
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Cliente d WHERE d.IdClienteOrigen = c.IdCliente);
GO

INSERT INTO dbo.Dim_Producto (IdProductoOrigen, Nombre, Categoria)
SELECT p.IdProducto, p.Nombre, c.NombreCategoria
FROM SistemaAnalisisOpiniones.dbo.Productos p
LEFT JOIN SistemaAnalisisOpiniones.dbo.Categorias c ON c.IdCategoria = p.IdCategoria
WHERE NOT EXISTS (SELECT 1 FROM dbo.Dim_Producto d WHERE d.IdProductoOrigen = p.IdProducto);
GO

INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Encuesta', 'Encuesta Interna', 'CSV'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'CSV'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Encuesta')
HAVING COUNT(*) > 0;
GO

INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Reseña Web', 'Reseña Web', 'Web'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'Web'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Reseña Web')
HAVING COUNT(*) > 0;
GO

INSERT INTO dbo.Dim_Fuente (IdFuenteOrigen, Canal, NombreFuente, TipoFuente)
SELECT MIN(f.IdFuente), 'Red Social', 'Red Social', 'Red Social'
FROM SistemaAnalisisOpiniones.dbo.Fuentes_Datos f
WHERE f.TipoFuente = 'Red Social'
  AND NOT EXISTS (SELECT 1 FROM dbo.Dim_Fuente d WHERE d.Canal = 'Red Social')
HAVING COUNT(*) > 0;
GO

INSERT INTO dbo.Fact_Opiniones (IdOpinionOrigen, TiempoKey, ClienteKey, ProductoKey, FuenteKey, SentimientoKey, Puntaje, LongitudComentario, Comentario)
SELECT e.IdOpinion, t.TiempoKey, dc.ClienteKey, dp.ProductoKey, df.FuenteKey, ds.SentimientoKey,
       CAST(e.PuntajeSatisfaccion AS DECIMAL(3,1)), LEN(e.Comentario), e.Comentario
FROM SistemaAnalisisOpiniones.dbo.Encuestas e
JOIN dbo.Dim_Tiempo t ON t.Fecha = e.Fecha
JOIN dbo.Dim_Cliente dc ON dc.IdClienteOrigen = e.IdCliente
JOIN dbo.Dim_Producto dp ON dp.IdProductoOrigen = e.IdProducto
JOIN dbo.Dim_Fuente df ON df.Canal = 'Encuesta'
JOIN dbo.Dim_Sentimiento ds ON ds.Clasificacion = e.Clasificacion
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Fact_Opiniones f
    WHERE f.FuenteKey = df.FuenteKey AND f.IdOpinionOrigen = e.IdOpinion
);
GO

INSERT INTO dbo.Fact_Opiniones (IdOpinionOrigen, TiempoKey, ClienteKey, ProductoKey, FuenteKey, SentimientoKey, Puntaje, LongitudComentario, Comentario)
SELECT r.IdReview, t.TiempoKey, dc.ClienteKey, dp.ProductoKey, df.FuenteKey, ds.SentimientoKey,
       r.Rating, LEN(r.Comentario), r.Comentario
FROM SistemaAnalisisOpiniones.dbo.Resenas_Web r
JOIN dbo.Dim_Tiempo t ON t.Fecha = r.Fecha
JOIN dbo.Dim_Cliente dc ON dc.IdClienteOrigen = r.IdCliente
JOIN dbo.Dim_Producto dp ON dp.IdProductoOrigen = r.IdProducto
JOIN dbo.Dim_Fuente df ON df.Canal = 'Reseña Web'
CROSS APPLY (
    SELECT CASE
        WHEN r.Rating < 3 THEN 'Negativa'
        WHEN r.Rating < 4 THEN 'Neutra'
        ELSE 'Positiva'
    END AS Clasificacion
) s
JOIN dbo.Dim_Sentimiento ds ON ds.Clasificacion = s.Clasificacion
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Fact_Opiniones f
    WHERE f.FuenteKey = df.FuenteKey AND f.IdOpinionOrigen = r.IdReview
);
GO

INSERT INTO dbo.Fact_Opiniones (IdOpinionOrigen, TiempoKey, ClienteKey, ProductoKey, FuenteKey, SentimientoKey, Puntaje, LongitudComentario, Comentario)
SELECT s.IdComment, t.TiempoKey, dc.ClienteKey, dp.ProductoKey, df.FuenteKey, ds.SentimientoKey,
       NULL, LEN(s.Comentario), s.Comentario
FROM SistemaAnalisisOpiniones.dbo.Comentarios_Sociales s
JOIN dbo.Dim_Tiempo t ON t.Fecha = s.Fecha
LEFT JOIN dbo.Dim_Cliente dc ON dc.IdClienteOrigen = s.IdCliente
JOIN dbo.Dim_Producto dp ON dp.IdProductoOrigen = s.IdProducto
JOIN dbo.Dim_Fuente df ON df.Canal = 'Red Social'
CROSS APPLY (
    SELECT CASE
        WHEN s.Comentario LIKE '%excelente%' OR s.Comentario LIKE '%recomiendo%'
          OR s.Comentario LIKE '%satisfecho%' OR s.Comentario LIKE '%me encanta%'
          OR s.Comentario LIKE '%calidad superior%' OR s.Comentario LIKE '%contento%'
            THEN 'Positiva'
        WHEN s.Comentario LIKE '%malo%' OR s.Comentario LIKE '%mala calidad%'
          OR s.Comentario LIKE '%pesimo%' OR s.Comentario LIKE '%pésimo%'
          OR s.Comentario LIKE '%decepcionado%' OR s.Comentario LIKE '%no funciona%'
          OR s.Comentario LIKE '%no cumple%' OR s.Comentario LIKE '%insatisfecho%'
          OR s.Comentario LIKE '%danado%' OR s.Comentario LIKE '%dañado%'
            THEN 'Negativa'
        WHEN s.Comentario LIKE '%regular%' OR s.Comentario LIKE '%basico%'
          OR s.Comentario LIKE '%básico%' OR s.Comentario LIKE '%esperaba%'
          OR s.Comentario LIKE '%sin mayor novedad%' OR s.Comentario LIKE '%cumple su funcion%'
          OR s.Comentario LIKE '%cumple su función%'
            THEN 'Neutra'
        ELSE 'No clasificado'
    END AS Clasificacion
) c
JOIN dbo.Dim_Sentimiento ds ON ds.Clasificacion = c.Clasificacion
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Fact_Opiniones f
    WHERE f.FuenteKey = df.FuenteKey AND f.IdOpinionOrigen = s.IdComment
);
GO

UPDATE dc
SET Segmento = CASE
    WHEN x.Total >= 5 THEN 'Frecuente'
    WHEN x.Total >= 2 THEN 'Ocasional'
    ELSE 'Esporádico'
END
FROM dbo.Dim_Cliente dc
JOIN (
    SELECT ClienteKey, COUNT(*) AS Total
    FROM dbo.Fact_Opiniones
    WHERE ClienteKey IS NOT NULL
    GROUP BY ClienteKey
) x ON x.ClienteKey = dc.ClienteKey;
GO

UPDATE dbo.Dim_Cliente
SET Segmento = 'Sin opiniones'
WHERE Segmento IS NULL;
GO
