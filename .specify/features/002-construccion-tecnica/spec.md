# Feature Specification: Construcción técnica (F0)

**Feature Branch**: `002-construccion-tecnica`

**Created**: 2026-10-05

**Status**: Borrador. Se ratifica cuando la aprueben los dos líderes.

**Fase del plan**: F0 · Construcción técnica, del 5 al 9 de octubre de 2026. Sin revisión con la operación; su salida la usan F1 y F2.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 0.1 a 0.9 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F0 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-51 (secretos), D-62 y D-63 (tablas propias y contrato), D-67 a D-73 (versiones), D-76 y D-77 (CI y ambientes), D-79 a D-82 (traspaso, pasos, validación, lotes), D-88, D-91, D-108 y D-115 (sesión del SDK y arranque), D-92 y D-95 (orden), D-110 a D-114 (comandos de la segunda ronda), D-116 a D-120 (fases, líderes y specs).

---

## Contexto para el agente

Lee esto antes de cualquier tarea.

- **Qué es F0.** Es la base que necesitan las fases siguientes: el **contrato del bridge**, la solución migrada a **.NET 10 con SQL Server**, el **bridge simulado**, la **base común** de PolyConecta (auditoría, bitácora de estados, folios y outbox), la **aplicación web vacía** con su estilo, la **integración continua** y el **servidor del conector** listo para operar. F0 no tiene funcionalidad de negocio visible para la operación.
- **El contrato es la pieza central.** Las specs de F1 y F2 dependen de él (ROADMAP §4). Se escribe primero (tarea 0.2), entre los dos líderes, y a partir de él trabajan por separado: L1 lo cumple con el simulador y el bridge real, y L2 lo consume con el despachador.
- **Fuentes.** Las reglas técnicas están en [06-constitucion-tecnica.md](../../../docs/diseno/06-constitucion-tecnica.md): CT-04 a CT-06, CT-07 y CT-08, CT-13, CT-15, CT-17 a CT-23, CT-27, CT-29 a CT-32, CT-36, CT-38 a CT-41. El modelo está en [04-modelo-de-dominio.md §1, §3 y §4](../../../docs/diseno/04-modelo-de-dominio.md). El estado actual del código y su deuda están en [05-arquitectura-tecnica.md §3 y §4](../../../docs/diseno/05-arquitectura-tecnica.md). Lo verificado del SDK está en `docs/contpaq/` y en el bloque S de la matriz.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F0. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| `docs/contratos/bridge-v1.md` y su OpenAPI, con los 5 comandos de CT-18, las lecturas de CT-17 y los callbacks | Implementar los comandos en el bridge real: `TRASPASO` en F3, `ALTA_PEDIDO` en F2, `CIERRE_PRODUCCION` en F5, `REMISION` en F6 |
| Migración de todos los proyectos .NET a .NET 10, con versiones centralizadas (CT-36) | Identidad, roles y permisos (1.2, F1) |
| Persistencia en SQL Server con migraciones; nuevo proyecto `PolyConecta.Application` | Sincronización de catálogos y mapeo ERP `plt.erp_mapping` (1.3, F1) |
| Mixins de auditoría y archivado, `StateTransitionLog`, `IReferenceSequenceService`, outbox transaccional y despachador hacia el contrato | Chatter guardado en la base, vistas de búsqueda y agrupaciones (1.6, F1) |
| Bridge simulado que cumple el contrato completo y corre en macOS y en CI | Tablero de sincronización y estado visible en formularios (2.5 y 2.7, F2) |
| Bridge en .NET 10 `win-x86` verificado contra el laboratorio | Sesión de larga duración del bridge real con doble inicio de sesión (1.1, F1); ejecución por pasos con reconciliación (2.2, F2) |
| Aplicación Angular: proyecto, estilos, layout, navegación y componentes compartidos | Las páginas: las hace la spec 001 en paralelo y cada fase sobre la API (D-118) |
| CI en GitHub Actions con SQL Server 2022 y la suite de contrato contra el simulador | Hosting de producción y respaldos (H-01, H-02) |
| Credenciales rotadas, login de solo lectura e inicio de sesión automático del administrador con la tarea del bridge | Cambios en `PolyConecta.Presentation` (prototipo, D-60) |

---

## User Scenarios & Testing *(mandatory)*

Los "usuarios" de F0 son los dos líderes y sus agentes. Cada historia se prueba sola.

### User Story 1 - Contrato `bridge-v1` acordado (Priority: P1) · Común (0.2)

Los dos líderes publican el contrato con el que se comunican PolyConecta y el conector: qué comandos existen, qué datos lleva cada uno, qué responde y qué errores devuelve. Desde ese momento, cada camino puede trabajar sin esperar al otro.

**Why this priority**: todo lo demás de F0 y las specs de F1 y F2 dependen de él.

**Independent Test**: el OpenAPI valida con una herramienta estándar; cada comando tiene una carga de ejemplo válida y una inválida; los dos líderes firman el documento.

**Acceptance Scenarios**:

1. **Given** el catálogo de comandos de CT-18, **When** se revisa `bridge-v1.md`, **Then** cada comando (`ALTA_ALMACEN`, `TRASPASO`, `ALTA_PEDIDO`, `CIERRE_PRODUCCION`, `REMISION`) declara carga, resultado, errores y su traducción al SDK con la decisión o prueba que la respalda.
2. **Given** un comando reenviado con la misma `idempotency_key`, **When** se lee el contrato, **Then** está definido qué responde el bridge y que nunca duplica el documento (CT-19).
3. **Given** las lecturas de CT-17, **When** se revisa el contrato, **Then** catálogos, existencias y recepciones de compra tienen ruta, filtros, paginación y forma de respuesta.

---

### User Story 2 - PolyConecta corre sobre .NET 10 y SQL Server (Priority: P1) · L2 (0.3)

La solución compila y prueba con .NET 10, guarda en SQL Server con migraciones y tiene los casos de uso en su propio proyecto, sin EF InMemory, Npgsql ni MediatR.

**Why this priority**: sin esto no hay dónde construir la base común ni las fases.

**Independent Test**: `dotnet build` y `dotnet test` en verde con .NET 10; `dotnet ef database update` crea la base en un SQL Server 2022 vacío; la API arranca contra esa base.

**Acceptance Scenarios**:

1. **Given** un clon limpio con el SDK de `global.json`, **When** se corre `dotnet build Polyconecta.slnx`, **Then** compila sin advertencias de versión y todos los paquetes salen de `Directory.Packages.props`.
2. **Given** un SQL Server 2022 vacío, **When** se aplican las migraciones con el login de migraciones, **Then** se crean los esquemas, y la API corre con un login sin permisos de DDL (CT-30).
3. **Given** las 17 pruebas que existen hoy, **When** se migran a xUnit v3 y AwesomeAssertions, **Then** siguen pasando o se reemplazan por pruebas equivalentes contra SQL Server.

---

### User Story 3 - Bridge simulado (Priority: P1) · L1 (0.7)

El camino 2 envía cualquier comando del contrato a un bridge que corre en macOS y en CI, sin SDK, y recibe su resultado por callback, igual que con el bridge real.

**Why this priority**: es la base de trabajo del camino 2 en todas las fases (CT-21).

**Independent Test**: la suite de contrato pasa completa contra el simulador en macOS y en CI.

**Acceptance Scenarios**:

1. **Given** una carga válida de cualquiera de los 5 comandos, **When** se envía al simulador, **Then** acepta, asigna un folio simulado y llama al callback con el resultado que define el contrato.
2. **Given** una carga inválida (por ejemplo, `Σ lotes ≠ unidades`, CT-39), **When** se envía, **Then** responde el mismo error que el contrato define para el bridge real.
3. **Given** el mismo comando enviado dos veces, **When** se reenvía, **Then** devuelve el resultado del primero y no asigna otro folio.
4. **Given** una configuración de fallo, **When** se pide simular un error o una demora, **Then** el simulador lo produce, para poder probar reintentos y `Error` en el camino 2.

---

### User Story 4 - Base común de PolyConecta (Priority: P1) · L2 (0.6)

Toda entidad de negocio que se construya desde F1 hereda auditoría y archivado, cambia de estado solo por transiciones registradas, obtiene sus folios de un servicio central y encola sus escrituras a CONTPAQi en el outbox, en la misma transacción.

**Why this priority**: son las piezas que las fases reutilizan; construirlas después obligaría a rehacer documentos.

**Independent Test**: un documento de prueba, solo en el proyecto de pruebas, recorre: alta con folio → transición registrada → mensaje en el outbox → despachador → simulador → callback con folio e id ERP guardados en sus columnas `erp_*`.

**Acceptance Scenarios**:

1. **Given** una entidad que hereda los mixins, **When** se crea y se modifica, **Then** quedan quién y cuándo, y archivarla la oculta sin borrarla.
2. **Given** un documento con estados cerrados, **When** se intenta cambiar el estado sin su transición, **Then** se rechaza; **When** se ejecuta la transición, **Then** queda en `StateTransitionLog` con usuario, rol, fecha, origen, destino y nota (CT-32).
3. **Given** una falla al guardar el documento, **When** la transacción se revierte, **Then** tampoco queda el mensaje en el outbox (CT-20).
4. **Given** tres comandos en el outbox, **When** corre el despachador, **Then** los envía en orden de registro, uno a la vez (CT-41); si uno queda en `Error`, solo detiene los posteriores que comparten producto o almacén (D-95).
5. **Given** un comando que el simulador rechaza siempre, **When** se agotan los reintentos con espera creciente, **Then** el documento queda en sincronización `Error` y un caso de uso lo reintenta sin tocar la base (CT-15, CT-20).
6. **Given** dos tipos de documento, **When** se piden folios, **Then** cada uno sale de su secuencia configurada (prefijo, relleno, reinicio), sin choques en concurrencia.

---

### User Story 5 - Bridge en .NET 10 verificado (Priority: P2) · L1 (0.4)

El bridge y `tools/sdk-lab` corren en .NET 10 `win-x86` contra `MGWServicios.dll` en el laboratorio, con el mismo resultado que tuvieron en .NET 8.

**Why this priority**: .NET 8 pierde soporte el 10-nov-2026, y CT-04 exige la misma versión en todos los proyectos.

**Independent Test**: las pruebas F y G de la matriz, ejecutadas con `sdk-lab` en .NET 10 contra la empresa `_LAB`, dan el mismo resultado que el 30-sep.

**Acceptance Scenarios**:

1. **Given** el bridge compilado para .NET 10 `win-x86`, **When** arranca en el VPS, **Then** abre la sesión del SDK y responde `/health`.
2. **Given** `sdk-lab` en .NET 10, **When** se corren F y G, **Then** el resultado coincide con el registrado en la matriz; cualquier diferencia se anota en "Exploración y cambios".

---

### User Story 6 - Aplicación web: estructura, navegación y estilo (Priority: P2) · L2 (0.5)

Existe `PolyConecta.Web.Angular` con el estilo, el layout tipo Odoo, la navegación entre módulos y los componentes compartidos, listo para que cada fase agregue sus pantallas y la spec 001 sus páginas.

**Why this priority**: F1 construye pantallas desde el 12-oct.

**Independent Test**: `npm run build` y `npm test` en verde; la aplicación abre en el navegador con la barra de navegación y una página vacía por módulo.

**Acceptance Scenarios**:

1. **Given** el proyecto nuevo, **When** se compara con el prototipo, **Then** el layout, la tipografía, los colores y los componentes compartidos se ven igual (CT-24).
2. **Given** la navegación, **When** se elige un módulo, **Then** abre su ruta en `src/app/features/<modulo>/` (CT-09).

---

### User Story 7 - CI con pruebas y SQL Server (Priority: P2) · L2 (0.8)

Cada PR compila y corre las pruebas de .NET y Angular, las de aplicación contra SQL Server 2022 en contenedor y la suite de contrato contra el simulador.

**Why this priority**: CT-27 impide integrar a `main` sin pruebas en verde.

**Independent Test**: un PR con una prueba rota queda en rojo; el mismo PR corregido queda en verde.

**Acceptance Scenarios**:

1. **Given** un PR, **When** corre el pipeline, **Then** ejecuta build, pruebas de dominio, de aplicación con SQL Server, de contrato con el simulador y de Angular.
2. **Given** el pipeline, **When** se revisan sus archivos, **Then** no contiene secretos (CT-29).

---

### User Story 8 - Servidor del conector seguro y operable (Priority: P2) · L1 (0.1, 0.9)

El bridge del VPS usa credenciales nuevas y un login de solo lectura, y vuelve solo después de un reinicio del servidor.

**Why this priority**: es requisito del cierre integrado de cualquier fase (ROADMAP §5) y cierra la deuda de credenciales (05 §4.1).

**Independent Test**: se reinicia el VPS y, sin que nadie inicie sesión, el bridge responde `/health` y lee catálogos con el login de solo lectura.

**Acceptance Scenarios**:

1. **Given** la contraseña anterior de `sa`, **When** se intenta usar, **Then** falla; el bridge lee con su login de solo lectura definido en `BridgeConfig__SqlConnectionString` (A-2).
2. **Given** el usuario de prueba `polyconecta-bridge`, **When** termina F0, **Then** está revertido (A-19).
3. **Given** un reinicio del servidor, **When** arranca Windows, **Then** inicia sesión el administrador y la tarea "al iniciar sesión" levanta el bridge (D-115).

---

### Edge Cases

- El simulador y el bridge real responden distinto a la misma carga: es un defecto del contrato o de uno de los dos; se registra en "Exploración y cambios" y se corrige en el contrato con los dos líderes (CT-22).
- El callback no llega (API caída): el despachador no da el comando por confirmado; al volver, el estado se recupera consultando la transacción en el bridge (`GET /api/v1/transactions/{id}`).
- El despachador se reinicia a mitad de un envío: el reenvío usa la misma `idempotency_key` y no duplica.
- Dos usuarios piden folio del mismo tipo a la vez: no se repite el folio.
- El VPS se reinicia durante la ventana de mantenimiento con un comando en curso: el bridge lo retoma por su outbox SQLite.
- Una migración falla a la mitad en CI: la base del contenedor se descarta; nunca se corrige un esquema a mano (CT-06).

---

## Requirements *(mandatory)*

### Functional Requirements

**Común · contrato (0.2)**

- **FR-001**: `docs/contratos/bridge-v1.md` MUST describir el contrato y `docs/contratos/bridge-v1.openapi.yaml` MUST ser su OpenAPI válido. Parte de la API actual del bridge (`/api/v1/transactions`, `/catalogs/*`, `/inventory/stocks`, `/dlq`) y la ajusta a CT-17 y CT-18.
- **FR-002**: El contrato MUST definir para cada comando de CT-18 su carga (códigos de CONTPAQi resueltos por PolyConecta y cantidades en KG; el bridge convierte a la unidad de CONTPAQi y elige el concepto por configuración, D-121), su resultado (folio, id ERP, ids de movimientos), sus errores con código estable y su traducción al SDK, citando la decisión o la prueba de la matriz que la respalda (Principio VII).
- **FR-003**: El contrato MUST fijar `idempotency_key` (`{tipo}:{id}:{transición}`, CT-19), `correlation_id` (CT-31), la referencia de reconciliación de 20 caracteres como máximo (CT-38), la forma del callback y su firma con secreto compartido, la equivalencia de estados con CT-15 y la zona horaria del servidor de CONTPAQi como referencia de fechas (D-121).
- **FR-004**: El contrato MUST incluir la lectura de recepciones de compra (`/inventory/purchases`, D-102) aunque su implementación real llegue en una fase posterior.
- **FR-005**: El contrato MUST declarar sus reglas de versión (CT-22) y quedar aprobado por los dos líderes antes de que L1 y L2 lo implementen.
- **FR-006**: La **suite de contrato** (CT-23) MUST vivir en `tests/PolyConecta.Contract.Tests`, probar solo por HTTP con los ejemplos de `docs/contratos/ejemplos/`, no referenciar al bridge ni a PolyConecta, y correr igual contra el simulador y contra el bridge real, cambiando solo la URL por variable de entorno (D-122).

**L1 · Integración (0.1, 0.4, 0.7, 0.9)**

- **FR-007**: El bridge simulado MUST ser el mismo bridge con `SimulatedSdkGateway` y `SimulatedReadRepository` elegidos por `BridgeConfig__Mode` (D-122): el ciclo del outbox se separa de `ContpaqiSdkGateway`, las validaciones de CT-39 suben por encima del gateway y la DLL del SDK solo se carga en modo real. MUST implementar el contrato completo sin SDK, correr en macOS, Linux y CI, validar la carga con las mismas reglas que el real (CT-39), asignar folios simulados, responder por callback y ser idempotente (CT-21).
- **FR-008**: El simulador MUST permitir provocar errores, demoras y callbacks perdidos por configuración, sin cambiar código.
- **FR-009**: El bridge y `tools/sdk-lab` MUST compilar para .NET 10 `win-x86` (CT-04) y repetir F y G de la matriz en el laboratorio.
- **FR-010**: La contraseña de `sa` MUST estar rotada; el bridge MUST leer CONTPAQi con un login de solo lectura (CT-30) definido por variable de entorno (CT-29).
- **FR-011**: El servidor MUST iniciar sesión automáticamente con el administrador y levantar el bridge con una tarea "al iniciar sesión" (D-115), y el usuario de prueba `polyconecta-bridge` MUST quedar revertido.

**L2 · PolyConecta (0.3, 0.5, 0.6, 0.8)**

- **FR-012**: Todos los proyectos .NET de PolyConecta MUST usar .NET 10, con `global.json` y `Directory.Packages.props` (CT-36), xUnit v3 y AwesomeAssertions. MUST quedar fuera Npgsql, EF InMemory en producción y MediatR (D-72, D-73).
- **FR-013**: MUST existir `PolyConecta.Application` con los casos de uso; los controladores solo traducen HTTP ↔ caso de uso (CT-07, CT-08). La lógica transversal (transacción, auditoría, validación) va en decoradores propios.
- **FR-014**: La persistencia MUST ser SQL Server 2022 con EF Core 10, un esquema por módulo (CT-12) y migraciones versionadas (CT-06), aplicadas con un login distinto al de la API (CT-30).
- **FR-015**: MUST existir `AuditableEntity` y `ArchivableEntity`, y lo referenciado MUST archivarse en lugar de borrarse (04 §1).
- **FR-016**: Los documentos MUST cambiar de estado solo por transiciones nombradas con precondiciones, y cada transición MUST quedar en `StateTransitionLog` (CT-32).
- **FR-017**: `IReferenceSequenceService` MUST dar los folios por tipo de documento (prefijo, relleno, reinicio), sin duplicados en concurrencia, y sustituir al value object `Folio`.
- **FR-018**: El outbox MUST escribirse en la misma transacción que el cambio de negocio (CT-20) e incluir `idempotency_key` y `correlation_id`.
- **FR-019**: El despachador MUST enviar en orden de registro, uno a la vez (CT-41), reintentar con espera creciente, detener solo los comandos posteriores que compartan llave con uno en `Error` (D-95) y dejar el documento en `Error` al agotar los reintentos (CT-20).
- **FR-020**: La API MUST recibir el callback del bridge, verificar su firma (D-121), guardar folio e id ERP en las columnas `erp_*` del documento (CT-13) y actualizar su estado de sincronización (`No aplica`, `Pendiente`, `Enviado`, `Confirmado`, `Error`, CT-15). Mostrarlo en pantalla es de F2.
- **FR-020a**: El puerto `IBridgeSyncService` y el caso de uso de confirmación MUST vivir en `Application`; el despachador, el cliente HTTP y la traducción del documento a la carga, en `Infrastructure/Erp/`. El dominio no conoce el contrato (D-122).
- **FR-021**: Pasar del simulador al bridge real MUST ser solo configuración: URL y lista de comandos habilitados (CT-03).
- **FR-022**: MUST existir `PolyConecta.Web.Angular` (Angular 22, Node 24, versiones exactas, CT-36) con estilos, layout, navegación por módulo y componentes compartidos. Para no duplicar trabajo, la tarea 0.5 hace las fases 0 y 2A de la spec 001 (proyecto, dependencias, estilos, layout y los 13 componentes), y la spec 001 sigue desde ahí con sus páginas.
- **FR-023**: El pipeline de GitHub Actions MUST correr en cada PR: build, pruebas de dominio, de aplicación contra SQL Server 2022 en contenedor, de contrato contra el simulador y de Angular (CT-27, D-76).

### Key Entities

- **Contrato `bridge-v1`**: comandos, lecturas, callbacks, errores y versión. Lo comparten los dos caminos (CT-33).
- **`AuditableEntity`, `ArchivableEntity`**: mixins de toda entidad de negocio.
- **`StateTransitionLog`**: entidad, id, estado origen y destino, usuario, rol, fecha y nota.
- **`ReferenceSequence`**: tipo de documento, prefijo, relleno, siguiente número y regla de reinicio.
- **`OutboxMessage`**: comando, carga, `idempotency_key`, `correlation_id`, orden, intentos, estado y último error.
- **Estado de sincronización**: atributo de todo documento que escribe en CONTPAQi, independiente de su estado de negocio.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El contrato `bridge-v1` está aprobado por los dos líderes a más tardar el 6-oct (fecha de la tarea 0.2).
- **SC-002**: La suite de contrato pasa al 100 % contra el simulador en CI.
- **SC-003**: `dotnet build` y `dotnet test` pasan con .NET 10 y sin paquetes fuera de `Directory.Packages.props`.
- **SC-004**: El documento de prueba de US-4 recorre el ciclo completo hasta el callback en CI, y su fallo forzado termina en `Error` recuperable.
- **SC-005**: F y G de la matriz dan en .NET 10 el mismo resultado que en .NET 8.
- **SC-006**: Tras un reinicio del VPS, el bridge responde `/health` sin intervención en menos de 5 minutos.
- **SC-007**: Un PR con una prueba en rojo no se puede integrar a `main`.
- **SC-008**: Al cerrar F0, `docs/diseno/05-arquitectura-tecnica.md` refleja la nueva solución y la deuda 1, 2 y 5 de su §4 queda resuelta.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **L1**, una **L2** y una **Común** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Tareas | 0.1, 0.4, 0.7, 0.9 | 0.3, 0.5, 0.6, 0.8 | 0.2 |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web.Angular/`, `tests/PolyConecta.*`, `.github/` | `docs/contratos/`, suite de contrato, simulador (lo construye L1; sus cambios de comportamiento los aprueban los dos, CT-33), `global.json`, `Directory.Packages.props` |

**Orden.** 0.2 va primero. 0.3 y 0.1 empiezan el 5-oct en paralelo con el contrato. 0.7 y 0.6 empiezan cuando el contrato está aprobado. 0.8 necesita 0.3 y el simulador. 0.9 cierra la fase.

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `002-construccion-tecnica`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
2. **Qué se puede tocar.** Solo las carpetas de tu sección. Lo común se cambia con los dos líderes. `PolyConecta.Presentation/` es de solo lectura (D-60).
3. **El contrato manda.** Si el simulador, el bridge o el despachador necesitan algo que el contrato no dice, se anota en "Exploración y cambios" y se acuerda entre los dos líderes antes de implementarlo (CT-22).
4. **Ante una duda.** Primero esta spec, luego `docs/diseno/`, luego `docs/contpaq/`. Si nada la resuelve, se anota en "Exploración y cambios" con la pregunta concreta y lo que se hizo mientras tanto, y la revisa el líder.
5. **Dependencias.** Solo las versiones ratificadas (D-67 a D-73). Un paquete nuevo se anota en "Exploración y cambios" antes de instalarlo; uno con licencia comercial requiere decisión (CT-36).
6. **Hecho es verificado.** Una tarea está hecha cuando se ejecutaron y pasaron sus pruebas; las de L1 contra el laboratorio cuando aplica. Si no se puede verificar, queda abierta.
7. **No se debilita una prueba para que pase.**

---

## Assumptions

- El VPS del laboratorio está disponible esta semana para 0.1, 0.4 y 0.9, y la ventana de mantenimiento para reiniciarlo se coordina con Sistemas.
- .NET 10, EF Core 10 y Angular 22 en las versiones ratificadas funcionan con el código actual sin cambios de diseño; si no, se anota en "Exploración y cambios".
- El documento de prueba de US-4 vive solo en las pruebas; el primer documento real (pedido) llega en F1.
- La ejecución por pasos con reconciliación (CT-38) y la sesión de larga duración (CT-40) del bridge real no son parte de F0, pero el contrato ya las contempla en sus errores y respuestas.

---

## Exploración y cambios *(obligatoria, CT-43)*

Las secciones anteriores son el **objetivo primario** de la fase, fijado al ratificar la spec. Todo lo que surja después se registra aquí y se ejecuta en esta misma spec; un cambio menor no abre otra spec. Si un cambio modifica una decisión validada o el contrato del bridge, regístralo también en `docs/diseno/decisiones.md` y anota aquí su número.

| Fecha | Camino | Cambio | Motivo | Impacto (requisitos y tareas) | Decisión |
| :--- | :---: | :--- | :--- | :--- | :---: |
| 2026-10-05 | Común | La tarea 0.5 hace las fases 0 y 2A de la spec 001 (FR-022) | Evitar dos proyectos Angular | FR-022; tareas de 0.5 | Aprobado por el usuario |
| 2026-10-05 | Común | Reglas del contrato: PolyConecta manda códigos y KG; el bridge convierte la unidad y elige el concepto por configuración; estados ↔ CT-15; callback firmado; zona horaria del servidor de CONTPAQi | Preguntas previas a la sesión del contrato | FR-002, FR-003, FR-020 | D-121 |
| 2026-10-05 | Común | Capas: simulador como adaptadores del bridge; suite de contrato por HTTP en proyecto aparte; despachador en `Infrastructure/Erp/` | Que el simulador no se aparte del contrato | FR-006, FR-007, FR-020a | D-122 |
| 2026-10-05 | L1 | El bridge ya tiene un modo simulado embebido (`BridgeConfig:UseMockSdk`, o automático fuera de Windows) con `if` dentro de `ContpaqiSdkGateway`. Se convierte en el adaptador `SimulatedSdkGateway` y la opción pasa a `BridgeConfig__Mode` | Hallazgo al revisar el código | FR-007 | D-122 |
| 2026-10-05 | Común | Borrador del contrato en `docs/contratos/bridge-v1.md` con ejemplos en `docs/contratos/ejemplos/`, para la sesión de los líderes | Preparar la tarea 0.2 | FR-001 a FR-006 | — |
| 2026-10-05 | L1 | Queda abierto quién aplica la regla de costo de D-79 en `TRASPASO` | Sin definir | FR-002 (`TRASPASO`); se cierra antes de F3 | T-17 |
| 2026-10-05 | Común | **Propuesta por confirmar:** `CIERRE_PRODUCCION` y `REMISION` entran a `v1.0` con su forma definida pero como provisionales, y se afinan en F5 y F6 como cambios compatibles (CT-22) | Cumplir la aprobación del 6-oct (SC-001) | FR-002 | Pendiente |
