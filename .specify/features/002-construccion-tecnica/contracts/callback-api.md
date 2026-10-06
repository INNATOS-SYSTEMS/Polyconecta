# Contrato: callback del bridge en PolyConecta

Endpoint de `PolyConecta.Api` que recibe los callbacks del contrato `bridge-v1` (§3 de [bridge-v1.md](../../../../docs/contratos/bridge-v1.md)). El cuerpo lo define el contrato del bridge; este documento fija cómo lo recibe PolyConecta.

## Ruta

`POST /api/v1/plataforma/bridge/callbacks`

Es el `callback_url` que el despachador manda en cada comando. La URL base sale de `Erp__CallbackBaseUrl`.

## Autenticación

Firma con secreto compartido (D-121). El secreto va en `Erp__CallbackSecret` (CT-29).

| Caso | Respuesta |
| :--- | :--- |
| Falta `X-Bridge-Signature` o no tiene el formato `t={unix},v1={hex}` | `401` |
| `HMAC-SHA256(secreto, t + "." + cuerpo_crudo)` no coincide (comparación de tiempo constante) | `401` |
| `t` difiere más de 5 minutos de la hora del servidor | `401` |

El cuerpo se lee crudo antes de deserializar, para firmar exactamente lo recibido.

## Procesamiento

1. Busca el `OutboxMessage` por `idempotency_key`. Si no existe, responde `404` y registra el caso con su `correlation_id`.
2. **Si el mensaje ya está `Confirmado`**, responde `200` sin cambios: el callback puede llegar repetido o tarde.
3. **Si el estado es `CONFIRMED`**:
   - guarda `folio`, `id_erp` y `documentos[]` en el `SyncState` del documento;
   - pasa el `SyncState` y el mensaje a `Confirmado`;
   - libera a los `Bloqueado` que dependían de él.
4. **Si el estado es `FAILED` o `DEAD_LETTER`**:
   - guarda `error.code` y `error.message`;
   - pasa el `SyncState` y el mensaje a `Error`;
   - los posteriores con llaves en común siguen bloqueados (D-95).
5. **Cualquier otro estado** (`PENDING`, `PROCESSING`) responde `200` sin cambios.

Los pasos 3 y 4 los hace el caso de uso `ConfirmarSincronizacion`, dentro de una transacción.

## Respuestas

| Código | Cuándo |
| :--- | :--- |
| `200` | Procesado o ignorado sin error |
| `400` | El cuerpo no cumple el esquema del callback |
| `401` | Firma inválida o vencida |
| `404` | `idempotency_key` desconocida |

Un `5xx` hace que el bridge reintente (contrato §3, "Entrega").

## Pruebas

En `tests/PolyConecta.IntegrationTests`:
- firma válida e inválida;
- firma vencida;
- callback repetido;
- `FAILED` con bloqueo de llaves.
