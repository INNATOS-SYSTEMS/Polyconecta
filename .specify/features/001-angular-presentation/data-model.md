# Modelo de datos: Presentación en Angular

Es el modelo del **prototipo**, tipado en TypeScript, no el de [04-modelo-de-dominio.md](../../../docs/diseno/04-modelo-de-dominio.md). Ese se adopta al conectar la API. La fuente de cada tipo es su clase en `PolyConecta.Presentation/Services/`. Los nombres de campo se pasan a camelCase (FR-006). Los textos de estado se conservan en español y con las mismas cadenas, porque la paridad compara texto visible.

**Cantidad y unidad.** Toda línea y toda asignación de lote lleva `cantidad` (número) y `unidad`. En modo libre, la unidad es la unidad base del producto en CONTPAQi, que en la réplica es `ProductRef.unidad` de la semilla (D-127).

**Montos.** El prototipo usa `decimal` y la réplica usa `number`. Lo que la pantalla muestra se formatea igual que en Blazor (separadores y decimales), y la fase 1 lo verifica con las pruebas de FR-008.

---

## 1. Tipos de `core/models/`

### Inventario (`InventoryState.cs`)

| Tipo | Campos | Notas |
| :--- | :--- | :--- |
| `ProductClass` | `Bolsa`, `RolloImpreso`, `RolloLiso`, `RolloMaestro`, `MateriaPrima`, `Scrap` | Unión de cadenas. La etiqueta visible sale de `classLabel()` |
| `LotStatus` | `Libre`, `Reservado`, `EnWip`, `Cuarentena` | |
| `ProductRef` | `clave`, `nombre`, `clasificacion: ProductClass`, `unidad` (por omisión `KGS`) | **Lo crea la tarea 0.5** (lo usa `OdooLineCapture`) |
| `LotBalance` | `lote`, `clave`, `ubicacion`, `cantidad`, `estado: LotStatus`, `comprometidoPor?` | Un lote en una ubicación. La reserva es por lote. **Lo crea la 0.5** |
| `StockQuant` | `producto: ProductRef`, `ubicacion`, `lote`, `cantidad` | Renglón del Inventario Actual |

### Operaciones de almacén (`StockOperationState.cs`)

| Tipo | Campos | Notas |
| :--- | :--- | :--- |
| `OperationType` | `codigo`, `nombre`, `origen`, `destino`, `eventoContpaq`, `reversa?`, `esDevolucion` | Catálogo fijo de la semilla |
| `LotAllocation` | `lote`, `cantidad` | **Lo crea la 0.5** |
| `StockOperationLine` | `clave`, `producto`, `unidad`, `solicitado`, `asignaciones: LotAllocation[]`, `entregado`; calculados `declarado`, `pendiente = max(0, solicitado − entregado)` | **Lo crea la 0.5** |
| `StockOperation` | `folio`, `tipo`, `ofFolio`, `origen`, `destino`, `fecha`, `backorderDe?`, `warning?`, `error?`, `lineas`, `step`, `contpaqId`; calculados `state`, `esParcial`, `operacion`, `fechaLimite = fecha + 1 día`, `totalSolicitado`, `totalEntregado` | `step`: 0 Borrador · 1 En espera · 2 Listo · 3 Hecho |

### Producción y flujo (`OperationalFlowState.cs`)

| Tipo | Campos | Notas |
| :--- | :--- | :--- |
| `BomLine` | `clave`, `producto`, `cantidad`, `unidad` | Componente de una OF |
| `SubProductLine` | `clave`, `producto`, `cantidad`, `unidad`, `producido`, `almacenDestino` | |
| `PlanningLine` | `centroTrabajo`, `producto`, `cantidad`, `unidad`, `horasAsignadas`, `fechaInicio`, `fechaFin`, `operador` | |
| `ProductionLot` | `lote`, `real`, `unidad`, `estado: 'En revisión' \| 'Aprobado' \| 'Rechazado'` | **Lo crea la 0.5** (lo usa `LotPickerModal`) |
| `ManufacturingOrder` | `folio`, `processType: 'Extrusion' \| 'Impresion' \| 'Bolseo'`, `processLabel`, `producto`, `empresa`, `cantidad`, `unidad`, `tiempoEstimadoHrs`, `numeroRollos`, `calidadRequerida`, `almacenFalla`, `fechaEsperada`, `state`, `originFolio?`, `pedidoFolio`, `componentes`, `subproductos`, `produccion`, `planeacion`, `sequenceCounter`; calculados `numeroLabel`, `producidoTotal` (sin rechazados) | `state`: Borrador · Planeado · En progreso · Hecho. Sin `originFolio` es la OF maestra |
| `QualityControlState` | `manufacturingOrderFolio`, `folio`, `auditor`, `processLabel` | |
| `SalesOrderLine` | `clave`, `producto`, `cantidad`, `unidad`, `precioUnitario`; calculado `subtotal` | En modo libre se agrega `moneda` (D-74) |
| `ProcessCheck` | `proceso`, `activo`, `origen`, `producto` | |
| `ShipmentLine` | `clave`, `producto`, `demanda`, `entregado`, `unidad`, `lotesSeleccionados: string[]` | |
| `Incidencia` | `fecha`, `centroTrabajo`, `tipo`, `comentarios`, `horaInicio`, `horaFin` | |

### Documentos que pasan a colección (research R-02)

En el prototipo son un objeto único dentro de `OperationalFlowState`. En la réplica son colecciones cuyo primer elemento es la semilla.

| Tipo nuevo | Sale de | Campos | Semilla |
| :--- | :--- | :--- | :--- |
| `SalesOrder` | `CurrentOrderStage`, `OrdenCompraCliente`, `Agente`, `ContpaqId`, `CantidadPedido`, `PedidoLineas`, `Procesos` y `Firmas` | `folio`, `cliente`, `stage`, `ordenCompraCliente`, `agente`, `contpaqId`, `cantidadPedido`, `lineas: SalesOrderLine[]`, `procesos: ProcessCheck[]`, `firmas: Record<rol, quién>`, `libre: boolean` | `IV310-26`, `libre = false` |
| `InterplantTransfer` | `InterplantTransferState` | Los mismos, más `libre` | `PIM/OUT/48213` |
| `Reception` | `ReceptionState` | Los mismos, más `libre` | `SC/IN/50974` |
| `Delivery` | `DeliveryState` | Los mismos, más `cliente` y `libre` | `SC/OUT/31688` |

`libre` marca un documento creado con "Nuevo". Sirve para que sus smart buttons de origen salgan vacíos o deshabilitados (FR-014) y para aplicar sus restricciones (FR-012).

### Búsqueda (`SearchViews.cs`)

| Tipo | Campos |
| :--- | :--- |
| `SearchField<T>` | `etiqueta`, `valor: (t: T) => string \| undefined` |
| `SearchFilter<T>` | `nombre`, `campo`, `condicion: (t: T) => boolean` |
| `SearchGroupBy<T>` | `etiqueta`, `clave: (t: T) => string` |
| `SearchView<T>` | campos, filtros y agrupaciones; `aplicar(items, texto, filtrosActivos)` |

**Regla de `aplicar` (FR-009):**
- El texto se busca en todos los campos.
- Los filtros activos del mismo `campo` se combinan con O.
- Los filtros de campos distintos se combinan con Y.

---

## 2. Servicios de `core/state/`

Son servicios de Angular con signals. Cada operación del prototipo conserva su nombre en camelCase. Los cuatro dependen entre sí como en Blazor: `OperationalFlowState` recibe `InventoryState` y `StockOperationState`.

| Servicio | Fuente | Estado (signals) | Operaciones |
| :--- | :--- | :--- | :--- |
| `UiViewState` | `UiViewState.cs` | modo de vista, tipo de documento y etapas, texto de búsqueda | `setViewMode`, `setDocumentType`, `setSearchQuery` |
| `InventoryState` | `InventoryState.cs` | productos, lotes | `getProducto`, `fisico`, `reservado`, `enWip`, `disponible`, `lotesDe`, `disponibleDeLote`, `lotesDisponibles`, `existencias`, `reservar`, `liberarReservasDe`, `moverAWip`, `devolverDeWip`, `saldoWip`, `saldoWipTotal`; estáticas `classLabel`, `esVendible` |
| `StockOperationState` | `StockOperationState.cs` | tipos de operación, operaciones | `getTipo`, `get`, `deOf`, `tieneRecoleccion`, `asegurarRecoleccion`, `confirmarRecoleccion`, `emitirDevolucion`, `asignarLote`, `quitarLote`, `comprobarDisponibilidad`, `validar` (devuelve `{ op, error }`), `cancelar`, `puedeCerrarOf` (devuelve `{ ok, motivo }`), `totalRecolectado`, `totalDevuelto` |
| `OperationalFlowState` | `OperationalFlowState.cs` | pedidos, OF, controles de calidad, traslados, recepciones, entregas, incidencias | Las del prototipo. Las que actuaban sobre el documento único reciben su folio: `autorizar(folio)`, `revocarFirmas(folio)`, `validarTraslado(folio)`, etc. Más `agregarLineaPedido`, `planear`, `registrarPesajeRollo`, `aprobarLote`, `rechazarLote`, `cerrarProduccion`, `resetAll` |

Los parámetros `out` de C# se devuelven como objeto. El `Notify()` del prototipo desaparece: los signals propagan los cambios.

---

## 3. Reglas que se prueban (FR-008, FR-012)

| Regla | Dónde vive | Prueba |
| :--- | :--- | :--- |
| Autorizar requiere dos firmas (Comercial y Cobranza) y solo en Confirmado | `autorizar`, `revocarFirmas` | La primera firma no cambia la etapa; la segunda la pasa a Autorizado. Revocar borra las firmas, libera reservas y regresa a Confirmado |
| No se cierra la OF con lotes en revisión si requiere calidad (hard-stop) | `cerrarProduccion` | Con un lote "En revisión", la OF no pasa a Hecho. Cuando todas las OF están en Hecho, el pedido pasa a Hecho |
| No se cierra la OF con saldo en WIP | `puedeCerrarOf` | Con saldo en WIP, devuelve el mismo motivo que Blazor |
| Rechazar marca el lote como Rechazado y le agrega `.S` una sola vez | `rechazarLote` | El nombre termina en `.S`, sin duplicarlo, y el lote no cuenta en `producidoTotal` |
| La disponibilidad excluye la cuarentena | `disponible`, `lotesDisponibles` | Un lote en cuarentena no suma ni se ofrece |
| La validación parcial genera backorder | `validar` | Lo pendiente pasa a una operación nueva con `backorderDe` |
| Lotes de OF libre con folio de la OF raíz (D-54) | creación libre de OF | `R001-BOL-2026-0007`, con `/` cambiado por `-` |
| Recepción libre solo con lotes en `TRANS/*` (D-56) | creación libre de recepción | Solo se ofrecen lotes en tránsito |
| Traslado y entrega libres solo con lotes liberados | creación libre | No se ofrecen lotes en revisión ni rechazados |
| Recolección libre deja saldo sin asignar (D-55) | creación libre de recolección | El saldo aparece en WIP sin OF |
| Asignar saldo de WIP es manual (FR-013) | `asignarSaldoWip` (nueva) | Nada se asigna sin la acción explícita |
| Pedido libre recibe un Contpaq ID simulado al confirmar (D-53) | creación libre de pedido | Recibe el ID y sigue el flujo de dos firmas |
| Toda línea libre lleva cantidad y la unidad base del producto en CONTPAQi (D-127) | captura de línea en modo libre | La unidad sale de `ProductRef.unidad` y no se edita; no se guarda una línea sin cantidad; no hay conversión |

La fase 1 completa la tabla con cada regla que encuentre al portar los servicios, porque FR-008 da ejemplos, no una lista cerrada.
