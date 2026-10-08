namespace Contpaq.Bridge.Infrastructure.Persistence
{
    /// <summary>
    /// El SQL de solo lectura de las lecturas del contrato (§6, CT-30). Está aparte del repositorio
    /// para que una prueba revise cada tabla y columna contra docs/contpaq/Referencia_BD_CONTPAQi.md
    /// (Principio VII). CONTPAQi guarda códigos y nombres como texto de ancho fijo: se les quita el
    /// espacio del final (RTRIM) para que PolyConecta compare códigos exactos.
    /// </summary>
    public static class SqlLecturas
    {
        /// <summary>Productos por página; el cursor es CIDPRODUCTO. <paramref name="clasificacion"/> es el número de CIDVALORCLASIFICACION{n} (1 a 6) de "TIPO DE PRODUCTOS", o null.</summary>
        public static string Productos(int? clasificacion) => $@"
            SELECT CAST(p.CIDPRODUCTO AS bigint) AS IdErp,
                   RTRIM(p.CCODIGOPRODUCTO) AS Codigo,
                   RTRIM(p.CNOMBREPRODUCTO) AS Nombre,
                   RTRIM(ISNULL(u.CABREVIATURA, '')) AS UnidadBase,
                   CAST(CASE WHEN (p.CCONTROLEXISTENCIA & 16) <> 0 THEN 1 ELSE 0 END AS bit) AS LlevaLote,
                   CAST(CASE WHEN p.CSTATUSPRODUCTO = 1 THEN 1 ELSE 0 END AS bit) AS Activo,
                   {(clasificacion is null ? "CAST(NULL AS varchar(3))" : "NULLIF(RTRIM(cv.CCODIGOVALORCLASIFICACION), '')")} AS ClasificacionCodigo,
                   {(clasificacion is null ? "CAST(NULL AS varchar(60))" : "RTRIM(cv.CVALORCLASIFICACION)")} AS ClasificacionNombre
            FROM admProductos p WITH (NOLOCK)
            LEFT JOIN admUnidadesMedidaPeso u WITH (NOLOCK) ON u.CIDUNIDAD = p.CIDUNIDADBASE
            {(clasificacion is null ? "" : $"LEFT JOIN admClasificacionesValores cv WITH (NOLOCK) ON cv.CIDVALORCLASIFICACION = p.CIDVALORCLASIFICACION{clasificacion}")}";

        /// <summary>Clientes (CTIPOCLIENTE 1 y 2: los proveedores puros no son clientes); el cursor es CIDCLIENTEPROVEEDOR.</summary>
        public const string Clientes = @"
            SELECT CAST(CIDCLIENTEPROVEEDOR AS bigint) AS IdErp,
                   RTRIM(CCODIGOCLIENTE) AS Codigo,
                   RTRIM(CRAZONSOCIAL) AS RazonSocial,
                   RTRIM(CRFC) AS Rfc,
                   CAST(CASE WHEN CESTATUS = 1 THEN 1 ELSE 0 END AS bit) AS Activo,
                   CAST(CIDMONEDA AS int) AS MonedaId
            FROM admClientes WITH (NOLOCK)
            WHERE CTIPOCLIENTE IN (1, 2)";

        /// <summary>Domicilios de una página de clientes, en una sola consulta (CTIPOCATALOGO 1 = clientes; CTIPODIRECCION 0 fiscal, 1 envío).</summary>
        public const string Domicilios = @"
            SELECT CAST(CIDCATALOGO AS bigint) AS ClienteId,
                   CAST(CIDDIRECCION AS bigint) AS IdErp,
                   CAST(CTIPODIRECCION AS int) AS TipoDireccion,
                   ISNULL(RTRIM(CNOMBRECALLE), '') AS Calle,
                   ISNULL(RTRIM(CNUMEROEXTERIOR), '') AS NumeroExterior,
                   ISNULL(RTRIM(CNUMEROINTERIOR), '') AS NumeroInterior,
                   ISNULL(RTRIM(CCOLONIA), '') AS Colonia,
                   ISNULL(RTRIM(CCODIGOPOSTAL), '') AS CodigoPostal,
                   ISNULL(RTRIM(CCIUDAD), '') AS Ciudad,
                   ISNULL(RTRIM(CMUNICIPIO), '') AS Municipio,
                   ISNULL(RTRIM(CESTADO), '') AS Estado,
                   ISNULL(RTRIM(CPAIS), '') AS Pais,
                   ISNULL(RTRIM(CSUCURSAL), '') AS Sucursal
            FROM admDomicilios WITH (NOLOCK)
            WHERE CTIPOCATALOGO = 1 AND CIDCATALOGO IN @Ids
            ORDER BY CIDCATALOGO, CTIPODIRECCION, CIDDIRECCION;";

        /// <summary>Agentes; CTIPOAGENTE 1 venta, 2 venta_cobro, 3 cobro. El cursor es CIDAGENTE.</summary>
        public const string Agentes = @"
            SELECT CAST(CIDAGENTE AS bigint) AS IdErp,
                   RTRIM(CCODIGOAGENTE) AS Codigo,
                   RTRIM(CNOMBREAGENTE) AS Nombre,
                   CAST(CTIPOAGENTE AS int) AS TipoAgente
            FROM admAgentes WITH (NOLOCK)";

        public const string Almacenes = @"
            SELECT RTRIM(CCODIGOALMACEN) AS Codigo, RTRIM(CNOMBREALMACEN) AS Nombre, CAST(CIDALMACEN AS bigint) AS IdErp
            FROM admAlmacenes WITH (NOLOCK)";

        /// <summary>
        /// Existencia por lote (F-02, D-83, D-87) de los productos que llevan lote: suma de
        /// admCapasProducto.CEXISTENCIA por número de lote y almacén.
        /// </summary>
        public const string ExistenciasPorLote = @"
            SELECT RTRIM(p.CCODIGOPRODUCTO) AS Producto, RTRIM(a.CCODIGOALMACEN) AS Almacen, RTRIM(ISNULL(u.CABREVIATURA, '')) AS Unidad,
                   CAST(SUM(cp.CEXISTENCIA) AS decimal(18,4)) AS Cantidad, RTRIM(cp.CNUMEROLOTE) AS Lote
            FROM admCapasProducto cp WITH (NOLOCK)
            JOIN admProductos p WITH (NOLOCK) ON cp.CIDPRODUCTO = p.CIDPRODUCTO
            JOIN admAlmacenes a WITH (NOLOCK) ON cp.CIDALMACEN = a.CIDALMACEN
            LEFT JOIN admUnidadesMedidaPeso u WITH (NOLOCK) ON u.CIDUNIDAD = p.CIDUNIDADBASE
            WHERE p.CCODIGOPRODUCTO IN @Productos
              AND (p.CCONTROLEXISTENCIA & 16) <> 0
              AND RTRIM(ISNULL(cp.CNUMEROLOTE, '')) <> ''
              AND (@Almacen IS NULL OR a.CCODIGOALMACEN = @Almacen)
            GROUP BY p.CCODIGOPRODUCTO, a.CCODIGOALMACEN, u.CABREVIATURA, RTRIM(cp.CNUMEROLOTE)
            HAVING SUM(cp.CEXISTENCIA) <> 0;";

        /// <summary>Ejercicio vigente: el que contiene la fecha de hoy (D-156). Sin respaldo; si no hay, <see cref="EjercicioVigenteException"/>.</summary>
        public const string EjercicioVigente = @"SELECT TOP 1 x.CIDEJERCICIO FROM admEjercicios x WITH (NOLOCK)
                     WHERE CAST(GETDATE() AS date) BETWEEN x.CFECINIPERIODO1 AND x.CFECHAFINAL
                     ORDER BY x.CNUMEROEJERCICIO DESC";

        /// <summary>
        /// Existencia por producto y almacén (F-01, D-87) de los productos sin lote:
        /// CENTRADASPERIODO12 − CSALIDASPERIODO12 de admExistenciaCosto del ejercicio vigente (solo el que
        /// contiene hoy, D-156; si ninguno, la lectura falla). CTIPOEXISTENCIA 1 es la existencia en la unidad base.
        /// </summary>
        public const string ExistenciasPorProducto = @"
            SELECT RTRIM(p.CCODIGOPRODUCTO) AS Producto, RTRIM(a.CCODIGOALMACEN) AS Almacen, RTRIM(ISNULL(u.CABREVIATURA, '')) AS Unidad,
                   CAST(SUM(e.CENTRADASPERIODO12 - e.CSALIDASPERIODO12) AS decimal(18,4)) AS Cantidad, CAST(NULL AS varchar(30)) AS Lote
            FROM admExistenciaCosto e WITH (NOLOCK)
            JOIN admProductos p WITH (NOLOCK) ON e.CIDPRODUCTO = p.CIDPRODUCTO
            JOIN admAlmacenes a WITH (NOLOCK) ON e.CIDALMACEN = a.CIDALMACEN
            LEFT JOIN admUnidadesMedidaPeso u WITH (NOLOCK) ON u.CIDUNIDAD = p.CIDUNIDADBASE
            WHERE p.CCODIGOPRODUCTO IN @Productos
              AND (p.CCONTROLEXISTENCIA & 16) = 0
              AND e.CTIPOEXISTENCIA = 1
              AND e.CIDEJERCICIO = (" + EjercicioVigente + @")
              AND (@Almacen IS NULL OR a.CCODIGOALMACEN = @Almacen)
            GROUP BY p.CCODIGOPRODUCTO, a.CCODIGOALMACEN, u.CABREVIATURA
            HAVING SUM(e.CENTRADASPERIODO12 - e.CSALIDASPERIODO12) <> 0;";
    }
}
