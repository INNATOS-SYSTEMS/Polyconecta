# Feature Specification: Pedidos de venta (F1)

**Feature Branch**: `003-pedidos-de-venta`

**Created**: 2026-10-05

**Status**: Esqueleto. Se completa y se ratifica con los dos líderes antes de que empiece la fase (12-oct).

**Fase del plan**: F1 · Pedidos de venta, 12 – 19 oct. Termina en la revisión **R1** con la operación.

**Líderes**: L1 · Alejandro Ponce (camino 1, integración CONTPAQi) · L2 · Luis Alvarado Martinez (camino 2, PolyConecta).

**Input**: Tareas 1.1 a 1.7 del [plan de trabajo](../../../docs/plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) y la fila F1 de [ROADMAP.md §3](../../../docs/ROADMAP.md).

**Decisiones que la rigen** (`docs/diseno/decisiones.md`): D-15, D-32 a D-38 (roles, firmas y suplentes), D-52, D-53, D-74, D-78, D-86, D-87, D-88, D-91, D-108, D-113, D-123, D-124, D-116 a D-120 (fases y specs).

---

## Contexto para el agente

- **Objetivo de la fase.** Atención a Clientes captura un pedido (libre o sincronizado desde CONTPAQi), lo confirma y lo autorizan Comercial y Cobranza con dos firmas de personas distintas. Para eso la fase entrega usuarios, roles y permisos por planta, la sincronización de productos, clientes y almacenes, la sesión permanente del bridge con sus lecturas, y la búsqueda y el chatter que usarán todas las pantallas.
- **Diseño que la sostiene.** [02 §1](../../../docs/diseno/02-flujo-y-reglas.md) (pedido, estados y dos firmas), [02 §6](../../../docs/diseno/02-flujo-y-reglas.md) (unidades) y [02 §7](../../../docs/diseno/02-flujo-y-reglas.md) (interfaz), [01 §3](../../../docs/diseno/01-modulos-y-roles.md) (roles), [04 §3](../../../docs/diseno/04-modelo-de-dominio.md) (Organización, Catálogo de productos, Comercial, Seguridad y auditoría, Configuración de interfaz), CT-13, CT-14, CT-40.
- **Contrato.** Lecturas `GET /catalogs/products`, `/catalogs/clients`, `/catalogs/warehouses` e `/inventory/stocks`. Ningún comando de escritura: `ALTA_PEDIDO` es de F2. Ver [`docs/contratos/bridge-v1.md`](../../../docs/contratos/bridge-v1.md).
- **Depende de.** F0: contrato `bridge-v1`, base común, simulador, aplicación web y CI.
- **Objetivo primario y exploración (CT-43).** Esta spec es el objetivo primario de F1. Lo que se descubra al construirla se registra al final, en "Exploración y cambios", y se hace aquí mismo. No se abre otra spec por un ajuste.

---

## Tareas del plan

| # | Actividad | Sección | Fechas | Horas | Nota |
| :--- | :--- | :---: | :--- | ---: | :--- |
| 1.1 | Sesión permanente con CONTPAQi: doble inicio de sesión y tiempos límite | L1 | 12 – 13 oct | 12 |  |
| 1.2 | Usuarios, roles y permisos por planta | L2 | 12 – 14 oct | 9 |  |
| 1.3 | Sincronizar productos, clientes y almacenes de CONTPAQi | L2 | 12 – 14 oct | 9 |  |
| 1.4 | Lectura de catálogos y existencias de CONTPAQi | L1 | 13 – 15 oct | 16 |  |
| 1.5 | Pedido de venta: captura, confirmación y autorización con dos firmas | L2 | 13 – 16 oct | 13 |  |
| 1.6 | Búsqueda, filtros y conversación por documento en listas y formularios | L2 | 15 – 19 oct | 12 |  |
| 1.7 | Preparar la revisión R1 | L2 | 19 oct | 4 |  |

Horas: L1 28 · L2 47 · Común 0.

## Alcance

| Dentro | Fuera |
| :--- | :--- |
| Las tareas del plan de esta fase | El motor de abastecimiento (al autorizar solo cambia el estado; el motor se conecta en F2), `ALTA_PEDIDO` y el estado de sincronización visible (F2). |

### Diseño sin tarea en el plan

| Elemento | Qué hacer |
| :--- | :--- |
| Pedido capturado en CONTPAQi que entra por sincronización (D-53) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Revocación de la autorización (02 §1) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Ficha técnica, unidades que admite el producto (D-124) y clasificación propia (D-86) | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |
| Cotejo de F-01, F-02 y F-05 con la UI de CONTPAQi (T-06, ex A-13), junto con 1.4 | Confirmar al ratificar si cabe en las horas de la fase o se registra fuera de alcance |

### Preguntas abiertas y decisiones por validar

T-06 (frescura de la lectura de existencias, tarea 1.4): se especifica con la lectura directa de D-87 como supuesto.

### Puesta en marcha

Titulares y suplentes de cada rol, con su planta (ex P-02). No bloquea la construcción (D-75).

---

## User Scenarios & Testing *(mandatory)*

Historias propuestas a partir de las tareas del plan. Se priorizan y redactan al completar la spec.

### User Story 1 - Usuarios, roles y permisos por planta, con suplentes (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 2 - Catálogos de CONTPAQi sincronizados en PolyConecta (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 3 - Pedido libre y sincronizado: captura, confirmación y autorización con dos firmas (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 4 - Búsqueda, filtros, agrupaciones y chatter en listas y formularios (Priority: P?) · L2

_Por redactar: descripción, por qué esta prioridad, prueba independiente y escenarios Given/When/Then._

### User Story 5 - Sesión permanente del bridge y lecturas de catálogos y existencias (Priority: P?) · L1

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
- **SC-00N**: La fase cumple sus dos cierres ([06 §9](../../../docs/diseno/06-constitucion-tecnica.md)) y pasa la revisión R1 con la operación.

---

## Ejecución por líderes y agentes

`plan.md` y `tasks.md` tienen una sección **Común**, una **L1** y una **L2** (CT-34, D-120). Cada líder y sus agentes son dueños de sus carpetas (CT-33):

| | L1 · Alejandro Ponce | L2 · Luis Alvarado Martinez | Común |
| :--- | :--- | :--- | :--- |
| Carpetas | `PolyConecta.Contpaq/`, `tools/sdk-lab/`, `tests/Contpaq.Bridge.Tests/`, `docs/contpaq/` | `PolyConecta.Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Web.Angular/`, `tests/PolyConecta.*` | `docs/contratos/`, `tests/PolyConecta.Contract.Tests/`, el simulador, `global.json`, `Directory.Packages.props` |

### Reglas de autonomía

1. **Rama y commits.** El trabajo va en ramas que salen de `003-pedidos-de-venta`, nunca en `main`. Un commit por tarea de `tasks.md`, con su id (`L1-T003`).
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
