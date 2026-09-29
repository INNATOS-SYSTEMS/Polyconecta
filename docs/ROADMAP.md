# Roadmap de construcción

La construcción avanza **módulo a módulo** por **dos caminos en paralelo**, cada uno con su líder y sus agentes. Las reglas de cómo se construye están en la [constitución técnica](diseno/06-constitucion-tecnica.md); aquí están el orden, qué entrega cada camino por módulo y el tablero de avance.

**Actualizado:** 29 de septiembre de 2026.

---

## 1. Los dos caminos

| | Camino 1 · Integración CONTPAQi | Camino 2 · PolyConecta independiente |
| :--- | :--- | :--- |
| **Objetivo** | Que cada comando del contrato del bridge funcione contra CONTPAQi | Que cada módulo funcione completo con sus propias tablas, contra el bridge simulado |
| **Construye** | Bridge real, `tools/sdk-lab`, matriz de pruebas del SDK | Dominio, casos de uso, SQL Server, API, Angular |
| **Se prueba contra** | CONTPAQi de laboratorio (empresa `_LAB`) | Bridge en modo simulado |
| **Líder** | por asignar | por asignar |

Se encuentran en el **contrato del bridge** (`docs/contratos/bridge-v1.md`, CT-17 a CT-23). El camino 2 lo consume y el camino 1 lo cumple. Un módulo se integra cuando los dos lados pasan la misma suite de contrato.

## 2. Módulos

El orden sigue la dependencia de datos: cada módulo usa lo que cerró el anterior. La conversión en Santa Cruz (impresión y bolseo) **no es un módulo aparte**: es Producción con las mismas 4 reglas universales (D-64).

| # | Módulo | Qué incluye | Diseño |
| :---: | :--- | :--- | :--- |
| 0 | **Plataforma** | Base SQL Server y migraciones, mixins de auditoría y archivado, `StateTransitionLog`, numeración centralizada, identidad, roles, permisos y suplentes, outbox y despachador, mapeo ERP, estado de sincronización, vistas de búsqueda, chatter | [01 §3](diseno/01-modulos-y-roles.md), [04 §1](diseno/04-modelo-de-dominio.md), [06](diseno/06-constitucion-tecnica.md) |
| 1 | **Catálogos e Inventario** | Productos (sincronizados), ficha técnica, unidades de venta, clasificación, plantas, ubicaciones y su alta en CONTPAQi, lotes, existencias (las cinco cifras), reservas, re-lotificación | [03](diseno/03-almacenes-y-operaciones.md), [04 §3](diseno/04-modelo-de-dominio.md) |
| 2 | **Ventas** | Pedido sincronizado y libre, confirmación, autorización de dos firmas, revocación, motor de abastecimiento (rutas MTSO/MTO, simulación, reserva), visor de disponibilidad | [02 §1–2](diseno/02-flujo-y-reglas.md) |
| 3 | **Producción** | Orden de fabricación autorreferenciada para extrusión, impresión y bolseo; componentes, subproductos, planeación, centros de trabajo, recolección a WIP y devolución, captura de producción, registro dual millares/kg, incidencias, cierre técnico y balance de masa | [02 §3–4](diseno/02-flujo-y-reglas.md) |
| 4 | **Calidad** | Control de calidad como documento, aprobación y rechazo por lote, cuarentena `.S`, hard-stop, re-liberación | [02 §3](diseno/02-flujo-y-reglas.md) |
| 5 | **Logística** | Traslado y recepción interplanta en dos pasos, entrega a cliente, backorder | [02 §5](diseno/02-flujo-y-reglas.md) |

**Transversal a todos:** la presentación en Angular. La spec 001 replica el prototipo con estado en el navegador; cada módulo conecta después sus pantallas a la API al cerrarse en PolyConecta.

## 3. Qué entrega cada camino por módulo

| Módulo | Camino 2 · PolyConecta | Camino 1 · Integración | Pruebas del SDK que lo sostienen |
| :--- | :--- | :--- | :--- |
| 0 · Plataforma | Todo lo de la tabla anterior, más el despachador del outbox hacia el contrato | Contrato `v1` publicado; **bridge en modo simulado**; idempotencia, callbacks y DLQ recuperable; corrección de G-01; lecturas de catálogos | G-01..G-05, F-01..F-03 |
| 1 · Catálogos e Inventario | Tablas `inv`, sincronización de productos, ubicaciones con su id ERP, existencias y reservas | `ALTA_ALMACEN` (o alta manual si T-10 falla); `TRASPASO` con lote; lectura de existencias | A, B, C-02, C-03, F-04, F-05, T-10, T-11 |
| 2 · Ventas | Tablas `ven`, pedido en sus dos modos, firmas, motor de abastecimiento | Sincronización de pedidos; `ALTA_PEDIDO` con folio devuelto e idempotencia (T-12) | F, T-12 |
| 3 · Producción | Tablas `prd`, orden de fabricación, recolección y devolución, captura, cierre y balance | `CIERRE_PRODUCCION` (consumo desde WIP + entrada de PT y scrap); `TRASPASO` de recolección y devolución | B, C, D, E, T-11 |
| 4 · Calidad | Tablas `cal`, documento de calidad, hard-stop aplicado en Inventario y Logística | `TRASPASO` hacia y desde cuarentena | B |
| 5 · Logística | Tablas `log`, traslado, recepción y entrega | `TRASPASO` a tránsito y a destino; `REMISION` ligada al pedido | B, T-07 |

El camino 1 puede adelantarse al 2: por ejemplo, probar `TRASPASO` mientras el camino 2 construye Plataforma. Nunca se atrasa la integración de un módulo por el orden de construcción de otro.

## 4. Tablero de avance

Estados de cada columna: ⬜ pendiente · 🟨 en curso · ✅ cerrado · ⛔ bloqueado. Al cerrar, anota la fecha y el commit (CT-35).

| # | Módulo | Diseño cerrado | Spec | Cerrado en PolyConecta | Comandos entregados (camino 1) | Cerrado integrado |
| :---: | :--- | :---: | :--- | :---: | :---: | :---: |
| — | Presentación Angular (réplica) | ✅ | `001-angular-presentation` | ⬜ | n/a | n/a |
| 0 | Plataforma | 🟨 | por crear | ⬜ | ⬜ | ⬜ |
| 1 | Catálogos e Inventario | 🟨 | por crear | ⬜ | ⬜ | ⬜ |
| 2 | Ventas | 🟨 | por crear | ⬜ | ⬜ | ⬜ |
| 3 | Producción | 🟨 | por crear | ⬜ | ⬜ | ⬜ |
| 4 | Calidad | 🟨 | por crear | ⬜ | ⬜ | ⬜ |
| 5 | Logística | 🟨 | por crear | ⬜ | ⬜ | ⬜ |

**Diseño cerrado** significa que el módulo no tiene preguntas abiertas que bloqueen su spec. Hoy ninguno lo cumple del todo; ver la sección 5.

## 5. Lo que bloquea cada módulo

| Módulo | Preguntas abiertas ([preguntas-abiertas.md](diseno/preguntas-abiertas.md)) |
| :--- | :--- |
| 0 · Plataforma | P-20 (versión de .NET); P-02 (titulares), solo para configurar usuarios |
| 1 · Catálogos e Inventario | T-01, T-02, T-05, T-06, T-10, T-11 |
| 2 · Ventas | P-19 (precio del pedido libre), T-12 |
| 3 · Producción | P-14 (centros de trabajo), P-16 (tolerancia), T-01, T-03, T-04 |
| 4 · Calidad | ninguna propia |
| 5 · Logística | T-07 (remisión ligada al pedido) |

## 6. Trabajo de arranque

| # | Trabajo | Camino | Estado |
| :---: | :--- | :---: | :---: |
| A-1 | Asignar a los dos líderes | — | ⬜ |
| A-2 | Rotar la contraseña de `sa`, crear el login de solo lectura del bridge y definir `BridgeConfig__SqlConnectionString` en el VPS | 1 | ⬜ |
| A-3 | Ejecutar la matriz del SDK: F (solo lectura) → A → B → C-02/C-03 → D → E → G | 1 | ⬜ |
| A-4 | Escribir el contrato `bridge-v1` a partir de la API actual del bridge y del catálogo de comandos (CT-18) | 1 y 2 | ⬜ |
| A-5 | Bridge en modo simulado (CT-21) | 1 | ⬜ |
| A-6 | Decidir la versión de .NET (P-20) | 2 | ⬜ |
| A-7 | Ejecutar la spec 001 (réplica en Angular) | 2 | ⬜ |
| A-8 | Crear la spec del módulo 0 (Plataforma) | 2 | ⬜ |

## 7. Ramas de contingencia de la matriz del SDK

| Si falla… | Consecuencia | Costo |
| :--- | :--- | :--- |
| **B** (traspaso entre almacenes) | WIP y tránsito pasan a ser ubicaciones internas de PolyConecta; se revierten D-22 y parte de D-43 | Se pierde el reflejo contable de WIP y tránsito; la recolección se simplifica |
| **C-02 / C-03** (multilote, fraccionamiento) | Solo se reserva el lote completo; el bridge emite un movimiento por lote | Rediseñar la reserva lote por lote |
| **D-02** (devolución parcial manual) | Devolución total o nada | Rediseñar la devolución re-pesada (008-FR-009b) |
| **E** (backorder) | El backorder vive solo en PolyConecta | Aceptable |
| **A-05** (clasificación) | PolyConecta mantiene su propia clasificación | Un catálogo más que mantener |
| **T-10** (alta de almacenes) | Los almacenes se crean a mano en CONTPAQi una vez | Aceptable |
| **T-12** (alta de pedido) | El pedido libre queda interno hasta resolverlo | Ventas se cierra integrado sin el modo libre hacia CONTPAQi |

## 8. Riesgos fuera de la matriz

- **Concurrencia de reservas**: CONTPAQi no conoce las reservas; dos pedidos no pueden comprometer el mismo lote. Requiere bloqueo optimista o restricción única en la base.
- **Deriva del contrato**: si los caminos cambian el contrato por separado, la integración falla al final. Por eso los cambios requieren a los dos líderes y la misma suite corre en los dos lados (CT-22, CT-23).
- **Base de laboratorio distinta de producción**: hay que refrescar el laboratorio cuando cambie el catálogo real.
- **Fin de soporte de .NET 8** (10-nov-2026): ver P-20.
- **Capacidad**: el roadmap no fija semanas; el avance se mide con el tablero, no con fechas comprometidas.
