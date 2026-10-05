# Feature Specification: Entregas (F6)

**Feature Branch**: `008-entregas`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (4-nov).

**Fase del plan**: F6 · Entregas, 4 – 30 nov. Termina en la revisión **R6** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 6.1 a 6.7 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F6 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-85, D-111, D-113, D-114, D-117, D-121, D-123, D-124, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** Tráfico entrega al cliente los lotes liberados de un pedido, en parcialidades con pendiente por surtir; la entrega genera la remisión en CONTPAQi y, al quedar surtido todo el pedido, el bridge lo cancela allá. En esta fase el conector queda listo: pasa la suite completa contra el laboratorio.
- **Diseño que la sostiene.** [02 §5](../../../docs/diseno/02-flujo-y-reglas.md) (entrega a cliente, estados y smart buttons; el riesgo de la remisión ya lo resolvieron D-113 y D-114), [02 §2](../../../docs/diseno/02-flujo-y-reglas.md) (`[007-FR-014]` backorder), [02 §3](../../../docs/diseno/02-flujo-y-reglas.md) (hard-stop), CT-23.
- **Contrato.** `REMISION` (6.1). Suite de contrato completa contra el bridge real (6.2). Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F5: lotes liberados. F1 y F2: pedido dado de alta en CONTPAQi.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F6. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

**Responsables (D-117).** Alejandro Ponce construye 6.3 a 6.5 en PolyConecta, en las carpetas del camino 2, y coordina con Luis Alvarado Martinez los cambios a lo común. **6.6 (ajustes de R1 a R5)** no son requisitos de esta spec: cada ajuste se registra y se ejecuta en la exploración de la spec de la fase que toca (CT-43); aquí solo se lleva su seguimiento.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 6.1 | Remisión en CONTPAQi y cancelación del pedido surtido | L1 | 4 – 5 nov | 8 |  |
| 6.2 | Prueba completa del conector contra el laboratorio: conector listo | L1 | 9 – 13 nov | 36 |  |
| 6.3 | Orden de entrega ligada al pedido, con lotes liberados | L1 | 17 – 19 nov | 20 | En PolyConecta (D-117) |
| 6.4 | Entrega parcial y pendiente por surtir | L1 | 19 – 20 nov | 12 | En PolyConecta (D-117) |
| 6.5 | Remisión desde la entrega y cierre del pedido surtido | L1 | 23 – 25 nov | 24 | En PolyConecta (D-117) |
| 6.6 | Ajustes pedidos en R1 a R5 | L2 | 24 – 27 nov | 32 | Se ejecutan en las specs 003 a 007 (CT-43) |
| 6.7 | Preparar la revisión R6 | L1 | 30 nov | 4 |  |

Horas: L1 104 · L2 32 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Traslados interplanta (F7). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Ninguno | |

### Preguntas abiertas y decisiones por validar

D-114 (cancelar en CONTPAQi el pedido remisionado) por validar con la operación.

### Puesta en marcha

Ninguno propio. No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - `REMISION` y cancelación del pedido surtido (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Conector listo: suite completa contra el laboratorio (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Orden de entrega ligada al pedido, con lotes liberados (en PolyConecta) (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Entrega parcial y pendiente por surtir (en PolyConecta) (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 5 - Remisión desde la entrega y cierre del pedido surtido (en PolyConecta) (Priority: P?) · L1

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R6 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web.Angular/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `008-entregas`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
