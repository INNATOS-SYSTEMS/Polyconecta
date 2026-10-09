# API de F1: sesión, seguridad, catálogos, sincronización, pedidos y chatter

Rutas nuevas de `PolyConecta.Api` en F1. Las listas de cada recurso usan la consulta de [api-listas.md](api-listas.md). Todas exigen sesión salvo `POST /plataforma/sesion` y el callback del bridge de F0. Los errores usan `ProblemDetails` con un `code` estable:

| HTTP | `code` | Cuándo |
| :--- | :--- | :--- |
| `400` | `VALIDACION` | La petición no cumple sus reglas; `errores[]` con campo y mensaje |
| `401` | `SIN_SESION` | Sin sesión o vencida |
| `403` | `PERMISO_DENEGADO` | El usuario no tiene el permiso; `razon` lleva el texto que pinta la interfaz |
| `404` | `NO_ENCONTRADO` | |
| `409` | `DOCUMENTO_MODIFICADO` | La `rowVersion` no coincide: otro usuario lo cambió |
| `409` | `TRANSICION_INVALIDA` | La transición no procede desde el estado actual; `razon` lo explica |
| `409` | `EDICION_REVOCA_AUTORIZACION` | Editar un pedido con firmas sin `revocarAutorizacion: true` (D-147) |
| `409` | `SINCRONIZACION_EN_CURSO` | Ya corre una sincronización de ese catálogo |

Toda petición que escribe lleva `X-Requested-With: PolyConecta` (R-01) y propaga `X-Correlation-ID` (CT-31).

---

## Sesión

| Método y ruta | Cuerpo | Respuesta |
| :--- | :--- | :--- |
| `POST /api/v1/plataforma/sesion` | `{ usuario, contrasena }` | `200` con la sesión y la cookie; `401` `CREDENCIALES_INVALIDAS` o `USUARIO_BLOQUEADO` |
| `GET /api/v1/plataforma/sesion` | — | `200` con la sesión, `401` sin ella |
| `DELETE /api/v1/plataforma/sesion` | — | `204` y borra la cookie |

```json
{
  "usuario": { "id": 7, "usuario": "cvillarreal", "nombre": "Celia Villarreal" },
  "asignaciones": [ { "grupo": "ATENCION_CLIENTES", "nombreGrupo": "Atención a Clientes", "planta": "PIM", "suplente": false } ],
  "permisos": ["ventas.pedido.leer", "ventas.pedido.crear", "ventas.pedido.confirmar"]
}
```

`permisos` sirve a la web para mostrar menús. La regla la decide la API en cada acción (CT-11).

## Usuarios y grupos (Administrador)

| Método y ruta | Qué hace | Permiso |
| :--- | :--- | :--- |
| `GET /api/v1/plataforma/usuarios/{id}` | Detalle con asignaciones | `plataforma.usuarios.leer` |
| `POST /api/v1/plataforma/usuarios` | Crea: `{ usuario, nombre, email?, contrasena, asignaciones[] }` (al menos una) | `plataforma.usuarios.administrar` |
| `PUT /api/v1/plataforma/usuarios/{id}` | Cambia nombre, email y asignaciones; `rowVersion` | ídem |
| `POST /api/v1/plataforma/usuarios/{id}/archivar` · `/restaurar` | | ídem |
| `POST /api/v1/plataforma/usuarios/{id}/contrasena` | Restablece: `{ contrasena }` | ídem |
| `GET /api/v1/plataforma/grupos/{id}` | Detalle con sus permisos | `plataforma.grupos.leer` |
| `GET /api/v1/plataforma/permisos` | Árbol del catálogo: módulo › objeto › acción (D-148) | `plataforma.grupos.leer` |
| `POST /api/v1/plataforma/grupos` | Crea: `{ codigo, nombre, descripcion?, copiarDe? }` | `plataforma.grupos.administrar` |
| `PUT /api/v1/plataforma/grupos/{id}` | Cambia nombre, descripción y **la lista completa de claves de permiso** asignadas; `rowVersion` | ídem |
| `POST /api/v1/plataforma/grupos/{id}/archivar` · `/restaurar` | Archivar exige que no tenga miembros activos | ídem |

`asignaciones[]`: `{ grupoId, plantaId, suplente }` (ids de `plt.group` y `plt.plant`); el detalle agrega el nombre del grupo y la clave de la planta. Los dos paneles mandan la lista final de permisos del grupo, no altas y bajas sueltas.

## Catálogos

Con el prefijo de su módulo (D-155): productos, clasificaciones y almacenes son de Inventario; clientes y agentes, de Ventas.

| Método y ruta | Qué hace | Permiso |
| :--- | :--- | :--- |
| `GET /api/v1/inventario/productos/{id}` | Producto con unidad, clasificación y ficha técnica | `inventario.producto.leer` |
| `PUT /api/v1/inventario/productos/{id}/clasificacion` | `{ clasificacionId }` | `inventario.producto.clasificar` |
| `PUT /api/v1/inventario/productos/{id}/ficha-tecnica` | `{ rollo: {…}, pt: {…} }`, los dos (FR-018) | `inventario.ficha.editar` |
| `GET /api/v1/inventario/clasificaciones` · `POST` · `PUT /{id}` | Catálogo de clasificación (D-86) | leer / `inventario.producto.clasificar` |
| `GET /api/v1/ventas/clientes/{id}` | Cliente con moneda y domicilios | `ventas.cliente.leer` |
| `GET /api/v1/ventas/clientes/buscar?texto=` | Para el selector del pedido: activos, hasta 20, con moneda y domicilios | `ventas.pedido.crear` |
| `GET /api/v1/inventario/productos/buscar?texto=` | Para la captura de líneas: activos, hasta 20, con unidad base | `ventas.pedido.crear` |
| `GET /api/v1/inventario/almacenes` | Almacenes de CONTPAQi sincronizados | `inventario.almacen.leer` |
| `GET /api/v1/ventas/agentes` | Agentes de CONTPAQi sincronizados, para el selector del pedido y el usuario (D-153) | `ventas.agente.leer` |
| `PUT /api/v1/plataforma/usuarios/{id}/agente` | `{ agenteId }`: liga el usuario a su agente de CONTPAQi | `plataforma.usuarios.ligar_agente` |

## Sincronización

| Método y ruta | Qué hace | Permiso |
| :--- | :--- | :--- |
| `GET /api/v1/plataforma/sincronizacion` | Estado por catálogo: última corrida, resultado, leídos, cambiados y error | `plataforma.sincronizacion.leer` |
| `POST /api/v1/plataforma/sincronizacion` | Corre los tres catálogos y responde el estado | `plataforma.sincronizacion.ejecutar` |
| `POST /api/v1/plataforma/sincronizacion/{catalogo}` | `productos`, `clientes`, `agentes` o `almacenes` | ídem |

## Pedidos de venta

| Método y ruta | Qué hace | Permiso |
| :--- | :--- | :--- |
| `GET /api/v1/ventas/pedidos/{id}` | Pedido con líneas, firmas, `acciones` y `rowVersion` | `ventas.pedido.leer` |
| `POST /api/v1/ventas/pedidos` | Crea en Borrador (maestro y líneas en un solo guardado, D-136) | `ventas.pedido.crear` |
| `PUT /api/v1/ventas/pedidos/{id}` | Edita maestro y líneas; `rowVersion` y `revocarAutorizacion` (D-147) | `ventas.pedido.editar` |
| `POST /api/v1/ventas/pedidos/{id}/confirmar` | `{ rowVersion }` | `ventas.pedido.confirmar` |
| `POST /api/v1/ventas/pedidos/{id}/autorizar` | `{ rowVersion, rol? }`; `rol` solo si el usuario puede firmar por los dos y ninguno ha firmado | `firmar_comercial` o `firmar_cobranza` |
| `POST /api/v1/ventas/pedidos/{id}/revocar` | `{ rowVersion, motivo }` | `ventas.pedido.revocar` |
| `POST /api/v1/ventas/pedidos/{id}/cancelar` | `{ rowVersion, motivo }` | `ventas.pedido.cancelar` |

Cuerpo de crear y editar:

```json
{
  "rowVersion": "AAAAAAAAB9E=",
  "revocarAutorizacion": false,
  "clienteId": 3,
  "ordenCompraCliente": "OC-4471",
  "agenteId": 4,
  "fechaPedido": "2026-10-13",
  "fechaPromesa": "2026-10-30",
  "domicilioEntregaId": 11,
  "moneda": "USD",
  "tipoCambio": 18.5,
  "lineas": [
    { "id": null, "productoId": 42, "cantidad": 1200, "precioUnitario": 0.85, "metaProduccionKg": null, "toleranciaPorcentaje": null }
  ]
}
```

La unidad de la línea no viaja: es la base del producto (D-127). Una línea con `id` se actualiza; sin `id` se agrega; una que ya no viene se elimina (solo en Borrador y Confirmado).

Respuesta del detalle:

```json
{
  "id": 15, "folio": "PV-2026-0015", "estado": "Confirmado", "rowVersion": "AAAAAAAAB9I=",
  "cliente": { "id": 3, "clave": "EMM-001", "nombre": "EMPRESA MEXICANA DE MANUFACTURA" },
  "moneda": "USD", "tipoCambio": 18.5, "domicilioEntrega": { "id": 11, "texto": "Av. Industrial 120, Apodaca, N.L." },
  "lineas": [ { "id": 31, "producto": "PT1113 C567 - BOLSA MEDIANA 44X84 C.430 BOL-004 [77]", "cantidad": 1200, "unidad": "PZA", "precioUnitario": 0.85 } ],
  "firmas": [ { "rol": "Comercial", "usuario": "Ana Treviño", "suplente": false, "fecha": "2026-10-13T10:12:00-06:00" } ],
  "sincronizacion": { "estado": "NoAplica" },
  "acciones": [
    { "accion": "autorizar", "disponible": false, "razon": "Ya firmaste este pedido; la otra firma la da otra persona" },
    { "accion": "revocar", "disponible": true, "razon": null },
    { "accion": "editar", "disponible": true, "razon": null, "aviso": "El pedido tiene 1 firma: guardar un cambio revoca la autorización" }
  ]
}
```

## Chatter

| Método y ruta | Qué hace |
| :--- | :--- |
| `GET /api/v1/plataforma/chatter/{tipo}/{id}?antesDe=` | Historial, del más reciente al más antiguo, en páginas de 50. Exige el permiso de lectura del documento |
| Hub `/hubs/chatter`, `UnirseADocumento(tipo, id)` y `SalirDeDocumento(tipo, id)` | Suscribe al grupo de SignalR del documento |
| Hub, `EnviarMensaje(tipo, id, clase, texto)` | `clase` = `Mensaje` o `Nota`; guarda con el autor de la sesión y transmite `MensajeChatter` al grupo |

El hub exige sesión (la misma cookie, por el mismo origen, R-01). Los registros de cambio llegan por `MensajeChatter` después de confirmarse la transacción (R-04).
