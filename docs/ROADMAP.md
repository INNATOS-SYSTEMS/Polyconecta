# Roadmap de construcción

La construcción avanza **fase a fase** según el [plan de trabajo](plan/Tarea%20(project.task)%20-%20replaneacion(2).xlsx) (D-116), por **dos caminos en paralelo**, cada uno con su líder y sus agentes. Las reglas de cómo se construye están en la [constitución técnica](diseno/06-constitucion-tecnica.md). Aquí están las fases, las specs, qué entrega cada camino y el tablero de avance.

**Actualizado:** 5 de octubre de 2026 (replaneación por fases y líderes asignados).

---

## 1. Los dos caminos

| | Camino 1 · Integración CONTPAQi | Camino 2 · PolyConecta independiente |
| :--- | :--- | :--- |
| **Líder** | **Alejandro Ponce** | **Luis Alvarado Martinez** |
| **Objetivo** | Que cada comando del contrato del bridge funcione contra CONTPAQi | Que cada fase funcione completa con sus propias tablas, contra el bridge simulado |
| **Construye** | Bridge real, `tools/sdk-lab`, matriz de pruebas del SDK y bridge simulado | Dominio, casos de uso, SQL Server, API y Angular |
| **Se prueba contra** | CONTPAQi de laboratorio (empresa `_LAB`) | Bridge en modo simulado |

Los dos caminos se encuentran en el **contrato del bridge** (`docs/contratos/bridge-v1.md`, CT-17 a CT-23). El camino 2 lo consume y el camino 1 lo cumple. Una fase queda integrada cuando los dos lados pasan la misma suite de contrato.

**Cambio de responsable desde F6 (D-117).** Cuando el conector queda listo (tarea 6.2), Alejandro Ponce también construye en PolyConecta las entregas (F6) y los traslados (F7). En esas fases es dueño de las carpetas del camino 2 que toque su spec y coordina con Luis Alvarado Martinez los cambios a lo común: dominio compartido, migraciones y navegación.

## 2. Fases y specs

Cada fase de F0 a F8 es **una spec** de Spec Kit en `.specify/features/NNN-<fase>/`, con sus tareas separadas por camino (CT-34). Cada fase termina en una revisión con la operación. F9 no lleva spec: ahí se empaqueta, se documenta y se entrega, y los ajustes que pidan las revisiones se hacen en la spec de la fase que tocan.

| Fase | Spec | Fechas | Revisión | Áreas del dominio |
| :--- | :--- | :--- | :---: | :--- |
| F0 · Construcción técnica | `002-construccion-tecnica` | 5 – 9 oct | — | Plataforma |
| F1 · Pedidos de venta | `003-pedidos-de-venta` | 12 – 19 oct | R1 | Plataforma, Catálogos, Ventas |
| F2 · Planeación de producción | `004-planeacion-produccion` | 15 oct – 2 nov | R2 | Producción, Ventas (alta en CONTPAQi), Plataforma (sincronización) |
| F3 · Almacén | `005-almacen` | 26 oct – 9 nov | R3 | Inventario, Producción (recolección) |
| F4 · Captura de producción | `006-captura-produccion` | 6 – 13 nov | R4 | Producción |
| F5 · Calidad y cierre | `007-calidad-y-cierre` | 28 oct – 23 nov | R5 | Calidad, Producción (cierre y balance) |
| F6 · Entregas | `008-entregas` | 4 – 30 nov | R6 | Logística, Ventas (cierre del pedido) |
| F7 · Traslados entre plantas | `009-traslados` | 26 nov – 7 dic | R7 | Logística, Inventario |
| F8 · Flujo completo | `010-flujo-completo` | 5 nov – 14 dic | R8 | Todas, más la conciliación nocturna |
| F9 · Ajustes y cierre | sin spec | 30 nov – 23 dic | R9 | Instalación, manual, ajustes de R8 y entrega |

La spec `001-angular-presentation` (réplica del prototipo) **sigue en paralelo** al plan, sin horas asignadas en él (D-118).

**Áreas del dominio.** Los 6 módulos de D-64 (Plataforma, Catálogos e Inventario, Ventas, Producción, Calidad y Logística) siguen siendo las áreas del modelo y sus esquemas (`inv`, `ven`, `prd`, `cal`, `log`). Ya no marcan el orden de construcción. La conversión en Santa Cruz (impresión y bolseo) es parte de Producción (D-64).

## 3. Qué entrega cada camino por fase

Los números son las tareas del plan.

| Fase | Camino 1 · Alejandro Ponce | Camino 2 · Luis Alvarado Martinez | Pruebas del SDK que lo sostienen |
| :--- | :--- | :--- | :--- |
| **F0** | 0.1 credenciales de `sa` y login de solo lectura · 0.2 contrato `bridge-v1`, con los dos líderes · 0.4 bridge en .NET 10 verificado con `sdk-lab` · 0.7 bridge simulado (CT-21) · 0.9 inicio de sesión automático del administrador con la tarea del bridge (D-115) | 0.2 contrato `bridge-v1` · 0.3 migración a .NET 10, EF Core SQL Server, xUnit v3 y AwesomeAssertions (D-73) · 0.5 aplicación Angular: estructura, navegación y estilo · 0.6 mixins de auditoría y archivado, `StateTransitionLog`, numeración centralizada, outbox y despachador · 0.8 CI en GitHub Actions con SQL Server (D-76) | F, G |
| **F1** | 1.1 sesión de larga duración: doble inicio de sesión y timeouts (D-88, D-91, D-108) · 1.4 lectura de catálogos y existencias (D-87) | 1.2 identidad, roles, permisos por planta y suplentes · 1.3 sincronización de productos, clientes y almacenes, con el mapeo ERP · 1.5 pedido de venta: captura, confirmación y autorización con dos firmas · 1.6 vistas de búsqueda, agrupaciones y chatter guardado en la base (D-78) | F, S-01, S-02 |
| **F2** | 2.2 ejecución por pasos con reintentos, reconciliación y verificación (CT-38, CT-39, D-80, D-81) · 2.4 `ALTA_PEDIDO` con folio, precio y moneda (D-113) | 2.1 orden de fabricación ligada al pedido y cadena de órdenes · 2.3 componentes con existencia y subproductos · 2.5 envío del pedido con estado de sincronización · 2.6 centros de trabajo y planeación por máquina · 2.7 tablero de sincronización: errores, reintentos y DLQ | G, S-06 a S-08, S-14 |
| **F3** | 3.1 `TRASPASO` como par Salida + Entrada con N lotes y costo (D-79, D-82) · 3.2 traspasos MP ↔ WIP y `ALTA_ALMACEN` de los WIP (D-110) | 3.3 recolección ligada a la OF · 3.4 entregas parciales de recolecciones validadas · 3.5 saldo en WIP por OF, recolección libre y asignación (D-55) · 3.6 devoluciones a stock | B, C, S-09 |
| **F4** | — | 4.1 lotes proyectados y lotificación planeada · 4.2 captura de lotes producidos · 4.3 registro de producción masiva · 4.4 incidencias de máquina | — |
| **F5** | 5.1 `CIERRE_PRODUCCION`: consumo desde WIP, PT con lote y costo, y subproductos (D-111) | 5.2 calidad por OF, con aprobación y rechazo por lote · 5.3 cuarentena `.S` y hard-stop del cierre · 5.4 cierre técnico y scrap por motivo · 5.5 balance de masa · 5.6 envío del cierre y los ajustes a CONTPAQi | D, S-11, S-12 |
| **F6** | 6.1 `REMISION` y cancelación del pedido surtido (D-114) · 6.2 suite completa contra el laboratorio: **conector listo** · en PolyConecta (D-117): 6.3 orden de entrega con lotes liberados · 6.4 entrega parcial y backorder (D-85) · 6.5 remisión desde la entrega y cierre del pedido | 6.6 ajustes pedidos en R1 a R5 | E, S-13, S-17, T-07 |
| **F7** | En PolyConecta (D-117): 7.1 traslado de origen a tránsito · 7.2 recepción en destino y existencia en tránsito · 7.3 inventario de MP y WIP por planta · 7.4 hard-stop de traslados y entregas con lotes sin liberar | — | B |
| **F8** | 8.1 conciliación nocturna de existencias (D-97, D-98, D-101) · 8.2 y 8.3 flujo completo e integración en laboratorio, con los dos líderes | 8.2 secuencia del flujo de pedido a entrega · 8.3 prueba integrada y correcciones | F-05 |
| **F9** | 9.2 a 9.5, con los dos líderes | 9.1 paquete de instalación y guía de despliegue · 9.2 manual por rol · 9.3 ajustes de R8 · 9.5 cierre y entrega | — |

El camino 1 puede adelantarse al 2: por ejemplo, 5.1 empieza el 28-oct, antes de que exista la captura (F4). Nunca se atrasa la integración de una fase por el orden de construcción de otra.

### Alcance del diseño sin tarea en el plan

Estos elementos están en `docs/diseno/` pero ninguna tarea del plan los nombra. Se asignan a la spec que les corresponde para que no se pierdan. Cada líder confirma al redactar su spec si caben en las horas o si se registran como fuera de alcance en [preguntas-abiertas.md](diseno/preguntas-abiertas.md).

| Elemento | Diseño | Spec propuesta |
| :--- | :--- | :--- |
| Pedido capturado en CONTPAQi que entra por sincronización (D-53) | [02 §1](diseno/02-flujo-y-reglas.md) | `003-pedidos-de-venta` |
| Revocación de la autorización | [02 §1](diseno/02-flujo-y-reglas.md) | `003-pedidos-de-venta` |
| Ficha técnica, unidades de venta y clasificación propia (D-86) | [04 §3](diseno/04-modelo-de-dominio.md) | `003-pedidos-de-venta` |
| Motor de abastecimiento (rutas MTSO/MTO, simulación y reserva) y visor de disponibilidad | [02 §2](diseno/02-flujo-y-reglas.md) | `004-planeacion-produccion` |
| Reservas lote por lote con control de concurrencia (§9) | [03](diseno/03-almacenes-y-operaciones.md) | `005-almacen` |
| Re-lotificación | [03](diseno/03-almacenes-y-operaciones.md) | `005-almacen` |
| Recepciones de compra sincronizadas como `IN-COMPRA` (D-102) | [03](diseno/03-almacenes-y-operaciones.md) | `005-almacen` |
| Aviso de cobertura nocturna (D-103) | [02 §3](diseno/02-flujo-y-reglas.md) | `005-almacen` |
| Registro dual millares/kg en bolseo | [02 §4](diseno/02-flujo-y-reglas.md) | `006-captura-produccion` |
| Re-liberación de lotes en cuarentena | [02 §3](diseno/02-flujo-y-reglas.md) | `007-calidad-y-cierre` |
| Carga inicial de almacenes e inventarios (D-100) | [03](diseno/03-almacenes-y-operaciones.md) | `010-flujo-completo` |
| Cotejo de F-01, F-02 y F-05 con la UI de CONTPAQi (T-06, ex A-13) | [preguntas-abiertas](diseno/preguntas-abiertas.md) | `003-pedidos-de-venta` (con 1.4) |

## 4. Tablero de avance

Estados: ⬜ pendiente · 🟨 en curso · ✅ cerrado · ⛔ bloqueado. Al cerrar, anota la fecha y el commit (CT-35). Los dos cierres de cada fase están en la [constitución técnica §9](diseno/06-constitucion-tecnica.md).

| Fase | Diseño cerrado | Spec | Cerrado en PolyConecta | Comandos entregados (camino 1) | Cerrado integrado | Revisión |
| :--- | :---: | :--- | :---: | :---: | :---: | :---: |
| Réplica Angular (paralelo) | ✅ | `001-angular-presentation` | ⬜ | n/a | n/a | — |
| F0 · Construcción técnica | ✅ | `002-construccion-tecnica` · borrador, por ratificar | ⬜ | ⬜ | ⬜ | — |
| F1 · Pedidos de venta | 🟨 | `003-pedidos-de-venta` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R1 |
| F2 · Planeación de producción | ✅ | `004-planeacion-produccion` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R2 |
| F3 · Almacén | ✅ | `005-almacen` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R3 |
| F4 · Captura de producción | ✅ | `006-captura-produccion` · por crear | ⬜ | n/a | ⬜ | ⬜ R4 |
| F5 · Calidad y cierre | ✅ | `007-calidad-y-cierre` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R5 |
| F6 · Entregas | 🟨 | `008-entregas` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R6 |
| F7 · Traslados entre plantas | ✅ | `009-traslados` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R7 |
| F8 · Flujo completo | 🟨 | `010-flujo-completo` · por crear | ⬜ | ⬜ | ⬜ | ⬜ R8 |
| F9 · Ajustes y cierre | n/a | sin spec | n/a | n/a | n/a | ⬜ R9 |

**Diseño cerrado** significa que la fase no tiene preguntas abiertas que bloqueen su spec (sección 5).

**Cuándo se crea cada spec.** Una spec se crea antes de que empiece su fase, una vez que su diseño está cerrado. La `002` va primero porque F0 arranca el 5-oct. Las de F1 y F2 se crean durante F0, cuando ya existe el contrato `bridge-v1` (tarea 0.2), porque sus comandos dependen de él.

## 5. Lo que bloquea cada fase

| Fase | Preguntas abiertas ([preguntas-abiertas.md](diseno/preguntas-abiertas.md)) o decisiones por validar |
| :--- | :--- |
| F0 | ninguna |
| F1 | T-06 (lectura de existencias, tarea 1.4): se puede especificar con la lectura directa de D-87 como supuesto. |
| F2 a F5, F7 | Dependen del contrato `bridge-v1` (0.2). T-17 (costo de la Entrada) se resuelve en el contrato antes de F3 |
| F6 | D-114 (cancelar el pedido remisionado) por validar con la operación |
| F8 | T-06 (la conciliación depende de F-05) |

Para el **cierre integrado** de cualquier fase hace falta el inicio de sesión automático del administrador con la tarea del bridge (tarea 0.9, D-115). Para el **piloto**, el hosting de producción y los respaldos (H-01, H-02).

## 6. Puesta en marcha

Son datos de la operación que no bloquean la construcción (D-75): cada fase se construye con catálogos editables y datos de ejemplo. Los datos reales se cargan **antes del piloto** de la fase que los usa.

| Dato | Fase | Quién lo entrega | Estado |
| :--- | :--- | :--- | :---: |
| Titulares y suplentes de cada rol, con su planta (ex P-02) | F1 | Dirección | ⬜ |
| Catálogo de centros de trabajo: código, proceso, planta y capacidad (ex P-14) | F2 | Planners de PIM y SC | ⬜ |
| Tolerancia del balance de masa (ex P-16) | F5 | Producción | ⬜ |
| Ventana nocturna para el reinicio del bridge y la conciliación (D-98) | F1 y F8 | Sistemas | ⬜ |
| Crear en CONTPAQi los conceptos propios por la UI (D-89, D-110) | F3 y F5 | Sistemas | ⬜ |
| Depurar en CONTPAQi antes de la carga inicial: productos cuya existencia no cuadra con sus lotes (F-03) y existencias negativas (F-06) (D-100) | F8 | Sistemas | ⬜ |
| Capturar las compras de MP en el almacén de MP de cada planta, no en "Gastos" (D-107) | F3 | Compras y Sistemas | ⬜ |
| Traspasar en CONTPAQi la existencia de MP que está en "Gastos" y otros almacenes a los almacenes de MP de cada planta (D-107) | F3 | Sistemas | ⬜ |
| Carga inicial de almacenes e inventarios, con WIP vacío (D-100) | F8 | Sistemas | ⬜ |
| Fecha del corte limpio de arranque: sin producción en curso ni OF abiertas (D-104) | F9 | Dirección y Producción | ⬜ |
| Retirar de la captura manual en CONTPAQi los conceptos de producción (por ejemplo "Salida materia prima MAQUINA N") al arrancar (D-96) | F9 | Sistemas y Dirección | ⬜ |

## 7. Trabajo de arranque

Lo que quedaba pendiente de la lista anterior lo absorben tareas del plan.

| # | Trabajo | Estado | Ahora es |
| :---: | :--- | :---: | :--- |
| A-1 | Asignar a los dos líderes | ✅ 5-oct (D-117) | — |
| A-2 | Rotar la contraseña de `sa`, crear el login de solo lectura del bridge y definir `BridgeConfig__SqlConnectionString` en el VPS | ⬜ | 0.1 |
| A-3 | Ejecutar la matriz del SDK. 30-sep, commit `7f0596e`: 35 de 36; queda F-05 | ✅ | — |
| A-4 | Escribir el contrato `bridge-v1` (CT-18) | ⬜ | 0.2 |
| A-5 | Bridge en modo simulado (CT-21) | ⬜ | 0.7 |
| A-6 | Decidir versiones: ratificadas el 29-sep (D-67 a D-73) | ✅ | — |
| A-7 | Ejecutar la spec 001 (réplica en Angular) | ⬜ | En paralelo (D-118) |
| A-8 | Crear la spec de Plataforma | ⬜ | Spec `002-construccion-tecnica` |
| A-9 | Migrar PolyConecta a .NET 10, EF Core SQL Server, xUnit v3 y AwesomeAssertions; quitar Npgsql, EF InMemory de producción y MediatR; centralizar versiones (D-73) | ⬜ | 0.3 |
| A-10 | Verificar el bridge en .NET 10 `win-x86` con `sdk-lab` (F y G) y migrarlo (D-67) | ⬜ | 0.4 |
| A-11 | Pipeline de GitHub Actions (D-76, CT-27) | ⬜ | 0.8 |
| A-12 | Definir el hosting de producción y los respaldos (H-01, H-02) | ⬜ | Sin tarea; antes de 9.1 |
| A-13 | Cotejar F-01, F-02 y F-05 con la UI de CONTPAQi (T-06). Los WIP por planta se crearon en S-09 | ⬜ | Con 1.4 |
| A-14 | Corregir el gateway del bridge según la matriz (D-79 a D-82, D-88, D-91) | ⬜ | 1.1, 2.2, 3.1 |
| A-15 | Medir la latencia con sesión de larga duración: 2.3 s por par (D-109) | ✅ 1-oct | — |
| A-16 | Probar el bridge como servicio y con inicio de sesión automático: corre en la sesión del administrador (D-115) | ✅ | — |
| A-17 | Bloque S de la matriz. Hecho el 1-oct, salvo el movimiento a los almacenes de S-09 | 🟨 | 3.2 |
| A-18 | Ventana de mantenimiento: S-04, S-09 y S-17 hechas el 1-oct | ✅ | — |
| A-19 | Revertir el usuario de prueba `polyconecta-bridge` y configurar el inicio de sesión automático del administrador con la tarea del bridge (D-115) | ⬜ | 0.9 |

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
| **T-12** (alta de pedido) | El pedido libre queda interno hasta resolverlo | F2 se cierra integrado sin el alta del pedido en CONTPAQi |
| **D-115** (sesión del administrador) | Si se desactiva el inicio de sesión automático, alguien inicia sesión tras cada reinicio | Riesgo operativo; alertar si el bridge no está vivo |

## 9. Riesgos fuera de la matriz

- **Concurrencia de reservas**: CONTPAQi no conoce las reservas; dos pedidos no pueden comprometer el mismo lote. Requiere bloqueo optimista o restricción única en la base.
- **Deriva del contrato**: si los caminos cambian el contrato por separado, la integración falla al final. Por eso los cambios requieren a los dos líderes y la misma suite corre en los dos lados (CT-22, CT-23).
- **Base de laboratorio distinta de producción**: hay que refrescar el laboratorio cuando cambie el catálogo real.
- **Fin de soporte de .NET 8** (10-nov-2026): la migración a .NET 10 está en F0 (0.3, 0.4).
- **F0 es la fase más apretada**: contrato, migración, simulador y CI en una semana, y todas las demás fases dependen del contrato (0.2) y del simulador (0.7). Si se atrasan, se mueven las fechas de F1 y F2.
- **Carga de Alejandro Ponce en F6 y F7**: se le suman la construcción en PolyConecta y el cierre del conector (6.2, 36 h). Las fases se traslapan con F8 (8.1 empieza el 5-nov).
- **Spec 001 sin horas**: corre en paralelo sin responsable en el plan (D-118); compite por tiempo con 0.5 y con las pantallas de cada fase.
