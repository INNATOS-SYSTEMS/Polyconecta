# Roadmap de construcción

La construcción avanza **módulo a módulo** por **dos caminos en paralelo**, cada uno con su líder y sus agentes. Las reglas de cómo se construye están en la [constitución técnica](diseno/06-constitucion-tecnica.md); aquí están el orden, qué entrega cada camino por módulo y el tablero de avance.

**Actualizado:** 30 de septiembre de 2026 (resultados de la matriz del SDK).

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
| 0 | **Plataforma** | Base SQL Server y migraciones, mixins de auditoría y archivado, `StateTransitionLog`, numeración centralizada, identidad, roles, permisos y suplentes, outbox y despachador, mapeo ERP, estado de sincronización, vistas de búsqueda con agrupaciones en las listas, chatter guardado en la base (D-78) | [01 §3](diseno/01-modulos-y-roles.md), [04 §1](diseno/04-modelo-de-dominio.md), [06](diseno/06-constitucion-tecnica.md) |
| 1 | **Catálogos e Inventario** | Productos (sincronizados), ficha técnica, unidades de venta, clasificación, plantas, ubicaciones y su alta en CONTPAQi, lotes, existencias (las cinco cifras), reservas, re-lotificación | [03](diseno/03-almacenes-y-operaciones.md), [04 §3](diseno/04-modelo-de-dominio.md) |
| 2 | **Ventas** | Pedido sincronizado y libre, confirmación, autorización de dos firmas, revocación, motor de abastecimiento (rutas MTSO/MTO, simulación, reserva), visor de disponibilidad | [02 §1–2](diseno/02-flujo-y-reglas.md) |
| 3 | **Producción** | Orden de fabricación autorreferenciada para extrusión, impresión y bolseo; componentes, subproductos, planeación, centros de trabajo, recolección a WIP y devolución, captura de producción, registro dual millares/kg, incidencias, cierre técnico y balance de masa | [02 §3–4](diseno/02-flujo-y-reglas.md) |
| 4 | **Calidad** | Control de calidad como documento, aprobación y rechazo por lote, cuarentena `.S`, hard-stop, re-liberación | [02 §3](diseno/02-flujo-y-reglas.md) |
| 5 | **Logística** | Traslado y recepción interplanta en dos pasos, entrega a cliente, backorder | [02 §5](diseno/02-flujo-y-reglas.md) |

**Transversal a todos:** la presentación en Angular. La spec 001 replica el prototipo con estado en el navegador; cada módulo conecta después sus pantallas a la API al cerrarse en PolyConecta.

## 3. Qué entrega cada camino por módulo

| Módulo | Camino 2 · PolyConecta | Camino 1 · Integración | Pruebas del SDK que lo sostienen |
| :--- | :--- | :--- | :--- |
| 0 · Plataforma | Todo lo de la tabla anterior, más el despachador del outbox hacia el contrato | Contrato `v1` publicado; **bridge en modo simulado**; idempotencia, callbacks y DLQ recuperable; sesión del SDK con usuario (CT-40); ejecución por pasos con reconciliación (CT-38, corrige G-01 y G-04); validación y verificación (CT-39); lecturas de catálogos | G-01..G-05, F-01..F-03 |
| 1 · Catálogos e Inventario | Tablas `inv`, sincronización de productos y de recepciones de compra (D-102), clasificación propia (D-86), ubicaciones con su id ERP, existencias agrupadas por número de lote y reservas | `ALTA_ALMACEN` (o alta manual si T-10 falla) y alta de conceptos propios (D-89); `TRASPASO` como par Salida + Entrada con N lotes (D-79, D-82); lectura de existencias (D-87) | A, B, C, F, T-10, T-11, T-14 |
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

**Diseño cerrado** significa que el módulo no tiene preguntas abiertas que bloqueen su spec. Hoy ninguno lo cumple del todo; ver la sección 5. De las decisiones derivadas de la matriz, solo D-88 (sesión del SDK) sigue en *Propuesta*, pendiente de H-03.

## 5. Lo que bloquea cada módulo

| Módulo | Preguntas abiertas ([preguntas-abiertas.md](diseno/preguntas-abiertas.md)) |
| :--- | :--- |
| 0 · Plataforma | ninguna propia |
| 1 · Catálogos e Inventario | T-06, T-14 |
| 2 · Ventas | P-22 |
| 3 · Producción | ninguna propia (D-111 por validar) |
| 4 · Calidad | ninguna propia |
| 5 · Logística | P-22 (la remisión no se liga a un pedido) |

Para el **cierre integrado** de cualquier módulo hace falta además el inicio de sesión automático del bridge (H-03, S-04). Para el **piloto**, el hosting de producción y los respaldos (H-01, H-02).

## 6. Puesta en marcha

Son datos de la operación que no bloquean la construcción (D-75): cada módulo se construye con catálogos editables y datos de ejemplo. Los datos reales se cargan **antes del piloto** del módulo que los usa.

| Dato | Módulo | Quién lo entrega | Estado |
| :--- | :--- | :--- | :---: |
| Titulares y suplentes de cada rol, con su planta (ex P-02) | 0 · Plataforma | Dirección | ⬜ |
| Catálogo de centros de trabajo: código, proceso, planta y capacidad (ex P-14) | 3 · Producción | Planners de PIM y SC | ⬜ |
| Tolerancia del balance de masa (ex P-16) | 3 · Producción | Producción | ⬜ |
| Ventana nocturna para el reinicio del bridge y la conciliación (D-98) | 0 · Plataforma | Sistemas | ⬜ |
| Depurar en CONTPAQi antes de la carga inicial: productos cuya existencia no cuadra con sus lotes (F-03) y existencias negativas (F-06) (D-100) | 1 · Inventario | Sistemas | ⬜ |
| Capturar las compras de MP en el almacén de MP de cada planta, no en "Gastos" (D-107) | 1 · Inventario | Compras y Sistemas | ⬜ |
| Traspasar en CONTPAQi la existencia de MP que está en "Gastos" y otros almacenes a los almacenes de MP de cada planta (D-107) | 1 · Inventario | Sistemas | ⬜ |
| Carga inicial de almacenes e inventarios, con WIP vacío (D-100) | 1 · Inventario | Sistemas | ⬜ |
| Fecha del corte limpio de arranque: sin producción en curso ni OF abiertas (D-104) | Todos | Dirección y Producción | ⬜ |
| Retirar de la captura manual en CONTPAQi los conceptos de producción (por ejemplo "Salida materia prima MAQUINA N") al arrancar cada módulo (D-96) | 1 y 3 | Sistemas y Dirección | ⬜ |

## 7. Trabajo de arranque

| # | Trabajo | Camino | Estado |
| :---: | :--- | :---: | :---: |
| A-1 | Asignar a los dos líderes | — | ⬜ |
| A-2 | Rotar la contraseña de `sa`, crear el login de solo lectura del bridge y definir `BridgeConfig__SqlConnectionString` en el VPS | 1 | ⬜ |
| A-3 | Ejecutar la matriz del SDK: F (solo lectura) → A → B → C-02/C-03 → D → E → G. Ejecutada el 30-sep (commit `7f0596e`), 35 de 36; queda F-05 | 1 | ✅ |
| A-13 | Cotejar con la UI de CONTPAQi F-01, F-02 y F-05, y crear los WIP por planta para A-04 (T-06, T-14) | 1 | ⬜ |
| A-14 | Corregir el gateway del bridge según la matriz: sesión de larga duración, par Salida + Entrada, N lotes, pasos con reconciliación, validación y verificación (D-79 a D-82, D-88, D-91) | 1 | ⬜ |
| A-15 | ✅ 1-oct: par en 2.3 s (D-109); causa de los bloqueos encontrada (D-108). Medir en el laboratorio (pruebas S-01, S-02) la latencia con sesión de larga duración: tiempo de iniciar el SDK, de abrir la empresa y de cada par Salida + Entrada, contra la meta de segundos (D-92, T-13) | 1 | ⬜ |
| A-16 | Probar el bridge como servicio de Windows con una cuenta de usuario real y, si falla, como tarea programada con inicio de sesión automático (pruebas S-03 a S-05, H-03). S-03 y S-05 hechas: no puede ser servicio. Falta S-04 (reinicio) | 1 | 🟨 |
| A-17 | Ejecutar el resto del bloque S de la matriz: reconciliación (S-06 a S-08), almacenes y conceptos (S-09, S-10), cierre y remisión (S-11 a S-13), pedido libre y compras (S-14 a S-16). Hecho el 1-oct salvo S-04 y el movimiento a los almacenes de S-09 | 1 | 🟨 |
| A-4 | Escribir el contrato `bridge-v1` a partir de la API actual del bridge y del catálogo de comandos (CT-18) | 1 y 2 | ⬜ |
| A-5 | Bridge en modo simulado (CT-21) | 1 | ⬜ |
| A-6 | ~~Decidir versiones~~ — ratificadas el 29-sep (D-67 a D-73) | 1 y 2 | ✅ |
| A-9 | Migrar PolyConecta a .NET 10, EF Core SQL Server, xUnit v3 y AwesomeAssertions; quitar Npgsql, EF InMemory de producción y MediatR; centralizar versiones (D-73) | 2 | ⬜ |
| A-10 | Verificar el bridge en .NET 10 `win-x86` con `sdk-lab` (F y G) y migrarlo (D-67). La matriz corrió en .NET 8 | 1 | ⬜ |
| A-11 | Pipeline de GitHub Actions: build, pruebas .NET y Angular, suite de contrato contra el simulador y SQL Server 2022 en contenedor (D-76, CT-27) | 1 y 2 | ⬜ |
| A-12 | Definir el hosting de producción y los respaldos (H-01, H-02) | — | ⬜ |
| A-7 | Ejecutar la spec 001 (réplica en Angular) | 2 | ⬜ |
| A-8 | Crear la spec del módulo 0 (Plataforma) | 2 | ⬜ |

## 8. Ramas de contingencia de la matriz del SDK

Resultado de la ejecución del 30-sep-2026:

| Bloque | Resultado | Consecuencia en el diseño |
| :--- | :---: | :--- |
| **B** (traspaso entre almacenes) | ⚠️ | El traspaso nativo no es viable, pero sí el par Salida + Entrada. WIP y tránsito siguen siendo almacenes (D-79) |
| **C-02 / C-03** (multilote, fraccionamiento) | ✅ | Se mantiene la reserva lote por lote (D-82) |
| **C-04, D-03** (linaje y capas) | ❌ | PolyConecta lleva el linaje; las lecturas agrupan por número de lote (D-83) |
| **D-02** (devolución parcial manual) | ✅ | Se mantiene la devolución re-pesada |
| **D-04** (desafectación) | — | Cancelar es siempre documento inverso (D-84) |
| **E** (backorder) | ⚠️ | Se aplica la contingencia: el backorder vive en PolyConecta (D-85) |
| **A-05** (clasificación) | ⚠️ | Se aplica la contingencia: clasificación propia (D-86) |
| **F** (existencias) | ✅ / ⏳ | Lectura directa sin proyección (D-87); falta F-05 |
| **G** (robustez) | ❌ / ✅ | Sin transacción ni idempotencia en el SDK: pasos con reconciliación y verificación en el bridge (D-80, D-81) |
| **S** (segunda ronda, 1-oct) | ✅ / ⚠️ | Bloqueos explicados por dos inicios de sesión (D-108); traspaso en 2.3 s (D-109); reconciliación y borrado de huérfanos probados; alta de almacenes por SDK (D-110); consumo y entrada de producción (D-111); la remisión no se liga a un pedido (P-22); el bridge no puede ser servicio (S-03) |

Siguen abiertas:

| Si falla… | Consecuencia | Costo |
| :--- | :--- | :--- |
| **T-12** (alta de pedido) | El pedido libre queda interno hasta resolverlo | Ventas se cierra integrado sin el modo libre hacia CONTPAQi |
| **H-03 / S-04** (inicio de sesión automático) | Alguien inicia sesión en el servidor tras cada reinicio | Riesgo operativo; alertar si el bridge no está vivo |

## 9. Riesgos fuera de la matriz

- **Concurrencia de reservas**: CONTPAQi no conoce las reservas; dos pedidos no pueden comprometer el mismo lote. Requiere bloqueo optimista o restricción única en la base.
- **Deriva del contrato**: si los caminos cambian el contrato por separado, la integración falla al final. Por eso los cambios requieren a los dos líderes y la misma suite corre en los dos lados (CT-22, CT-23).
- **Base de laboratorio distinta de producción**: hay que refrescar el laboratorio cuando cambie el catálogo real.
- **Fin de soporte de .NET 8** (10-nov-2026): la migración a .NET 10 (A-9, A-10) debe cerrarse antes de esa fecha.
- **Capacidad**: el roadmap no fija semanas; el avance se mide con el tablero, no con fechas comprometidas.
