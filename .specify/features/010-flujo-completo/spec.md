# Feature Specification: Flujo completo (F8)

**Feature Branch**: `010-flujo-completo`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (5-nov).

**Fase del plan**: F8 · Flujo completo, 5 nov – 14 dic. Termina en la revisión **R8** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 8.1 a 8.4 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F8 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-87, D-94, D-97, D-98, D-100, D-101, D-104, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** El flujo de pedido a entrega corre completo contra el CONTPAQi de laboratorio y lo verifica una persona: documentos, existencias y lotes. Cada noche se concilian las existencias de PolyConecta con las de CONTPAQi y Sistemas revisa toda diferencia.
- **Diseño que la sostiene.** CT-42 (existencia oficial y conciliación), [06 §9](../../../docs/diseno/06-constitucion-tecnica.md) (cierre integrado), [ROADMAP §6](../../../docs/ROADMAP.md) (puesta en marcha).
- **Contrato.** Todos, contra el bridge real. Lectura de existencias para la conciliación. Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F1 a F7 cerradas en PolyConecta.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F8. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 8.1 | Conciliación nocturna de existencias con CONTPAQi | L1 | 5 – 9 nov | 16 |  |
| 8.2 | Secuencia del flujo completo, de pedido a entrega | Común | 7 – 8 dic | 20 |  |
| 8.3 | Prueba integrada contra el CONTPAQi de laboratorio y correcciones | Común | 8 – 11 dic | 56 |  |
| 8.4 | Preparar la revisión R8 | Común | 14 dic | 8 |  |

Horas: L1 16 · L2 0 · Común 84.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Paquete de instalación, manual y entrega (F9, sin spec). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Carga inicial de almacenes e inventarios, con WIP vacío (D-100) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

T-06 (la conciliación depende de F-05).

### Puesta en marcha

Depurar en CONTPAQi F-03 y F-06; carga inicial (D-100); ventana nocturna para la conciliación (D-98). No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Conciliación nocturna de existencias (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Secuencia del flujo completo, de pedido a entrega (Priority: P?) · Común

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Prueba integrada contra el laboratorio y correcciones (Priority: P?) · Común

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R8 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `010-flujo-completo`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
