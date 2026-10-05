# Feature Specification: Planeación de producción (F2)

**Feature Branch**: `004-planeacion-produccion`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (15-oct).

**Fase del plan**: F2 · Planeación de producción, 15 oct – 2 nov. Termina en la revisión **R2** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 2.1 a 2.8 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F2 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-05 a D-08, D-41, D-42, D-52, D-80, D-81, D-92, D-93, D-95, D-112, D-113, D-124, D-127, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** Del pedido autorizado nace la cadena de órdenes de fabricación con sus componentes, subproductos y planeación por máquina; el pedido se da de alta en CONTPAQi y su estado de sincronización se ve en el formulario y en un tablero para Sistemas. El bridge real ejecuta cada envío por pasos, con reintentos, reconciliación y verificación.
- **Diseño que la sostiene.** [02 §2](../../../docs/diseno/02-flujo-y-reglas.md) (motor de abastecimiento), [02 §3](../../../docs/diseno/02-flujo-y-reglas.md) (jerarquía, reglas universales, componentes, subproductos y planeación), [03 §7](../../../docs/diseno/03-almacenes-y-operaciones.md) (centros de trabajo), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Abastecimiento, Manufactura), CT-15, CT-38, CT-39, CT-41.
- **Contrato.** `ALTA_PEDIDO` (2.4). Ejecución por pasos y reconciliación del bridge real para todos los comandos (2.2). Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F1: pedido autorizado, catálogos sincronizados y roles.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F2. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 2.1 | Orden de fabricación ligada al pedido y cadena de órdenes | L2 | 20 – 22 oct | 16 |  |
| 2.2 | Registro por pasos, reintentos y verificación de cada envío | L1 | 15 – 23 oct | 40 |  |
| 2.3 | Componentes con existencia y subproductos | L2 | 21 – 23 oct | 12 |  |
| 2.4 | Alta de pedidos en CONTPAQi con folio, precio y moneda | L1 | 21 – 27 oct | 20 |  |
| 2.5 | Envío del pedido a CONTPAQi con estado de sincronización visible | L2 | 26 – 27 oct | 8 |  |
| 2.6 | Centros de trabajo y planeación por máquina | L2 | 23 – 28 oct | 16 |  |
| 2.7 | Tablero de sincronización: errores y reintentos | L2 | 28 – 30 oct | 12 |  |
| 2.8 | Preparar la revisión R2 | L2 | 2 nov | 4 |  |

Horas: L1 60 · L2 68 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Recolección a WIP (F3), captura de producción (F4), cierre técnico (F5). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Motor de abastecimiento: rutas MTSO/MTO, simulación y reserva lógica (02 §2) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Visor de disponibilidad (02 §2, `[007-FR-001]`) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

Ninguna propia. Contingencia: si `ALTA_PEDIDO` falla (T-12), el pedido libre queda interno hasta resolverlo.

### Puesta en marcha

Catálogo de centros de trabajo: código, proceso, planta y capacidad (ex P-14). No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Cadena de órdenes de fabricación ligada al pedido, y orden libre (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Componentes con existencia y subproductos (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Centros de trabajo y planeación por máquina (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Pedido enviado a CONTPAQi con su estado de sincronización y tablero de errores (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 5 - `ALTA_PEDIDO` con folio, precio y moneda (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 6 - Ejecución por pasos con reintentos, reconciliación y verificación (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### Edge Cases

_Por redactar._

---

## Requirements *(mandatory)*

### Functional Requirements

**Común**

_Por redactar._

**L1 · Integración**

_Por redactar._

**L2 · PolyConecta**

_Por redactar._

### Key Entities

_Por redactar, a partir de [04-modelo-de-dominio.md](../../../docs/diseno/04-modelo-de-dominio.md)._

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: _Por redactar._
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R2 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `004-planeacion-produccion`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
2. **Qué se puede tocar.** Solo las carpetas de tu sección (CT-33). Lo común se cambia con los dos líderes. `PolyConecta.Presentation/` es de solo lectura (D-60).
3. **El contrato manda.** Si algo no está en `docs/contratos/bridge-v1.md`, se anota en "Exploración y cambios" y se acuerda entre los dos líderes antes de implementarlo (CT-22).
4. **Ante una duda.** Primero esta spec, luego `docs/diseno/`, luego `docs/contpaq/`. Si nada la resuelve, se anota en "Exploración y cambios" con la pregunta concreta y lo que se hizo mientras tanto.
5. **Dependencias.** Solo las versiones ratificadas (CT-36). Un paquete nuevo se anota en "Exploración y cambios" antes de instalarlo.
6. **Hecho es verificado.** Una tarea está hecha cuando se ejecutaron y pasaron sus pruebas, y se verificó en navegador o contra el laboratorio cuando aplica.
7. **No se debilita una prueba para que pase.**

---

## Assumptions

_Por redactar._

---

## Exploración y cambios *(obligatoria, CT-43)*

Las secciones anteriores son el **objetivo primario** de la fase, fijado al ratificar la spec. Todo lo que surja después se registra aquí y se ejecuta en esta misma spec; un cambio menor no abre otra spec. Si un cambio modifica una decisión validada o el contrato del bridge, regístralo también en `docs/diseno/decisiones.md` y anota aquí su número.

| Fecha | Camino | Cambio | Motivo | Impacto (requisitos y tareas) | Decisión |
| :--- | :---: | :--- | :--- | :--- | :---: |
| | | | | | |
