# Feature Specification: Calidad y cierre (F5)

**Feature Branch**: `007-calidad-y-cierre`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (28-oct).

**Fase del plan**: F5 · Calidad y cierre, 28 oct – 23 nov. Termina en la revisión **R5** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 5.1 a 5.7 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F5 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-09, D-22, D-46, D-84, D-96, D-111, D-121, D-123, D-124, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** Calidad aprueba o rechaza cada lote de la orden; un rechazo manda el lote a cuarentena `.S` y bloquea el cierre. El cierre técnico registra el scrap por motivo, cuadra lo recolectado contra lo consumido, devuelto y desechado, y escribe en CONTPAQi el consumo desde WIP y la entrada del producto terminado con su lote y costo.
- **Diseño que la sostiene.** [02 §3](../../../docs/diseno/02-flujo-y-reglas.md) (calidad, cierre técnico y balance de masa), [02 §4](../../../docs/diseno/02-flujo-y-reglas.md) (`[008-FR-010/012]`), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Manufactura, Inventario), CT-39.
- **Contrato.** `CIERRE_PRODUCCION` (5.1) y `TRASPASO` (variantes `CUARENTENA` y `LIBERACION`). Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F4: lotes producidos. F3: saldo en WIP.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F5. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 5.1 | Cierre de producción en CONTPAQi: consumo, producto terminado con lote y costo, y subproductos | L1 | 28 oct – 4 nov | 30 |  |
| 5.2 | Control de calidad por orden: aprobar o rechazar cada lote | L2 | 12 – 17 nov | 8 |  |
| 5.3 | Cuarentena y bloqueo del cierre con lotes en revisión | L2 | 17 – 18 nov | 7 |  |
| 5.4 | Cierre técnico con descuento de cantidad, alta de scrap por motivo | L2 | 17 – 19 nov | 11 |  |
| 5.5 | Cuadre de stock surtido contra consumido en el proceso de fabricación | L2 | 19 – 20 nov | 8 |  |
| 5.6 | Envío del cierre / ajustes de inventario a CONTPAQi | L2 | 20 nov | 4 |  |
| 5.7 | Preparar la revisión R5 | L2 | 23 nov | 8 |  |

Horas: L1 30 · L2 46 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Hard-stop en traslados y entregas (se aplica en F6 y F7, tarea 7.4). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Re-liberación de lotes en cuarentena (02 §3) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

T-17 (quién calcula el costo de la entrada de PT).

### Puesta en marcha

Tolerancia del balance de masa (ex P-16); conceptos de producción creados en CONTPAQi. No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Control de calidad por orden, lote por lote (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Cuarentena `.S` y bloqueo del cierre (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Cierre técnico con scrap por motivo (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Balance de masa: recolectado = consumido + devuelto + scrap (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 5 - Envío del cierre y ajustes a CONTPAQi (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 6 - `CIERRE_PRODUCCION`: consumo desde WIP y entrada de PT y subproductos (Priority: P?) · L1

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R5 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `007-calidad-y-cierre`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
