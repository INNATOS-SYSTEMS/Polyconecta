# PLAN DE CONSTRUCCIÓN — POLYCONECTA (POC → producción)

**Fecha:** 24 de septiembre de 2026 · **Actualizado:** 28 de septiembre de 2026 · **Base:** [diseño consolidado](diseno/) y [matriz de pruebas del SDK](contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md).
**Alcance de este documento:** orden de construcción por capas, dependencias y compuertas de decisión. Cada fase se especifica con `/speckit-specify` a partir de `docs/diseno/` y se detalla con `/speckit-plan` y `/speckit-tasks`.

> **Estado no verificado.** Este plan se derivó de la documentación y de una lectura parcial del código. No se ejecutó la suite de pruebas ni la POC. Antes de la Fase 1 hay que correr `./run.sh` y registrar la línea base real.

---

## 1. Principios de secuenciación

1. **Lo que solo se sabe con CONTPAQi real se prueba primero**, en paralelo con todo lo demás (matriz del SDK, ver `tools/sdk-lab`).
2. **Plataforma antes que funcionalidad**: sin persistencia real ni identidad, cada spec funcional se construye dos veces.
3. **Cada fase termina en algo que un usuario de planta pueda usar** y se cierra con verificación en navegador contra la API real (lección del prototipo: nada se marca hecho sin ejecutarse).
4. **Las specs ya tienen ramas de contingencia** en la matriz; el plan las hereda (sección 5).

## 2. Capas y su trabajo

| Capa | Hoy | Destino |
| :--- | :--- | :--- |
| **Domain** | Entidades Odoo-native parciales (`ManufacturingOrder` autorreferenciado, `StockLot`) | Modelo objetivo de [04-modelo-de-dominio.md](diseno/04-modelo-de-dominio.md): `PlanningLine`, ficha técnica, `QualityControl`, abastecimiento, WIP y recolección, numeración centralizada |
| **Infrastructure** | EF Core con Npgsql referenciado, pero la API corre en **InMemory**; outbox definido | **SQL Server** (D-49) con migraciones, repositorios reales, outbox transaccional, `IUnitOfWork` |
| **Api** | 4 controladores (Orders, Rolls, RawMaterials, Locations) | Contrato por caso de uso, auth, paginación y filtrado declarativo, ProblemDetails |
| **Contpaq (bridge)** | Gateway x86 con outbox SQLite, circuit breaker, dashboard | Contrato de movimientos corregido según la matriz, idempotencia, DLQ recuperable, lectura de existencias |
| **Presentation** | Prototipo Blazor Server sobre estado en memoria (`OperationalFlowState`, `InventoryState`…) | **Angular** (D-48): réplica 1:1 del prototipo y después conexión a la API, con vistas de búsqueda declarativas y permisos por rol |
| **Transversal** | Sin CI, secretos en `appsettings.json` | CI, gestión de secretos, observabilidad, despliegue repetible, migración desde Excel |

## 3. Fases

### Fase 0 — Desbloqueo y higiene (semanas 1–2, todo en paralelo)

| # | Trabajo | Capa | Salida |
| :--- | :--- | :--- | :--- |
| 0.1 | Ejecutar la matriz del SDK con `tools/sdk-lab` — orden: **F** (solo lectura, desde el día 1) → A → **B (punto de control)** → C-02/C-03 → D → E → G | Bridge | Registro de resultados completo y decisión sobre WIP y lotes |
| 0.2 | ~~Responder autenticación, revocación y segregación~~ — hecho el 28-sep (D-32 a D-34). Falta el dato de titulares y suplentes (P-02) | Producto | Decisión escrita |
| 0.3 | ~~Decidir motor de persistencia~~ — SQL Server (D-49). Cambiar Npgsql por el proveedor de SQL Server | Infra | Decisión registrada |
| 0.4 | **Secretos**: la cadena ya salió del código (variable `BridgeConfig__SqlConnectionString`). Falta **rotar la contraseña de `sa`**, crear el login de solo lectura del bridge y definir la variable en el VPS. El historial no se reescribe (D-51) | Transversal | Credencial rotada |
| 0.5 | Corregir **G-01** del gateway (documento huérfano tras fallo de movimiento) | Bridge | Prueba unitaria + G-01 verde |
| 0.6 | Correr la POC en navegador real y registrar la línea base | Presentation | Línea base honesta |
| 0.7 | ~~Reconciliar documentos~~ — hecho el 28-sep: `docs/diseno/` y [decisiones.md](diseno/decisiones.md) | Docs | Un solo relato |
| 0.8 | Verificar alta de almacenes por SDK (T-10), el registro de consumo, remisión y compra con ubicaciones virtuales como almacenes (T-11) y el alta de pedidos por SDK (T-12) | Bridge | Decisión sobre la reserva inicial en `admAlmacenes` |
| 0.9 | ~~Corregir `Contpaq.Bridge.Tests` y el nombre de la solución en `run.sh`~~ — hecho el 28-sep | Transversal | Las 17 pruebas corren |
| 0.10 | Réplica 1:1 del prototipo en Angular (`.specify/features/011-angular-presentation/`), en paralelo con la Fase 0 | Presentation | Paridad con las 18 pantallas |

**Compuerta G0:** resultados de la matriz A, B, C-02, C-03 y F, más T-10 y T-11. Las decisiones de producto y de persistencia ya están tomadas. Sin G0 no se congela el modelo de WIP, lotes y almacenes.

### Fase 1 — Plataforma (semanas 3–6)

- Persistencia real en SQL Server: migraciones, semillas de catálogos (almacenes, ubicaciones, rutas base), reserva inicial de ubicaciones en `admAlmacenes`, `IUnitOfWork`, outbox transaccional. Reemplazar `UseInMemoryDatabase`.
- Identidad con usuarios propios (ASP.NET Identity) y permisos según [01-modulos-y-roles.md](diseno/01-modulos-y-roles.md): roles, matriz, suplentes y reglas de fila por planta.
- Casos de uso implementados sobre la API; la réplica en Angular se conecta a ella y deja de mutar el estado en el navegador.
- CI mínimo: build + `PolyConecta.Domain.Tests` + `IntegrationTests` + `Contpaq.Bridge.Tests` en cada PR.
- Observabilidad: correlación de extremo a extremo (ya hay `CorrelationMiddleware` en el bridge) y logs estructurados en API.

**Salida:** el flujo actual de la POC funciona igual, pero persistido, con login y roles.

### Fase 2 — Comercial y autorización (semanas 6–9)

- Pedido multi-línea (`SalesOrderLine` con `PackagingUnit`; hoy solo hay una línea).
- Ingesta del pedido (maestro + detalle) desde CONTPAQi por el bridge, lectura vía SQL, y alta del pedido libre en CONTPAQi (D-53, sujeto a T-12 y P-19). La ficha técnica se captura en PolyConecta (D-01).
- Autorización de dos firmas con botón único, revocación (P-03), numeración centralizada de referencias.
- Visor de disponibilidad: **depende de F** y del resultado de A-05 (si CONTPAQi clasifica productos, el visor lee; si no, PolyConecta mantiene su clasificación).

### Fase 3 — Abastecimiento (semanas 9–13)

- `ProcurementRoute/Rule`, resolución en tres niveles, MTSO/MTO, simulación sin reserva, cascada por receta, `StockReservation`.
- Reserva lote por lote: **contingente a C-02/C-03** (sección 5).

### Fase 4 — Manufactura y WIP (semanas 12–18)

- `PlanningLine`/`WorkOrder`, ficha técnica multinivel, `QualityControl` como documento (hard-stop de Calidad, Principio IV).
- Recolección de componentes a WIP con parcialidades y backorder, devolución con cantidad manual: **contingente a B, C, D, E**.
- Pesaje iterativo con slots precargados, nomenclatura `R001-IV310-26` y cuarentena `.S`.

### Fase 5 — Cierre técnico y logística (semanas 17–22)

- Balance de masa: `Recolectado = Consumido + Devuelto + Scrap`; scrap por catálogo cerrado; **un solo descuento consolidado en CONTPAQi** al cierre.
- Traslado, recepción y entrega con spec formal (hoy solo prototipo). Riesgo abierto: enlace **Remisión ↔ Pedido** vía SDK — se prueba como caso adicional de la matriz **antes** de escribir esta spec.

### Fase 6 — Conversión en Santa Cruz y salida (semanas 22–28)

- Bolseo e impresión con registro dual millares/kg y factor real; cierre del pedido en `Hecho`.
- Piloto por planta, migración de los Excel vigentes, capacitación y operación en paralelo antes de apagar el proceso manual.

**Transversal en todas las fases:** cada documento se construye con sus dos modos, ligado y libre ("Nuevo"), según el Principio X y [02-flujo-y-reglas.md §0](diseno/02-flujo-y-reglas.md).

**Transversal (se cuela donde estorbe menos):** vistas de búsqueda declarativas para todos los modelos de una vez — después de identidad y permisos, ya que los favoritos dependen de usuarios y las reglas de fila no pueden ampliarse con filtros.

## 4. Dependencias críticas

```
Matriz F ─────────────────────────────► Visor (F2) ─► Rutas (F3)
Matriz A ─► B ─► (C-02/03) ─► D ─► E ──► Recolección WIP (F4)
D-32..D-38 ───────► Identidad (F1) ─► Firmas (F2) ─► Búsqueda
SQL Server (D-49) ─► Migraciones (F1) ─► todo lo demás
Prueba Remisión↔Pedido ─► Spec de logística (F5)
```

## 5. Ramas de contingencia (heredadas de la matriz)

| Si falla… | Consecuencia | Costo |
| :--- | :--- | :--- |
| **B** (traspaso entre almacenes) | WIP pasa a ser ubicación interna de PolyConecta; se revierte D-22 | Se pierde el reflejo contable de WIP; la recolección se simplifica |
| **C-02 / C-03** (multilote, fraccionamiento) | Solo se reserva el lote completo; el bridge emite un movimiento por lote | Rediseñar la reserva lote por lote; el bridge ya asume un lote por movimiento |
| **D-02** (devolución parcial manual) | Devolución total o nada | Rediseñar la devolución re-pesada (008-FR-009b) |
| **E** (backorder) | El backorder vive solo en PolyConecta | Aceptable |
| **A-05** (clasificación) | PolyConecta mantiene su propia clasificación y proyección de existencias | Un catálogo más que mantener |

## 6. Riesgos fuera de la matriz

- **Concurrencia de reservas**: CONTPAQi no conoce las reservas; dos pedidos no pueden comprometer el mismo lote. Requiere bloqueo optimista o restricción única en BD.
- **Frescura de existencias**: F-04/F-05 fijan si basta consulta directa o hace falta una proyección.
- **Base de laboratorio ≠ producción**: conviene refrescar el laboratorio cuando cambie el catálogo real.
- **Equipo y capacidad**: el plan asume una cadencia que no se ha validado; las semanas son orientativas, no compromisos.
