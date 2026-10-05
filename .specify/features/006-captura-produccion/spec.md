# Feature Specification: Captura de producción (F4)

**Feature Branch**: `006-captura-produccion`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (6-nov).

**Fase del plan**: F4 · Captura de producción, 6 – 13 nov. Termina en la revisión **R4** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 4.1 a 4.5 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F4 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-10, D-39, D-40, D-45, D-54, D-98, D-124, D-127, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** El Planner vacía en PolyConecta lo que los operadores anotaron en los diarios de piso: lotes producidos con su cantidad en la unidad base de CONTPAQi (D-127; el peso en kg depende de P-25), registro masivo e incidencias de máquina. Los lotes se proyectan al confirmar la orden con su nomenclatura.
- **Diseño que la sostiene.** [02 §3](../../../docs/diseno/02-flujo-y-reglas.md) (pesaje y lotes, conversión en Santa Cruz, incidencias), [02 §6](../../../docs/diseno/02-flujo-y-reglas.md) (unidades), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Manufactura).
- **Contrato.** Ninguno. La entrada de producción a CONTPAQi ocurre en el cierre (F5). Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F2: orden de fabricación con planeación. F3: material en WIP.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F4. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 4.1 | Lotes proyectados al confirmar la orden y lotificación planeada | L2 | 6 – 9 nov | 5 |  |
| 4.2 | Captura de lotes producidos | L2 | 9 – 11 nov | 13 |  |
| 4.3 | Registro de producción masiva | L2 | 11 – 12 nov | 5 |  |
| 4.4 | Incidencias de máquina | L2 | 11 – 13 nov | 8 |  |
| 4.5 | Preparar la revisión R4 | L2 | 13 nov | 3 |  |

Horas: L1 0 · L2 34 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Calidad y cierre técnico (F5). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Registro dual en bolseo: millares y kg. D-127 quitó el kg capturado aparte en general; cómo queda el bolseo depende de P-25, dentro de 4.2 | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

Ninguna.

### Puesta en marcha

Ninguno propio. No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Lotes proyectados al confirmar la orden (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Captura de lotes producidos con sus dos cantidades (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Registro de producción masiva (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Incidencias de máquina (Priority: P?) · L2

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R4 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `006-captura-produccion`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
