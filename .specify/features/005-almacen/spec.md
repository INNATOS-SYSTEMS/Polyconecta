# Feature Specification: Almacén (F3)

**Feature Branch**: `005-almacen`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (26-oct).

**Fase del plan**: F3 · Almacén, 26 oct – 9 nov. Termina en la revisión **R3** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 3.1 a 3.7 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F3 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-24, D-43, D-55, D-79, D-81, D-82, D-83, D-85, D-89, D-94, D-99, D-103, D-106, D-110, D-121, D-123, D-124, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** Producción pide materia prima con la recolección ligada a la orden y Almacén la surte a WIP, en parcialidades si hace falta; el sobrante regresa a stock. Cada movimiento se escribe en CONTPAQi como un traspaso (par Salida + Entrada) con varios lotes, y los almacenes de proceso se dan de alta por SDK.
- **Diseño que la sostiene.** [02 §4](../../../docs/diseno/02-flujo-y-reglas.md) (recolección a WIP), [02 §2](../../../docs/diseno/02-flujo-y-reglas.md) (dos niveles de reserva), [03 §1 a §6](../../../docs/diseno/03-almacenes-y-operaciones.md) (ubicaciones, operaciones, traspaso en CONTPAQi), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Inventario), CT-39, CT-42.
- **Contrato.** `TRASPASO` (variantes `RECOLECCION` y `DEVOLUCION`) y `ALTA_ALMACEN`. Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F2: orden de fabricación con componentes. F1: almacenes sincronizados.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F3. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 3.1 | Traspasos entre almacenes con varios lotes y costo | L1 | 26 – 29 oct | 15 |  |
| 3.2 | Traspasos entre materia prima y proceso, y alta de almacenes de proceso | L1 | 29 – 30 oct | 7 |  |
| 3.3 | Órdenes de recolección ligadas a la fabricación | L2 | 29 oct – 4 nov | 19 |  |
| 3.4 | Entregas parciales de recolecciones validadas | L2 | 3 – 5 nov | 9 |  |
| 3.5 | Saldo en proceso por orden, recolección libre y asignación de saldo | L2 | 4 – 6 nov | 8 |  |
| 3.6 | Devoluciones a stock | L2 | 5 – 6 nov | 5 |  |
| 3.7 | Preparar la revisión R3 | L2 | 9 nov | 3 |  |

Horas: L1 22 · L2 44 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | Consumo desde WIP y cierre (F5), traslados interplanta (F7). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Reservas lote por lote con control de concurrencia (ROADMAP §9) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Re-lotificación (03) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Recepciones de compra sincronizadas como `IN-COMPRA` (D-102) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Aviso de cobertura nocturna (D-103) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

T-17 (costo de la Entrada en `TRASPASO`): se cierra en el contrato antes de empezar 3.1.

### Puesta en marcha

Conceptos propios creados en CONTPAQi por la UI (D-89, D-110); compras de MP en el almacén de MP de cada planta y traspaso de la MP que está en "Gastos" (D-107). No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Recolección ligada a la orden, liberada al confirmarla (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Surtido en parcialidades con backorder (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Saldo en WIP por orden, recolección libre y asignación de saldo (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Devolución re-pesada a stock (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 5 - `TRASPASO` como par Salida + Entrada con N lotes y costo (Priority: P?) · L1

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 6 - `ALTA_ALMACEN` y traspasos MP ↔ WIP (Priority: P?) · L1

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R3 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `005-almacen`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
