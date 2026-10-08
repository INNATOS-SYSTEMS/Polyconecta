# Contrato `bridge-v1`

Contrato entre PolyConecta (camino 2) y el bridge de CONTPAQi (camino 1). Es el **único** punto de contacto entre los dos caminos (CT-02). Lo cumplen por igual el bridge real y el simulado (CT-21, D-122), y lo prueba la suite de `tests/PolyConecta.Contract.Tests` (CT-23).

**Estado:** ✅ **`1.1` aprobado** el 2026-10-08 por los dos líderes, en la ratificación de la spec 003 (CT-22). Sobre el `1.0` del 2026-10-06 (D-132) agrega, como cambio compatible, la clasificación de productos, la moneda y los domicilios de clientes, la lectura de agentes, el `id_erp` en productos y clientes y el agente de `ALTA_PEDIDO`, y declara obsoleto `modified_since` en productos y clientes (§6, §8). Todo cambio posterior sigue la sección 8.

| Líder | Firma | Fecha |
| :--- | :---: | :--- |
| L1 · Alejandro Ponce | ✅ | 2026-10-06 (`1.0`) · 2026-10-08 (`1.1`) |
| L2 · Luis Alvarado Martinez | ✅ | 2026-10-06 (`1.0`) · 2026-10-08 (`1.1`) |

**Cómo leer este contrato.** Cada regla cita la decisión que la respalda. Dos puntos se difirieron y viajan como campos opcionales: el costo de las entradas (T-17) y el precio de la remisión (P-26); cerrarlos es un cambio compatible (§8). Cada sección dice quién la redactó (**R**) y quién la revisó (**V**). La OpenAPI (`bridge-v1.openapi.yaml`) sale de este documento, que manda si difieren.

---

## 1. Reglas generales

| # | Regla | Estado | Fuente |
| :---: | :--- | :---: | :--- |
| 1 | El contrato habla en **comandos de negocio** (CT-18), no en funciones del SDK. El `DOCUMENT_CREATE` genérico de hoy desaparece | ✅ | CT-18 |
| 2 | **PolyConecta manda los códigos de CONTPAQi** (producto, almacén, cliente), resueltos con su catálogo de mapeo | ✅ | D-121 |
| 3 | Cada cantidad viaja con su **unidad de medida**, que es la **unidad base** del producto en CONTPAQi. **Nadie convierte**: ni PolyConecta ni el bridge. El bridge valida que la unidad sea la base del producto | ✅ | D-123, D-127, CT-16 |
| 4 | El **concepto de documento lo elige el bridge** por comando y variante, según su configuración. PolyConecta no lo manda | ✅ | D-121 |
| 5 | La fecha de un documento es su **fecha de negocio** en PolyConecta, no la de envío, y se interpreta en la **zona horaria del servidor de CONTPAQi** | ✅ | D-121, D-123 |
| 6 | Reenviar un comando con la misma `idempotency_key` nunca duplica un documento | ✅ | CT-19 |
| 7 | Los comandos se procesan en orden de llegada, uno a la vez | ✅ | CT-41 |
| 8 | Antes de escribir, el bridge valida; después de escribir, verifica lo registrado contra la carga | ✅ | CT-39 |

---

## 2. Sobre del comando · R: L1 · V: L2

`POST /api/v1/transactions`. Parte del `CreateTransactionRequest` actual.

| Campo | Tipo | Obligatorio | Regla | Estado |
| :--- | :--- | :---: | :--- | :---: |
| `contract_version` | string | sí | `"1.0"` o `"1.1"`. El bridge acepta cualquier versión menor de `1`; una versión mayor distinta se rechaza | ✅ D-132, `1.1` |
| `command_type` | enum | sí | Uno de la sección 5 | ✅ D-132 |
| `variant` | string | según el comando | Variante de negocio que elige el concepto. Por ejemplo, en `TRASPASO`: `RECOLECCION`, `DEVOLUCION`, `CUARENTENA`, `LIBERACION`, `TRASLADO_SALIDA`, `TRASLADO_RECEPCION` | ✅ D-132 |
| `idempotency_key` | string | sí | `{tipo}:{id}:{transición}` (CT-19). Hoy es opcional | ✅ D-132 |
| `correlation_id` | string | sí | Viaja hasta el SDK y los logs (CT-31) | ✅ D-132 |
| `client_app_id` | string | sí | `"polyconecta"` | ✅ ya existe |
| `callback_url` | string | sí | Endpoint de la API de PolyConecta | ✅ D-132 |
| `payload` | objeto | sí | Su forma depende del comando | ✅ D-132 |

**Acuse** (`202 Accepted`): `transaction_id`, `correlation_id`, `status`, `created_at` y, si es un reenvío, `is_duplicate: true` con el estado del original. Ya existe así.

**Rechazo inmediato** (`400`): la carga no cumple el esquema. Lleva el mismo modelo de error de la sección 4 y no crea transacción.

**Reconciliación con marca** ✅ (D-131). El bridge no escribe ninguna referencia propia en el documento: `CREFERENCIA` y los textos extra los captura el usuario, y `CGUIDDOCUMENTO` lo genera CONTPAQi. Antes de crear cada documento guarda en su base local la **marca** (el `CIDDOCUMENTO` más alto de `admDocumentos`) y la **huella** del documento: concepto, fecha, cliente o proveedor, `CTOTALUNIDADES`, número de movimientos y sus almacenes. Si el SDK falla o se pierde su respuesta, busca documentos por encima de la marca con ese concepto y esa huella: **ninguno**, el documento no se creó y el paso se reintenta; **uno**, se creó y el bridge toma su `CIDDOCUMENTO` y su folio; **más de uno**, se detiene con `RECONCILIACION_AMBIGUA` para revisión manual. PolyConecta no manda nada para esto.

---

## 3. Estados y callback · R: L2 · V: L1

### Estados

| Estado del bridge | Hoy se llama | Estado de sincronización en PolyConecta (CT-15) | Estado |
| :--- | :--- | :--- | :---: |
| `PENDING` | `PENDING` | `Enviado` | ✅ |
| `PROCESSING` | `PROCESSING` | `Enviado` | ✅ |
| `CONFIRMED` | `COMPLETED` | `Confirmado` | ✅ D-121 (cambia el nombre) |
| `FAILED` | (vuelve a `PENDING` con reintento) | `Error` | ✅ D-121 |
| `DEAD_LETTER` | `DEAD_LETTER_QUEUE` | `Error` | ✅ D-121 |

✅ (D-132): `FAILED` es definitivo cuando el error no se puede reintentar (`retryable: false`). Un error que sí se puede reintentar no se notifica hasta agotar los reintentos, y entonces pasa a `DEAD_LETTER`.

### Callback

`POST {callback_url}` en cada cambio a `CONFIRMED`, `FAILED` o `DEAD_LETTER`. Parte del `WebhookDispatcher` actual y agrega `idempotency_key`, `command_type`, `result` y `error` estructurados. Ver [ejemplos/callback.confirmado.json](ejemplos/callback.confirmado.json), [ejemplos/callback.error.json](ejemplos/callback.error.json) y [ejemplos/callback.dead-letter.json](ejemplos/callback.dead-letter.json).

- **Firma** ✅ (D-121): el bridge firma cada callback con un secreto compartido. Cabecera `X-Bridge-Signature: t={unix},v1={HMAC-SHA256(secreto, t + "." + cuerpo)}`. PolyConecta rechaza con `401` una firma inválida o con más de 5 minutos de antigüedad. El secreto va por variable de entorno en los dos lados (CT-29).
- **Entrega** ✅: si PolyConecta no responde `2xx`, el bridge reintenta con espera creciente. Si aun así no llega, PolyConecta consulta `GET /api/v1/transactions/{id}`, que devuelve lo mismo que el callback.
- **Orden** ✅: PolyConecta acepta callbacks repetidos o desordenados. Un callback sobre una transacción ya confirmada no cambia nada.
- **Reintento** ✅: un reenvío con la misma `idempotency_key` de una transacción en `FAILED` o `DEAD_LETTER` la **vuelve a encolar** con la carga guardada y responde `202` con `is_duplicate: true` y estado `PENDING`. Es como PolyConecta recupera un documento en `Error` sin cambiar la llave (CT-20). No duplica nada: esas transacciones no dejaron documento en CONTPAQi (CT-38). Un reenvío de una transacción `CONFIRMED`, `PENDING` o `PROCESSING` solo devuelve su estado.

---

## 4. Modelo de errores · R: L1 · V: L2

```json
{ "code": "EXISTENCIA_INSUFICIENTE_LOTE", "retryable": false,
  "message": "texto para Sistemas", "detail": { } }
```

`code` es estable y en mayúsculas; `message` puede cambiar. Catálogo ✅ (D-132, D-131):

| `code` | `retryable` | Cuándo | Fuente |
| :--- | :---: | :--- | :--- |
| `CARGA_INVALIDA` | no | No cumple el esquema | — |
| `VERSION_NO_SOPORTADA` | no | `contract_version` incompatible | CT-22 |
| `PRODUCTO_NO_EXISTE` / `PRODUCTO_INACTIVO` | no | Código desconocido o inactivo | D-112, S-14 |
| `ALMACEN_NO_EXISTE` | no | Código de almacén desconocido | — |
| `CLIENTE_NO_EXISTE` | no | Código de cliente desconocido | — |
| `AGENTE_NO_EXISTE` | no | `ALTA_PEDIDO` con un código de agente que no existe en `admAgentes`. Desde `1.1` | D-153 |
| `UNIDAD_NO_ADMITIDA` | no | La unidad de la línea no es la unidad base del producto en CONTPAQi | D-123, D-127 |
| `VARIANTE_SIN_CONCEPTO` | no | La configuración del bridge no tiene concepto para el comando y la variante | D-121 |
| `LOTES_NO_CUADRAN` | no | `Σ cantidad de lotes ≠ cantidad de la línea` | CT-39 |
| `EXISTENCIA_INSUFICIENTE` / `EXISTENCIA_INSUFICIENTE_LOTE` | no | Sin existencia en origen | CT-39 |
| `VERIFICACION_FALLIDA` | no | Lo registrado en CONTPAQi no coincide con la carga | CT-39 |
| `SDK_TIMEOUT` | sí | La llamada excedió su tiempo límite | CT-40 |
| `SDK_SESION` | sí | No hay sesión del SDK o se perdió | CT-40, D-108 |
| `SDK_ERROR` | no | Código del SDK no clasificado; `detail.sdk_code` lleva el número. No se reintenta: un error desconocido reintentado puede duplicar efectos (G-02) | G-02 |
| `ALMACEN_YA_EXISTE` | no | `ALTA_ALMACEN` con un código que ya existe con **otro** nombre. Con el mismo nombre no es error: responde `CONFIRMED` con su `id_erp` | D-110 |
| `MONEDA_NO_SOPORTADA` | no | `ALTA_PEDIDO` con una moneda que CONTPAQi no tiene dada de alta | S-14 |
| `RECONCILIACION_AMBIGUA` | no | Al reconciliar hay más de un documento posible por encima de la marca (§2). Va a revisión manual | D-131 |

---

## 5. Comandos

Cada comando se describe con la misma ficha. **Lado negocio** (R: L2): cuándo se dispara y qué datos manda PolyConecta. **Lado SDK** (R: L1): cómo se traduce, qué se valida y qué prueba lo respalda.

| Comando | Fase que lo usa | Estado en `1.0` |
| :--- | :--- | :---: |
| `ALTA_ALMACEN` | F3 (3.2) | ✅ |
| `TRASPASO` | F3, F5, F7 | ✅ (`costo` opcional, T-17) |
| `ALTA_PEDIDO` | F2 (2.4) | ✅ |
| `CIERRE_PRODUCCION` | F5 (5.1) | ✅ (D-123; `costo` opcional, T-17) |
| `REMISION` | F6 (6.1) | ✅ (D-123, D-114; `precio` opcional, P-26) |

### 5.1 `TRASPASO`

**Lado negocio.** Recolección (MP → WIP), devolución (WIP → MP), cuarentena y liberación, y traslado interplanta (origen → tránsito y tránsito → destino). Lo dispara la validación del documento de PolyConecta. Ejemplo: [ejemplos/traspaso.valido.json](ejemplos/traspaso.valido.json).

| Campo | Regla |
| :--- | :--- |
| `fecha` | Fecha de negocio del documento (`AAAA-MM-DD`), no la de envío, en la zona del servidor de CONTPAQi (D-123) |
| `referencia_negocio` | Folio de PolyConecta, solo informativo |
| `almacen_origen`, `almacen_destino` | Códigos de CONTPAQi |
| `lineas[]` | Una por producto (D-82): `producto`, `cantidad`, `unidad` y `lotes[]` con `numero` y `cantidad`, en la misma unidad de la línea. `lotes` va vacío si el producto no lleva lote, como la MP (D-106). Cantidad y unidad base del producto en CONTPAQi, sin conversión (D-123, D-127) |

**Lado SDK.** Se registra como un par Salida (origen) + Entrada (destino), con un movimiento por producto y N capas de lote (D-79, D-82). Lo respaldan B-02, B-03, C-02, C-03 y S-01.

**Resultado.** `documentos[]` con `rol` (`salida` o `entrada`), `concepto`, `folio` e `id_erp`.

**Costo de la Entrada.** D-79 fija la regla; quién la aplica se difirió (T-17). En `1.0` el resultado lleva un campo opcional `costo` por documento de entrada, que el bridge llena cuando lo conoce. Cerrarlo es un cambio compatible (§8).

### 5.2 `ALTA_PEDIDO`

**Lado negocio.** El pedido confirmado y autorizado con dos firmas en PolyConecta (D-53, D-113). Lo dispara la autorización. Ejemplo: [ejemplos/alta-pedido.valido.json](ejemplos/alta-pedido.valido.json).

| Campo | Regla |
| :--- | :--- |
| `fecha` | Fecha de negocio del pedido (`AAAA-MM-DD`) |
| `referencia_negocio` | Folio del pedido en PolyConecta |
| `cliente` | Código del cliente en CONTPAQi |
| `orden_compra_cliente` | Opcional. Orden de compra del cliente |
| `agente` | Opcional, desde `1.1`. Código del agente de CONTPAQi (`CCODIGOAGENTE`): el del usuario de AC que capturó el pedido, o el que eligió (D-153). Viaja en `tDocumento.aCodigoAgente` y queda en `admDocumentos.CIDAGENTE` |
| `moneda` | Código ISO de la moneda (`MXN`, `USD`). `admMonedas` no guarda un código ISO (solo id, nombre y símbolo), así que el bridge lo traduce a `CIDMONEDA` con su configuración, igual que los conceptos. Una moneda sin traducción falla con `MONEDA_NO_SOPORTADA` |
| `tipo_cambio` | Obligatorio si la moneda no es la base; `1` si lo es |
| `lineas[]` | `producto`, `cantidad`, `unidad` (base, D-127) y `precio` por esa unidad. IVA, descuentos y totales los calcula CONTPAQi (D-74) |

**Lado SDK.** Alta del documento de pedido con su concepto configurado y sus movimientos; precio, moneda y tipo de cambio por movimiento (S-14).

**Resultado.** `folio` e `id_erp` del pedido.

**Errores propios.** `CLIENTE_NO_EXISTE`, `PRODUCTO_NO_EXISTE`, `PRODUCTO_INACTIVO`, `UNIDAD_NO_ADMITIDA`, `MONEDA_NO_SOPORTADA` y, desde `1.1`, `AGENTE_NO_EXISTE`.

### 5.3 `ALTA_ALMACEN`

**Lado negocio.** Alta en CONTPAQi de un almacén que PolyConecta necesita, como los WIP por planta (D-110). Ejemplo: [ejemplos/alta-almacen.valido.json](ejemplos/alta-almacen.valido.json).

| Campo | Regla |
| :--- | :--- |
| `codigo` | Código del almacén en CONTPAQi |
| `nombre` | Nombre del almacén |
| `fecha_alta` | Fecha de negocio que se escribe en `CFECHAALTAALMACEN` |

**Lado SDK.** `fInsertaAlmacen` → `fSetDatoAlmacen` → `fGuardaAlmacen`, fijando `CFECHAALTAALMACEN` (D-110, S-09).

**Resultado.** `id_erp` del almacén. Si ya existe un almacén con el mismo código y el mismo nombre, responde `CONFIRMED` con su `id_erp` sin crear otro. Si el nombre es distinto, falla con `ALMACEN_YA_EXISTE`.

### 5.4 `CIERRE_PRODUCCION`

**Lado negocio.** El cierre técnico de la OF (02 §3), que se define completo en `1.0` (D-123). Ejemplo: [ejemplos/cierre-produccion.valido.json](ejemplos/cierre-produccion.valido.json).

| Campo | Regla |
| :--- | :--- |
| `fecha` | Fecha de negocio del cierre |
| `referencia_negocio` | Folio de la OF en PolyConecta |
| `almacen_wip` | Almacén WIP de donde se consume |
| `consumos[]` | `producto`, `cantidad`, `unidad` y `lotes[]` si el producto lleva lote (vacío para la MP, D-106) |
| `entradas[]` | Producto terminado: `producto`, `cantidad`, `unidad`, `almacen_destino` y `lotes[]` con el lote nuevo de cada rollo o paquete |
| `subproductos[]` | Scrap y otros subproductos: `producto`, `cantidad`, `unidad`, `almacen_destino` y `lotes[]` si lleva lote |

**Lado SDK.** Salida desde WIP por los consumos, y entrada de PT y de subproductos con la capa del lote nuevo y su costo (D-111, S-11, S-12). Las validaciones de CT-39 aplican a cada documento.

**Resultado.** `documentos[]` con `rol` (`consumo`, `entrada` o `subproducto`), `concepto`, `folio` e `id_erp`.

**Costo de la entrada de PT.** Igual que en `TRASPASO`: campo opcional `costo` en cada documento de entrada (T-17).

### 5.5 `REMISION`

**Lado negocio.** La entrega validada (02 §5), que se define completa en `1.0` (D-123). Ejemplo: [ejemplos/remision.valido.json](ejemplos/remision.valido.json).

| Campo | Regla |
| :--- | :--- |
| `fecha` | Fecha de negocio de la entrega |
| `referencia_negocio` | Folio de la entrega en PolyConecta |
| `cliente` | Código del cliente en CONTPAQi |
| `almacen` | Almacén de PT de donde sale |
| `pedido_erp` | Opcional. Folio del pedido en CONTPAQi, solo de referencia: el SDK no liga la remisión al pedido (S-13) |
| `cierra_pedido` | `true` cuando con esta remisión queda surtido todo el pedido. Lo sabe PolyConecta, que lleva lo entregado por línea. Con `true`, el bridge cancela el pedido en CONTPAQi (D-114, validada con la operación el 6-oct) |
| `lineas[]` | `producto`, `cantidad`, `unidad` (base), `lotes[]` liberados y `precio` opcional por esa unidad (P-26) |

**Lado SDK.** Remisión desde el almacén de PT, con sus lotes. Si `cierra_pedido`, cancelación del pedido (D-114).

**Resultado.** `folio` e `id_erp` de la remisión, que queda pendiente de facturar, y `pedido_cancelado: true` si se canceló el pedido.

**Precio de la remisión.** Se difirió (P-26). Como el SDK no liga la remisión al pedido (S-13), CONTPAQi no toma solo el precio del pedido. En `1.0` cada línea puede llevar `precio`, copiado del pedido en PolyConecta; antes de F6 se decide si es obligatorio o si Facturación lo captura al facturar.

---

## 6. Lecturas · R: L1 · V: L2

Las sirve `LecturasController` por SQL de solo lectura (CT-30). Todas responden en `snake_case`, y las paginadas usan `limit` (1 a 500) y `cursor` (el `next_cursor` de la página anterior) (D-132).

| Ruta | Qué devuelve | Desde | Fase |
| :--- | :--- | :---: | :--- |
| `GET /api/v1/catalogs/products` | Productos: `codigo`, `nombre`, `unidad_base`, `lleva_lote`, `activo`; desde `1.1`, `id_erp` y `clasificacion` | `1.0` | F1 |
| `GET /api/v1/catalogs/clients` | Clientes: `codigo`, `razon_social`, `rfc`; desde `1.1`, `id_erp`, `activo`, `moneda` y `domicilios[]` | `1.0` | F1 |
| `GET /api/v1/catalogs/agents` | Agentes: `codigo`, `nombre`, `id_erp`, `tipo` | `1.1` | F1 |
| `GET /api/v1/catalogs/warehouses` | Almacenes: `codigo`, `nombre`, `id_erp` (sin paginar) | `1.0` | F1 |
| `GET /api/v1/inventory/stocks` | Existencias de varios productos (`productos` separados por coma), por almacén y por lote, en la unidad base | `1.0` | F1 |
| `GET /api/v1/inventory/purchases` | Recepciones de compra afectadas, con `modified_since` (D-102) | `1.0` | F3 |

**Origen de cada campo en CONTPAQi:**

| Campo | Origen | Fuente |
| :--- | :--- | :--- |
| `unidad_base` | `CABREVIATURA` de `admUnidadesMedidaPeso`, vía `CIDUNIDADBASE` | D-127 |
| `lleva_lote` | Bit de lotes (16) de `CCONTROLEXISTENCIA` | — |
| `activo` (producto) | `CSTATUSPRODUCTO = 1` | — |
| `clasificacion` | `{ codigo, nombre }` del valor de "TIPO DE PRODUCTOS" (`CIDVALORCLASIFICACION{n}` → `admClasificacionesValores`; el número lo da la configuración del bridge). Solo es el valor inicial de la clasificación propia | A-05, D-86 |
| `moneda` | Código ISO de `admClientes.CIDMONEDA`, la moneda del cliente, traducido con `BridgeConfig__Monedas__{ISO}` como en `ALTA_PEDIDO` | D-146, D-150 |
| `domicilios[]` | `admDomicilios` con `CTIPOCATALOGO = 1` y `CIDCATALOGO` del cliente: `id_erp` (`CIDDIRECCION`), `tipo` (`fiscal` si `CTIPODIRECCION = 0`, `envio` si es 1), `calle`, `numero_exterior`, `numero_interior`, `colonia`, `codigo_postal`, `ciudad`, `municipio`, `estado`, `pais` y `sucursal`. Un cliente tiene un domicilio fiscal y N de envío | D-149, D-150 |
| Agentes | `admAgentes`: `CCODIGOAGENTE`, `CNOMBREAGENTE`, `CIDAGENTE`; `tipo` de `CTIPOAGENTE` (1 `venta`, 2 `venta_cobro`, 3 `cobro`) | D-153 |
| Existencias | Por producto y almacén, `CENTRADASPERIODO12 − CSALIDASPERIODO12` de `admExistenciaCosto` del ejercicio vigente; por lote, suma de `admCapasProducto.CEXISTENCIA` por número de lote | D-83, D-87 |

Los campos que trae `1.1` son **opcionales**: un consumidor `1.0` los ignora y PolyConecta funciona sin ellos.

**`modified_since` obsoleto en productos y clientes** (`1.1`, D-150). `CTIMESTAMP` de `admProductos` y `admClientes` no es la fecha de última modificación, así que el bridge no puede cumplir el filtro: si se manda, responde `501`, en el real y en el simulador. PolyConecta lee el catálogo completo y compara. Se conserva en las recepciones de compra, que se definen en F3.

**Fuera del contrato.** `GET /api/v1/catalogs/concepts` (el concepto lo elige el bridge, D-121) y `GET /api/v1/invoices` (ninguna fase lo usa) pasan a las rutas de operación (§7).

---

## 7. Fuera del contrato

Las rutas de operación del bridge (`/dlq`, `/metrics`, `/logs`, `/health` y las de borrado de transacciones) son para Sistemas y el tablero del bridge. PolyConecta no las usa y pueden cambiar sin versión nueva. Se agregan `catalogs/concepts`, `invoices` y, solo en modo simulado, `PUT /admin/simulated/faults` (spec 002, FR-008).

---

## 8. Versiones

Un cambio compatible (campo opcional nuevo, comando nuevo, código de error nuevo) sube la versión menor. Un cambio incompatible abre `v2`, que convive con `v1` hasta migrar. Todo cambio requiere a los dos líderes (CT-22) y se anota en la exploración de la spec de la fase en curso (CT-43).

| Versión | Fecha | Cambios | Decisiones |
| :--- | :--- | :--- | :--- |
| `1.0` | 2026-10-06 | Primera versión: sobre, estados y callback, errores, cinco comandos y lecturas | D-131, D-132 |
| `1.1` | 2026-10-08 | Compatible. Productos: `id_erp` y `clasificacion`. Clientes: `id_erp`, `activo`, `moneda` y `domicilios[]`. Lectura nueva `GET /catalogs/agents`. `ALTA_PEDIDO`: `agente` opcional y error `AGENTE_NO_EXISTE`. `modified_since` obsoleto en productos y clientes: el bridge real ya respondía `501` y ningún consumidor dependía de él, por eso los líderes lo declararon obsoleto en vez de abrir `v2` | D-150, D-153; spec 003 |

---

## 9. Aprobación

Sesión de firma del 2026-10-06, con los dos líderes. Las 20 decisiones se tomaron en la página de cierre de F0 y quedaron así: 16 propuestas aceptadas, la referencia de reconciliación corregida (D-131), D-114 validada con la operación, y T-17 y el precio de la remisión diferidos como campos opcionales (P-26). Los ejemplos de [`ejemplos/`](ejemplos/) traen una carga válida y una inválida por comando, más los tres callbacks (C-T005). La OpenAPI se validó con `@redocly/cli@2.58.1` sin errores (C-T007).

**`1.1`.** Aprobado el 2026-10-08 por los dos líderes en la [hoja de ratificación de la spec 003](https://claude.ai/artifact/GBKCh2pAF9r4CppZGT9Smx). Los ejemplos de lectura están en [`ejemplos/lecturas/`](ejemplos/lecturas/) y los de `ALTA_PEDIDO` con agente, en [`ejemplos/pendientes-1.1/`](ejemplos/pendientes-1.1/) hasta que el bridge acepte `agente` (C-T003) y la OpenAPI `1.1.0` se validó con `@redocly/cli@2.58.1` sin errores (C-T002).
