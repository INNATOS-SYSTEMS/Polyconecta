# Feature Specification: Traslados entre plantas (F7)

**Feature Branch**: `009-traslados`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (26-nov).

**Fase del plan**: F7 · Traslados entre plantas, 26 nov – 7 dic. Termina en la revisión **R7** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 7.1 a 7.5 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F7 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-43, D-56, D-79, D-82, D-117, D-121, D-123, D-124, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** El material sale de una planta a tránsito y la otra lo recibe lote por lote, con recepción parcial; cada paso es un traspaso en CONTPAQi. Se consulta el inventario de materia prima y en proceso por planta, y ningún traslado ni entrega se valida con lotes sin liberar.
- **Diseño que la sostiene.** [02 §5](../../../docs/diseno/02-flujo-y-reglas.md) (traslado interplanta en dos pasos), [02 §3](../../../docs/diseno/02-flujo-y-reglas.md) (hard-stop), [03](../../../docs/diseno/03-almacenes-y-operaciones.md) (tránsito), CT-42.
- **Contrato.** `TRASPASO` (variantes `TRASLADO_SALIDA` y `TRASLADO_RECEPCION`). Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F3: traspasos y almacenes. F5: liberación de lotes.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F7. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

**Responsables (D-117).** Toda la fase la construye Alejandro Ponce, en PolyConecta y en el bridge; coordina con Luis Alvarado Martinez los cambios a lo común.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 7.1 | Traslado de origen a 'EN TRÁNSITO' | L1 | 26 nov – 1 dic | 24 | En PolyConecta y bridge |
| 7.2 | Recepción en destino y existencia en tránsito | L1 | 1 – 3 dic | 11 | En PolyConecta y bridge |
| 7.3 | Inventario de materia prima y en proceso por planta | L1 | 2 – 4 dic | 11 | En PolyConecta |
| 7.4 | Bloqueo de traslados y entregas con lotes sin liberar | L1 | 3 – 4 dic | 6 | En PolyConecta |
| 7.5 | Preparar la revisión R7 | L1 | 7 dic | 4 |  |

Horas: L1 56 · L2 0 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Conciliación nocturna (F8). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Ninguno | |

### Preguntas abiertas y decisiones por validar

Ninguna.

### Puesta en marcha

Ninguno propio. No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Traslado de origen a tránsito (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Recepción en destino, parcial y libre (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Inventario de MP y WIP por planta (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Hard-stop de traslados y entregas con lotes sin liberar (Priority: P?) · L1

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R7 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `009-traslados`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
