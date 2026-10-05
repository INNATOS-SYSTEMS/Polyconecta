# Modelo de datos: Construcción técnica (F0)

F0 solo crea la **base común**: lo que heredan o usan todas las entidades de negocio desde F1. El modelo de negocio está en [04-modelo-de-dominio.md](../../../docs/diseno/04-modelo-de-dominio.md) y lo construye cada fase.

---

## 1. PolyConecta · `PolyConecta.Domain`

### `AuditableEntity` (clase base abstracta, `Domain/Common/`)

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `Id` | `long` (`BIGINT IDENTITY`) | Inmutable |
| `CreatedAt` | `DateTimeOffset` | Lo llena el `DbContext` al insertar, con `IClock` |
| `CreatedBy` | `string(100)` | Usuario de `ICurrentUser`. En F0, `"sistema"` hasta que exista identidad (F1) |
| `ModifiedAt`, `ModifiedBy` | igual, anulables | Los llena el `DbContext` al actualizar |
| `RowVersion` | `byte[]` (`rowversion`) | Concurrencia optimista (R-03) |

### `ArchivableEntity : AuditableEntity`

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `IsActive` | `bool`, verdadero por omisión | `Archive()` y `Restore()`. Hay un filtro global de EF que oculta lo archivado; las consultas que lo necesitan usan `IgnoreQueryFilters()`. Un borrado físico de algo referenciado se rechaza en el dominio (04 §1) |

### `IStatefulDocument<TState>` (interfaz, `Domain/Common/`)

- **Qué expone:** `TState State { get; }`, un enum cerrado, sin `set` público.
- **Cómo cambia el estado:** solo con métodos de transición con nombre (`Confirmar()`, `Cancelar()`…). Cada uno valida sus precondiciones y llama a `RecordTransition(from, to, nota)`.
- **Qué pasa con la transición:** `RecordTransition` deja un evento de dominio. El `DbContext` lo convierte en `StateTransitionLog` en el mismo `SaveChanges` (CT-32).
- **Error:** intentar una transición inválida lanza `TransicionInvalidaException` con el estado actual y el pedido.

### `SyncState` (tipo propio, `Domain/Common/`)

Va en todo documento que escribe en CONTPAQi (CT-13, CT-15).

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `Status` | enum `NoAplica`, `Pendiente`, `Enviado`, `Confirmado`, `Error` | Independiente del estado de negocio |
| `ErpFolio` | `string(50)`, anulable | Folio que devuelve el bridge |
| `ErpId` | `string(50)`, anulable | Id del documento en CONTPAQi |
| `ErpDocuments` | JSON, anulable | `documentos[]` del resultado (por ejemplo, la salida y la entrada de un `TRASPASO`) |
| `LastErrorCode`, `LastErrorMessage` | `string(60)`, `string(500)`, anulables | Código estable del contrato y mensaje |
| `LastSyncAt` | `DateTimeOffset`, anulable | |

**Transiciones del estado de sincronización:**

| Desde | Hacia | Qué la provoca |
| :--- | :--- | :--- |
| `NoAplica` | `Pendiente` | El documento encola un comando |
| `Pendiente` | `Enviado` | El despachador recibe `202` |
| `Enviado` | `Confirmado` | Callback `CONFIRMED` |
| `Enviado` | `Error` | Callback `FAILED` o `DEAD_LETTER`, o reintentos de red agotados |
| `Error` | `Pendiente` | `ReintentarSincronizacion` |

Un callback sobre un documento `Confirmado` no cambia nada (contrato §3, "Orden").

### `StateTransitionLog` (esquema `plt`, `Domain/Plataforma/`)

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `Id` | `bigint` | |
| `EntityType` | `string(100)` | Nombre del tipo de documento |
| `EntityId` | `bigint` | |
| `FromState`, `ToState` | `string(50)` | |
| `UserName` | `string(100)` | |
| `Role` | `string(50)`, anulable | Rol ejercido. Se llena desde F1 |
| `OccurredAt` | `DateTimeOffset` | |
| `Note` | `string(500)`, anulable | |
| `CorrelationId` | `string(64)` | CT-31 |

Índice en `(EntityType, EntityId, OccurredAt)`. Solo se inserta: no tiene actualización ni borrado.

### `ReferenceSequence` (esquema `plt`)

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `DocumentType` | `string(50)`, llave primaria | Por ejemplo, `PEDIDO` u `OF_EXTRUSION` |
| `Prefix` | `string(20)` | Puede llevar `{yyyy}` o `{planta}`, que se sustituyen al generar |
| `Padding` | `int` | Dígitos del consecutivo |
| `NextNumber` | `bigint` | |
| `ResetRule` | enum `Nunca`, `Anual`, `Mensual` | |
| `CurrentPeriod` | `string(7)`, anulable | `2026` o `2026-10`. Si cambia el periodo, `NextNumber` vuelve a 1 |

**`IReferenceSequenceService.NextAsync(documentType, contexto)`:**
- Lee la fila con `UPDLOCK, ROWLOCK` dentro de la transacción del caso de uso, incrementa y devuelve el folio formateado.
- Si se pide un tipo inexistente, lanza un error; nunca se crea solo.
- **Concurrencia:** 50 peticiones simultáneas dan 50 folios distintos y consecutivos (escenario 6 de US-4).

Reemplaza al value object `Folio` (`Domain/ValueObjects/Folio.cs`), que se borra junto con sus usos.

### `OutboxMessage` (esquema `plt`)

Sustituye a la clase actual de `Domain/Entities/OutboxMessage.cs`.

| Campo | Tipo | Regla |
| :--- | :--- | :--- |
| `Id` | `Guid` | |
| `Sequence` | `bigint IDENTITY`, único | Orden de envío (CT-41) |
| `CommandType` | `string(30)` | `TRASPASO`, `ALTA_PEDIDO`… |
| `Variant` | `string(30)`, anulable | Contrato §2 |
| `Payload` | JSON (`nvarchar(max)`) | La carga del contrato ya traducida |
| `IdempotencyKey` | `string(150)`, único | `{tipo}:{id}:{transición}` (CT-19) |
| `CorrelationId` | `string(64)` | CT-31 |
| `DocumentType`, `DocumentId` | `string(100)`, `bigint` | Documento al que se le actualiza el `SyncState` |
| `LockKeys` | JSON | `["producto:PT1113 C567","almacen:WIP-SC"]`, para D-95 |
| `Status` | enum `Pendiente`, `Enviado`, `Confirmado`, `Error`, `Bloqueado` | |
| `Attempts` | `int` | Reintentos de red |
| `NextAttemptAt` | `DateTimeOffset`, anulable | Espera creciente |
| `BridgeTransactionId` | `string(64)`, anulable | Lo devuelve el `202` |
| `LastErrorCode`, `LastErrorMessage` | anulables | |
| `CreatedAt`, `SentAt`, `CompletedAt` | `DateTimeOffset` | |

Índices en `(Status, Sequence)` y en `IdempotencyKey` (único).

**Regla de escritura:** se inserta en el mismo `SaveChanges` que el cambio de negocio. Lo garantiza el `TransactionDecorator`: si falla el negocio, no queda el mensaje (CT-20).

---

## 2. Bridge · SQLite (`PolyConecta.Contpaq`)

Las tablas actuales (`DbInitializer.cs`) se conservan. Cambios de F0:

| Tabla | Cambio | Para qué |
| :--- | :--- | :--- |
| `transactions` | Agrega `contract_version`, `variant` y `result_json`. `status` usa los nombres del contrato (`CONFIRMED` en lugar de `COMPLETED`, `DEAD_LETTER` en lugar de `DEAD_LETTER_QUEUE`), con migración de datos existentes | Contrato §2 y §3 |
| `simulated_folio` (nueva) | `concepto` (PK) y `ultimo_folio` | Folios simulados por concepto (CT-21) |
| `callback_attempts` (nueva) | `transaction_id`, `attempt`, `status_code` y `sent_at` | Reintentos del callback firmado y diagnóstico |

La ejecución por pasos con reconciliación (CT-38) agrega su propia tabla en F2 (tarea 2.2), no en F0.

---

## 3. Fuera de F0

Identidad, roles y `ICurrentUser` real (1.2); catálogos y `plt.erp_mapping` (1.3); chatter guardado (1.6); y todas las entidades de negocio de 04 §2 en adelante.
