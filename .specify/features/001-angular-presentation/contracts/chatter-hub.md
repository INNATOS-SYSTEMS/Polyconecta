# Contrato: `ChatterHub`

Hub de SignalR en `PolyConecta.Api` (FR-015, D-58). Es una copia de `PolyConecta.Presentation/Hubs/ChatterHub.cs`, que no se toca (D-60).

## Conexión

| | |
| :--- | :--- |
| URL | `http://localhost:9020/hubs/chatter` |
| Transporte | El que negocie `@microsoft/signalr`: WebSockets primero |
| Autenticación | Ninguna. La réplica no tiene usuarios |
| CORS | Política con nombre que admite el origen `http://localhost:4200`, con cualquier encabezado y método y **con credenciales**. No sirve `AllowAnyOrigin`, porque el cliente negocia con credenciales (research R-07) |

## Métodos

### Cliente → servidor: `SendMessage`

| Parámetro | Tipo | Valor |
| :--- | :--- | :--- |
| `documentId` | string | Folio del documento, tal como aparece en la ruta: `BOL-2026-0001`, `PIM/OUT/48214` |
| `author` | string | `Administrator`, como en el prototipo |
| `text` | string | Texto del mensaje. Uno vacío o solo con espacios no se envía |

### Servidor → clientes: `ReceiveChatterMessage`

Se emite a **todos** los clientes conectados (`Clients.All`), igual que el hub del prototipo.

| Parámetro | Tipo | Valor |
| :--- | :--- | :--- |
| `documentId` | string | El mismo que se envió |
| `author` | string | El mismo que se envió |
| `text` | string | El mismo que se envió |
| `timestamp` | string | `DateTime.UtcNow.ToString("g")`. El cliente **no lo muestra** (research R-07) |

## Comportamiento del cliente

1. **Al abrir un formulario con chatter**, el cliente se conecta si no lo está. Si no puede, el panel muestra "Sin conexión en vivo" y sigue funcionando en modo local.
2. **Al enviar con conexión**, llama a `SendMessage` y **no** agrega el mensaje local. Lo agrega cuando le llega su propio `ReceiveChatterMessage`, así no se duplica.
3. **Al recibir**, solo agrega los mensajes cuyo `documentId` es el del documento abierto, con la hora local de recepción en formato `h:mm tt`.
4. **Al enviar sin conexión**, agrega el mensaje local, como en Blazor.
5. **Si la conexión se pierde**, intenta reconectarse automáticamente (`withAutomaticReconnect`) y el panel indica el estado.
6. **Los mensajes no se guardan.** Al recargar o al reiniciar la API se pierden, igual que en el prototipo. Guardarlos en la base (D-78) es trabajo de la tarea 1.6 de la spec 003.
