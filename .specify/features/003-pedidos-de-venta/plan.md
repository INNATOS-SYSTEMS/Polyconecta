# Implementation Plan: Pedidos de venta (F1)

> **Secciones por líder (CT-34, D-120).** Lo común va primero y lo acuerdan los dos líderes. Después, la sección **L1** la edita solo Alejandro Ponce y la **L2** solo Luis Alvarado Martinez.

**Branch**: `003-pedidos-de-venta` | **Date**: 2026-10-08 | **Spec**: [spec.md](spec.md)

**Status**: Plan completo, ajustado con la ratificación del 8-oct (D-151 a D-153).

---

## Común

### Summary

F1 entrega el primer documento de negocio sobre la API: el **pedido libre con dos firmas**. Para sostenerlo:

- **identidad propia** con grupos configurables por planta y suplentes (D-32, D-148);
- **sincronización** de productos, clientes (con moneda y domicilios) y almacenes desde el bridge;
- **clasificación propia y ficha técnica** del producto;
- **listas en el servidor**, favoritos por usuario y **chatter guardado**;
- en el bridge, la **sesión permanente** del SDK, las lecturas completas del contrato y el cotejo de T-06.

**Enfoque técnico:**
- Identity solo para credenciales, con cookie y mismo origen. Grupos y permisos son del dominio, y un decorador de autorización corre antes de cada caso de uso (R-01, R-02).
- Listas híbridas: hasta 5,000 filas, una sola consulta y todo lo demás en el navegador; arriba del umbral, cada consulta se resuelve en el servidor con la vista de búsqueda declarada allá. Resuelve P-28 (R-03, D-151).
- El interceptor de auditoría de F0 también escribe el chatter de cada transición (R-04).
- La sincronización es un caso de uso por catálogo, periódico y bajo demanda, que lee completo y escribe solo lo que cambió, con bloqueo de aplicación (R-05, D-150).
- Las pantallas de Pedidos ya existen: solo cambian su origen de datos y sus acciones a la API (R-10).

Detalles y alternativas en [research.md](research.md).

### Technical Context

**Language/Version**: .NET 10 (SDK 10.0.300 o mayor 10.0.x, D-133) y C#; Angular 22.2.1 con TypeScript 6.0.3 y Node 24.16.0 (CT-04).

**Primary Dependencies**: las de F0 (EF Core 10.0.12, SignalR, Swashbuckle 10.2.3), más **`Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.12** (nuevo, R-01). En la web, las de la spec 011 (TanStack Table, Angular CDK, Spartan `brain`, Lucide) y `@microsoft/signalr` 10.0.11. Nada más.

**Storage**: SQL Server 2022. Esquema nuevo `ven`; tablas nuevas en `plt` e `inv` ([data-model.md](data-model.md)). Migraciones `F1_*`, una por tarea que agrega entidades.

**Testing**:
- Dominio: cada regla del pedido, las firmas (RF-3, RF-4, D-34, D-38), la revocación y la edición con firmas (D-147).
- Aplicación e integración contra SQL Server en contenedor: autorización por permiso y planta, consulta de listas (incluida la cuenta de consultas), sincronización con el bridge simulado, chatter.
- Contrato: lecturas de F1 contra el simulador y contra el real.
- Web: Vitest por componente y servicio, galería `/catalogo` para los componentes nuevos, y Playwright extremo a extremo de F1 contra API y bridge simulado.

**Target Platform**: igual que F0. API y web en macOS, Linux y CI; bridge real en el VPS (`win-x86`).

**Project Type**: servicio web, SPA y bridge.

**Performance Goals**: SC-005 (lista de 500 pedidos en menos de 1 s por consulta) y SC-006 (una hora con una sola sesión del SDK).

**Constraints**:
- Ninguna escritura en CONTPAQi en F1.
- La web y la API, en el mismo origen (proxy de desarrollo; proxy inverso en producción).
- Ninguna regla de permisos en la interfaz ni en los controladores (CT-11).
- Las pantallas existentes no cambian su estructura (07).
- Límite de la carga inicial de la web: 89 kB (D-143).

**Scale/Scope**: 7 tareas del plan (1.1 a 1.7), 5 historias, 32 requisitos, 5 lecturas del contrato, unas 21 entidades nuevas y 5 listas HTTP.

### Constitution Check

*Se revisa antes de investigar y otra vez después del diseño.*

| Principio o regla | Cómo se cumple | Antes | Después |
| :--- | :--- | :---: | :---: |
| I · CONTPAQi es el sistema de registro | Productos, clientes y almacenes se copian de CONTPAQi y son de solo lectura; el pedido guarda `erp_*` para F2 | ✅ | ✅ |
| II · Toda escritura a CONTPAQi va por outbox y bridge | F1 no escribe en CONTPAQi. Las lecturas son SQL de solo lectura en el bridge (CT-30) | ✅ | ✅ |
| III · Catálogos | El producto es único por código de CONTPAQi; la clasificación y la ficha son de PolyConecta (CT-14) | ✅ | ✅ |
| V · Aprobaciones digitales | Dos firmas de personas distintas, con suplentes y atribución (RF-3, RF-4, D-34, D-38) | ✅ | ✅ |
| VII · Respaldo técnico | Precio por movimiento: `tMovimiento.aPrecio` y S-14. `CTIMESTAMP`, la moneda del cliente (`CIDMONEDA`) y los domicilios (1 fiscal, N de envío) los verificó el usuario en CONTPAQi (D-150) | ⚠️ | ✅ |
| VIII · Realidad de CONTPAQi | Columnas de `admClientes`, `admDomicilios` y `admMovimientos` tomadas de `Referencia_BD_CONTPAQi.md` | ✅ | ✅ |
| IX · Odoo 19 | Lista, kanban, formulario y chatter con los contratos visuales; componentes nuevos primero a 07 y la galería (R-10) | ✅ | ✅ |
| X · Documentos libres | El pedido es libre ("Nuevo"). Su modo ligado por sincronización quedó fuera de alcance por decisión del usuario (D-145) | ⚠️ | ✅ |
| CT-11 · Reglas fuera de la interfaz | Decorador de autorización y `AccionesDisponibles` en el dominio; la web solo pinta | ✅ | ✅ |
| CT-12 · Esquema por módulo | `ven` nuevo; seguridad en `plt`; catálogos en `inv` | ✅ | ✅ |
| CT-24 · Contratos visuales | Dos paneles e inicio de sesión entran a 07 y a `/catalogo` antes de usarse | ✅ | ✅ |
| CT-26 · Acciones deshabilitadas con razón | `acciones[]` con `razon` en cada detalle; la API rechaza igual | ✅ | ✅ |
| CT-29, CT-30 · Secretos y privilegios | Contraseña del Administrador inicial por variable de entorno; la API sigue sin DDL | ✅ | ✅ |
| CT-32 · Bitácora | Toda operación del pedido, también la firma y la revocación, en `StateTransitionLog` con el grupo ejercido | ✅ | ✅ |
| CT-36 · Versiones | Un solo paquete nuevo, del mismo tren de 10.0.12, anotado en la exploración | ✅ | ✅ |
| CT-40 · Sesión del SDK | Una sesión por proceso, timeout por llamada y reinicio diario (R-07) | ✅ | ✅ |
| CT-43 · Exploración | Los cambios del plan están en "Exploración y cambios" | ✅ | ✅ |

**Nada queda en ⚠️.** Los tres datos que dependían del VPS los verificó el usuario el 8-oct (D-150). Lo único sin verificar es el cotejo de T-06, que es tarea de la fase (FR-008) y no condiciona el diseño.

### Contrato

El contrato vive en [docs/contratos/bridge-v1.md](../../../docs/contratos/bridge-v1.md). F1 usa sus lecturas (§6) y no envía comandos.

| Punto | Propuesta | Fuente |
| :--- | :--- | :--- |
| `1.1`, compatible, aprobado el 8-oct | `clasificacion` en productos; `moneda` y `domicilios[]` en clientes; `GET /catalogs/agents` y `agente` opcional en `ALTA_PEDIDO`; `modified_since` obsoleto | FR-003, research R-11, D-150, D-153 |
| `SDK_TIMEOUT` | Ya existe; 1.1 lo usa al vencer el tiempo límite de una llamada | CT-40, R-07 |
| Rutas de operación del simulador | `PUT /admin/simulated/catalog/…` para cambiar el catálogo semilla en caliente (fuera del contrato, §7) | R-05 |

El contrato HTTP propio de PolyConecta está en [contracts/api-listas.md](contracts/api-listas.md), que resuelve P-28, y en [contracts/api-f1.md](contracts/api-f1.md).

### Project Structure

#### Documentación (esta spec)

```text
.specify/features/003-pedidos-de-venta/
├── spec.md
├── plan.md            # este archivo: Común, L1, L2
├── research.md        # R-01 a R-11
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── api-listas.md  # consulta de listas y favoritos (P-28)
│   └── api-f1.md      # sesión, seguridad, catálogos, sincronización, pedidos y chatter
├── checklists/requirements.md
└── tasks.md
```

#### Código

```text
docs/contratos/
├── bridge-v1.md                         # 1.1 al aprobarse (común)
└── ejemplos/                            # lecturas de productos y clientes 1.1

PolyConecta.Contpaq/                     # L1
├── Infrastructure/Sdk/                  # sesión permanente, vigilante de timeout, reinicio diario (1.1)
├── Infrastructure/Persistence/          # SqlContractReadRepository: moneda, domicilios, clasificación, existencias por lote de productos (1.4)
└── Simulated/                           # seed.json ampliado y rutas de operación del catálogo (común)

PolyConecta.Domain/
├── Plataforma/Seguridad/                # User, Group, GroupAssignment, Permission, Permisos (catálogo), Plant
├── Plataforma/Chatter/                  # ChatterMessage
├── Plataforma/Listas/                   # SavedSearch
├── Plataforma/Sincronizacion/           # CatalogSyncState
├── Inventario/                          # Product, PackagingUnit, ProductClassification, RollSpecification, PtSpecification, ErpWarehouse
└── Ventas/                              # Customer, CustomerAddress, SalesOrder, SalesOrderLine, AuthorizationSignature

PolyConecta.Application/
├── Common/                              # AuthorizationDecorator, IRequierePermiso, ICurrentUser ampliado
├── Common/Listas/                       # VistaDeBusqueda<T>, ConsultaLista, ResultadoLista, IConsultaDeLista<T>
├── Plataforma/Seguridad/                # casos de uso de usuarios y grupos, IReglaDeFila<T>
├── Plataforma/Chatter/                  # PublicarMensaje, IChatterNotificador
├── Plataforma/Sincronizacion/           # SincronizarCatalogo, SincronizarTodo, IBridgeLecturas
├── Inventario/                          # ClasificarProducto, GuardarFichaTecnica
└── Ventas/                              # CrearPedido, EditarPedido, ConfirmarPedido, AutorizarPedido, RevocarAutorizacion, CancelarPedido

PolyConecta.Infrastructure/
├── Plataforma/Identidad/                # CredencialUsuario, IdentityUserContext, CurrentUser desde la cookie
├── Plataforma/Listas/                   # traductor ConsultaLista → IQueryable
├── Persistence/                         # configuraciones de plt, inv y ven; migraciones F1_*; sembradores
├── Persistence/AuditoriaInterceptor.cs  # + mensaje de chatter por transición
└── Erp/                                 # BridgeLecturasHttp, SincronizadorCatalogos (BackgroundService)

PolyConecta.Api/
├── Controllers/Plataforma/              # Sesion, Usuarios, Grupos, Permisos, Favoritos, Sincronizacion, Chatter
├── Controllers/Catalogos/               # Productos, Clasificaciones, Clientes, Almacenes
├── Controllers/Ventas/                  # Pedidos
├── Controllers/ListasController.cs      # /{modulo}/{lista}/vista y /consulta
└── Hubs/ChatterHub.cs                   # [Authorize], grupos por documento

PolyConecta.Web/src/app/
├── core/lista/origen-http.ts            # OrigenHttp<T>
├── core/lista/favoritos-http.ts         # AlmacenDeFavoritos por la API
├── core/sesion/                         # SesionState desde la API, guardia de rutas, interceptor 401 y X-Requested-With
├── shared/odoo-dual-list/               # dos paneles en árbol (contrato nuevo, D-148)
├── features/plataforma/                 # login, usuarios, grupos, sincronización
├── features/catalogos/                  # productos (con ficha técnica), clientes
└── features/ventas/                     # lista, kanban, formulario y "Nuevo" sobre la API

tests/
├── PolyConecta.Domain.Tests/Ventas/     # pedido, firmas, revocación, edición con firmas
├── PolyConecta.Application.Tests/       # autorización, listas, sincronización
├── PolyConecta.IntegrationTests/        # API de F1 contra SQL Server y bridge simulado
└── PolyConecta.Contract.Tests/Lecturas/ # lecturas de F1 y 1.1
```

---

## L1 · Camino 1 · Alejandro Ponce

### 1.1 · Sesión permanente (R-07)

- Separar en `ContpaqiSdkGateway` el ciclo del **SDK** (una vez por proceso) del de la **empresa** (por lote). `IdleSessionTimeoutSeconds` solo cierra la empresa.
- Vigilante de tiempo límite por llamada nativa: al vencer, `SDK_TIMEOUT`, registro con `correlation_id` y salida del proceso para que lo levante la tarea de D-115.
- `BridgeConfig__ReinicioDiario`: deja de tomar comandos, termina el actual, cierra con `fTerminaSDK` y sale con código 0.
- Pruebas en `tests/Contpaq.Bridge.Tests` con un gateway nativo falso (orden de llamadas, timeout, reinicio). En el VPS, quickstart §6 con evidencia en `evidence/F1/1.1.md`.

### 1.4 · Lecturas y cotejo (R-06, R-11)

- Lectura completa paginada de productos y clientes; `modified_since` responde `501` (D-150). Clientes con `CIDMONEDA` y sus domicilios (1 fiscal, N de envío). Medir en el VPS la duración de la lectura completa.
- Campos de `1.1` en el modo real, detrás de `BridgeConfig__Contrato__Expone11` hasta la aprobación.
- Existencias de varios productos por consulta (lotes de 100).
- Ampliar `Simulated/seed.json` y agregar las rutas de operación del catálogo simulado (común, con L2).
- Cotejo de T-06 (F-01, F-02 y F-05) con la UI, con evidencia en `evidence/F1/1.4.md`.

---

## L2 · Camino 2 · Luis Alvarado Martinez

### 1.2 · Identidad, grupos y permisos (R-01, R-02)

- Identity con `IdentityUserContext` y cookie; `ICurrentUser` desde la sesión; proxy de desarrollo de Angular.
- Dominio de seguridad, catálogo de permisos en código y sembradores (plantas, permisos, diez grupos, Administrador inicial).
- `AuthorizationDecorator` antes de la validación; `PermisoDenegadoException` → `403` con razón.
- Pantallas: inicio de sesión, usuarios (lista y formulario con asignaciones por planta) y grupos (lista y formulario con los dos paneles). **Antes**, el contrato de `pc-odoo-dual-list` en 07 y su ejemplo en `/catalogo`.

### 1.3 · Catálogos (R-05, R-08)

- Entidades de `inv` y `ven`, `IBridgeLecturas` y `SincronizarCatalogo`, `SincronizarTodo` y `SincronizadorCatalogos`.
- Pantallas: productos (lista, formulario con pestaña de ficha técnica y clasificación), clientes (lista y formulario con domicilios, solo lectura) y sincronización (estado por catálogo, "Sincronizar ahora" por catálogo y "Sincronizar todo").

### 1.5 · Pedido (R-09)

- `SalesOrder` con sus operaciones y `AccionesDisponibles`; casos de uso; API de pedidos.
- La web de Pedidos cambia de `OperationalFlowState` a la API: `pedidos-acciones.ts` usa `OrigenHttp` y los endpoints de transición; "Nuevo" y el formulario guardan por la API, con el selector de cliente que propone moneda y domicilio, y la captura `[Producto] [Cantidad] [Unidad] [Precio unitario]`. El diálogo de D-147 al guardar con firmas.
- El botón inteligente "Pedido" de la OF en memoria queda deshabilitado con su razón (R-10).

### 1.6 · Listas, favoritos y chatter (R-03, R-04)

- `VistaDeBusqueda<T>` y el traductor a `IQueryable`; `ListasController`; `OrigenHttp` y `FavoritosHttp`.
- `ChatterMessage`, interceptor, hub con grupos por documento y `PublicarMensaje`; la web del chatter carga el historial por la API.
- Prueba de rendimiento con 500 pedidos y la cuenta de consultas (SC-005).

### 1.7 · R1

- `scripts/sql/f1-datos-r1.sql` o un sembrador de desarrollo con los usuarios de la [tabla de R1](quickstart.md#tabla-de-usuarios-de-r1), `scripts/dev/sembrar-pedidos.sh` y el guion de la revisión (FR-032).

---

## Complexity Tracking

Sin violaciones a la constitución. Dos puntos de riesgo de horas, ya señalados en la spec:

| Riesgo | Por qué | Qué se mueve si no cabe |
| :--- | :--- | :--- |
| 1.2 creció con la administración de grupos y el componente de dos paneles (D-148) | Los 9 h del plan cubrían identidad y roles fijos | Primero, la copia de grupos (US2, escenario 7) |
| 1.3 creció con la clasificación, la ficha técnica y los domicilios (D-145, D-149) | Los 9 h del plan cubrían solo la sincronización | La ficha técnica pasa a F2, que es la que la consume |

Lo que se mueva se registra en "Exploración y cambios" de la spec con su decisión.
