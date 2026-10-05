# Contrato `bridge-v1`

Contrato entre PolyConecta (camino 2) y el bridge de CONTPAQi (camino 1). Es el **único** punto de contacto entre los dos caminos (CT-02). Lo cumplen por igual el bridge real y el simulado (CT-21, D-122), y lo prueba la suite de `tests/PolyConecta.Contract.Tests` (CT-23).

**Estado:** ✏️ **Borrador para la sesión de los líderes** (tarea 0.2 de la spec `002-construccion-tecnica`). Se aprueba como `1.0` cuando lo firmen los dos líderes (CT-22).

| Líder | Firma | Fecha |
| :--- | :---: | :--- |
| L1 · Alejandro Ponce | ⬜ | |
| L2 · Luis Alvarado Martinez | ⬜ | |

**Cómo leer este borrador.** Lo marcado ✅ ya está decidido y tiene su decisión registrada. Lo marcado ✏️ tiene una propuesta inicial que los líderes aceptan o corrigen. Lo marcado ❓ está por definir. Cada sección dice quién la redacta (**R**) y quién la revisa (**V**). La OpenAPI (`bridge-v1.openapi.yaml`) se escribe al final, a partir de este documento.

---

## 1. Reglas generales

| # | Regla | Estado | Fuente |
| :---: | :--- | :---: | :--- |
| 1 | El contrato habla en **comandos de negocio** (CT-18), no en funciones del SDK. El `DOCUMENT_CREATE` genérico de hoy desaparece | ✅ | CT-18 |
| 2 | **PolyConecta manda los códigos de CONTPAQi** (producto, almacén, cliente), resueltos con su catálogo de mapeo | ✅ | D-121 |
| 3 | Cada cantidad viaja con su **unidad de medida**, la misma que tiene CONTPAQi para el producto. **Nadie convierte**: ni PolyConecta ni el bridge. El bridge solo valida que la unidad la admita el producto en CONTPAQi | ✅ | D-123, CT-16 |
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
| `contract_version` | string | sí | `"1.0"`. Una versión mayor distinta se rechaza | ✏️ nuevo |
| `command_type` | enum | sí | Uno de la sección 5 | ✏️ cambia |
| `variant` | string | según el comando | Variante de negocio que elige el concepto. Por ejemplo, en `TRASPASO`: `RECOLECCION`, `DEVOLUCION`, `CUARENTENA`, `LIBERACION`, `TRASLADO_SALIDA`, `TRASLADO_RECEPCION` | ✏️ nuevo |
| `idempotency_key` | string | sí | `{tipo}:{id}:{transición}` (CT-19). Hoy es opcional | ✏️ cambia |
| `correlation_id` | string | sí | Viaja hasta el SDK y los logs (CT-31) | ✏️ cambia |
| `client_app_id` | string | sí | `"polyconecta"` | ✅ ya existe |
| `callback_url` | string | sí | Endpoint de la API de PolyConecta | ✏️ cambia |
| `payload` | objeto | sí | Su forma depende del comando | ✏️ cambia |

**Acuse** (`202 Accepted`): `transaction_id`, `correlation_id`, `status`, `created_at` y, si es un reenvío, `is_duplicate: true` con el estado del original. Ya existe así.

**Rechazo inmediato** (`400`): la carga no cumple el esquema. Lleva el mismo modelo de error de la sección 4 y no crea transacción.

**Referencia de reconciliación.** El bridge deriva de la `idempotency_key` una referencia de 20 caracteres como máximo y la escribe en `CREFERENCIA` (CT-38, S-06). PolyConecta no la manda. ❓ Falta definir cómo se deriva (por ejemplo, un hash corto).

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

✏️ Propuesta: `FAILED` es definitivo cuando el error no se puede reintentar (`retryable: false`). Un error que sí se puede reintentar no se notifica hasta agotar los reintentos, y entonces pasa a `DEAD_LETTER`.

### Callback

`POST {callback_url}` en cada cambio a `CONFIRMED`, `FAILED` o `DEAD_LETTER`. Parte del `WebhookDispatcher` actual y agrega `idempotency_key`, `command_type`, `result` y `error` estructurados. Ver [ejemplos/callback.confirmado.json](ejemplos/callback.confirmado.json) y [ejemplos/callback.error.json](ejemplos/callback.error.json).

- **Firma** ✅ (D-121): el bridge firma cada callback con un secreto compartido. ✏️ Propuesta: cabecera `X-Bridge-Signature: t={unix},v1={HMAC-SHA256(secreto, t + "." + cuerpo)}`. PolyConecta rechaza con `401` una firma inválida o con más de 5 minutos de antigüedad. El secreto va por variable de entorno en los dos lados (CT-29).
- **Entrega** ✏️: si PolyConecta no responde `2xx`, el bridge reintenta con espera creciente. Si aun así no llega, PolyConecta consulta `GET /api/v1/transactions/{id}`, que devuelve lo mismo que el callback.
- **Orden** ✏️: PolyConecta acepta callbacks repetidos o desordenados. Un callback sobre una transacción ya confirmada no cambia nada.

---

## 4. Modelo de errores · R: L1 · V: L2

```json
{ "code": "EXISTENCIA_INSUFICIENTE_LOTE", "retryable": false,
  "message": "texto para Sistemas", "detail": { } }
```

`code` es estable y en mayúsculas; `message` puede cambiar. ✏️ Catálogo inicial:

| `code` | `retryable` | Cuándo | Fuente |
| :--- | :---: | :--- | :--- |
| `CARGA_INVALIDA` | no | No cumple el esquema | — |
| `VERSION_NO_SOPORTADA` | no | `contract_version` incompatible | CT-22 |
| `PRODUCTO_NO_EXISTE` / `PRODUCTO_INACTIVO` | no | Código desconocido o inactivo | D-112, S-14 |
| `ALMACEN_NO_EXISTE` | no | Código de almacén desconocido | — |
| `CLIENTE_NO_EXISTE` | no | Código de cliente desconocido | — |
| `UNIDAD_NO_ADMITIDA` | no | La unidad de la línea no es una que el producto admita en CONTPAQi | D-123 |
| `VARIANTE_SIN_CONCEPTO` | no | La configuración del bridge no tiene concepto para el comando y la variante | D-121 |
| `LOTES_NO_CUADRAN` | no | `Σ cantidad de lotes ≠ cantidad de la línea` | CT-39 |
| `EXISTENCIA_INSUFICIENTE` / `EXISTENCIA_INSUFICIENTE_LOTE` | no | Sin existencia en origen | CT-39 |
| `VERIFICACION_FALLIDA` | no | Lo registrado en CONTPAQi no coincide con la carga | CT-39 |
| `SDK_TIMEOUT` | sí | La llamada excedió su tiempo límite | CT-40 |
| `SDK_SESION` | sí | No hay sesión del SDK o se perdió | CT-40, D-108 |
| `SDK_ERROR` | ❓ | Código del SDK no clasificado; `detail.sdk_code` lleva el número | — |

---

## 5. Comandos

Cada comando se describe con la misma ficha. **Lado negocio** (R: L2): cuándo se dispara y qué datos manda PolyConecta. **Lado SDK** (R: L1): cómo se traduce, qué se valida y qué prueba lo respalda.

| Comando | Fase que lo usa | Estado en `1.0` |
| :--- | :--- | :---: |
| `ALTA_ALMACEN` | F3 (3.2) | ✏️ completo |
| `TRASPASO` | F3, F5, F7 | ✏️ completo, salvo el costo (T-17) |
| `ALTA_PEDIDO` | F2 (2.4) | ✏️ completo |
| `CIERRE_PRODUCCION` | F5 (5.1) | ✏️ completo (D-123) |
| `REMISION` | F6 (6.1) | ✏️ completo (D-123) |

### 5.1 `TRASPASO`

**Lado negocio.** Recolección (MP → WIP), devolución (WIP → MP), cuarentena y liberación, y traslado interplanta (origen → tránsito y tránsito → destino). Lo dispara la validación del documento de PolyConecta. Ejemplo: [ejemplos/traspaso.valido.json](ejemplos/traspaso.valido.json).

| Campo | Regla |
| :--- | :--- |
| `fecha` | Fecha de negocio del documento (`AAAA-MM-DD`), no la de envío, en la zona del servidor de CONTPAQi (D-123) |
| `referencia_negocio` | Folio de PolyConecta, solo informativo |
| `almacen_origen`, `almacen_destino` | Códigos de CONTPAQi |
| `lineas[]` | Una por producto (D-82): `producto`, `cantidad`, `unidad` y `lotes[]` con `numero` y `cantidad`, en la misma unidad de la línea. `lotes` va vacío si el producto no lleva lote, como la MP (D-106). Cantidad y unidad son las de CONTPAQi, sin conversión (D-123, P-24) |

**Lado SDK.** Se registra como un par Salida (origen) + Entrada (destino), con un movimiento por producto y N capas de lote (D-79, D-82). Lo respaldan B-02, B-03, C-02, C-03 y S-01.

**Resultado.** `documentos[]` con `rol` (`salida` o `entrada`), `concepto`, `folio` e `id_erp`.

❓ **Costo de la Entrada (T-17).** D-79 fija la regla; falta decidir quién la aplica y si el resultado devuelve el costo.

### 5.2 `ALTA_PEDIDO` · ✏️ por redactar en la sesión

Lado negocio: el pedido confirmado y autorizado en PolyConecta (D-53, D-113), con `cliente`, `fecha` (de negocio), `moneda`, `tipo_cambio` y `lineas[]` (`producto`, `cantidad`, `unidad`, `precio`; el precio es por esa unidad). Lado SDK: S-14. Resultado: `folio` e `id_erp`. Cancelación del pedido ya remisionado: D-114, por validar con la operación.

### 5.3 `ALTA_ALMACEN` · ✏️ por redactar en la sesión

Lado SDK: `fInsertaAlmacen` → `fSetDatoAlmacen` → `fGuardaAlmacen`, fijando `CFECHAALTAALMACEN` (D-110, S-09).

### 5.4 `CIERRE_PRODUCCION` · ✏️ por redactar en la sesión

Se define completo en `1.0` (D-123). Lado negocio: el cierre técnico de la OF (02 §4) con `almacen_wip`, `consumos[]` (producto, cantidad, unidad, y lotes si los lleva), `entradas[]` de PT con su lote nuevo y `subproductos[]` (scrap) con su almacén destino. Lado SDK: Salida desde WIP (consumo) + Entrada de PT y scrap con la capa del lote nuevo y su costo (D-111, S-11, S-12). ❓ Quién calcula el costo de la entrada de PT; se resuelve junto con T-17.

### 5.5 `REMISION` · ✏️ por redactar en la sesión

Se define completa en `1.0` (D-123). Lado negocio: la entrega validada (02 §5) con `cliente`, `fecha`, `almacen` (PT), `pedido_erp` de referencia y `lineas[]` con producto, cantidad, unidad y lotes. Lado SDK: remisión desde el almacén de PT; no se liga al pedido (S-13); al quedar remisionado todo el pedido, el bridge lo cancela en CONTPAQi (D-114, por validar). Resultado: `folio` e `id_erp`; queda pendiente de facturar.

---

## 6. Lecturas · R: L1 · V: L2

Ya existen en `CatalogsController`. ✏️ Propuesta de cambios: paginación con `limit` y `cursor`, filtro `modified_since` para sincronizar solo lo que cambió, y nombres en `snake_case` como el resto del contrato.

| Ruta | Hoy | Falta | Fase |
| :--- | :--- | :--- | :--- |
| `GET /api/v1/catalogs/products` | `search`, `limit` | Paginación, `modified_since`, **unidades que admite el producto** (base y alternas), si lleva lote | F1 |
| `GET /api/v1/catalogs/clients` | `search`, `limit` | Paginación, `modified_since` | F1 |
| `GET /api/v1/catalogs/warehouses` | sin filtros | — | F1 |
| `GET /api/v1/inventory/stocks` | un producto, almacén opcional; capas por lote | Varios productos por consulta, con la unidad base del producto en CONTPAQi | F1 |
| `GET /api/v1/inventory/purchases` | no existe | Recepciones de compra afectadas, con `modified_since` (D-102) | F3 |
| `GET /api/v1/catalogs/concepts` | existe | ❓ ¿Lo necesita PolyConecta, si el concepto lo elige el bridge (D-121)? | — |
| `GET /api/v1/invoices` | existe | ❓ ¿Sigue en el contrato? Ninguna fase lo usa | — |

---

## 7. Fuera del contrato

Las rutas de operación del bridge (`/dlq`, `/metrics`, `/logs`, `/health` y las de borrado de transacciones) son para Sistemas y el tablero del bridge. PolyConecta no las usa y pueden cambiar sin versión nueva. ❓ Confirmarlo en la sesión.

---

## 8. Versiones

Un cambio compatible (campo opcional nuevo, comando nuevo, código de error nuevo) sube la versión menor. Un cambio incompatible abre `v2`, que convive con `v1` hasta migrar. Todo cambio requiere a los dos líderes (CT-22) y se anota en la exploración de la spec de la fase en curso (CT-43).

---

## 9. Agenda de la sesión

1. **Lunes 5, tarde:** secciones 2, 3 y 4.
2. **Martes 6, mañana:** secciones 5 y 6, con los 5 comandos completos. El costo de `TRASPASO` y de `CIERRE_PRODUCCION` (T-17).
3. **Martes 6, tarde:** revisión cruzada, ejemplos completos en `ejemplos/` y firma de `1.0`. Después, la OpenAPI.
