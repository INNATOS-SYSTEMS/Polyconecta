# Handoff · Camino 2 (L2) de la fase F1 · PolyConecta

**Para:** el agente que retome el camino L2, sea Claude (Claude Code) o Gemini (Antigravity). Está escrito para que lo entiendas sin haber visto la conversación que lo produjo.
**Fecha:** 2026-10-08. **Lo escribió:** Claude (sesión que coordina los dos caminos), con lo que dejó el agente L2 al detenerse.

---

## 1. Qué es esto en un minuto

**PolyConecta** es una aplicación web de operación de planta para Polyempaques (película y bolsa de polietileno). **CONTPAQi Comercial** sigue siendo el sistema de registro: PolyConecta nunca escribe directo en sus tablas `adm*`; toda escritura va por un outbox y un **bridge** (.NET x86 con el SDK de CONTPAQi), que es otro proyecto del mismo repositorio. La interfaz imita a **Odoo 19**.

La construcción va por fases. Estamos en **F1 · Pedidos de venta**, la spec `003-pedidos-de-venta`. Se trabaja por **dos caminos**:
- **L1 (camino 1):** el bridge de CONTPAQi. **Ya terminó** y está integrado en la rama de la spec. No es tu trabajo.
- **L2 (camino 2):** PolyConecta (dominio, casos de uso, SQL Server, API y Angular). **Es tu trabajo.** Va por la mitad.

El objetivo de F1: Atención a Clientes captura un **pedido libre**, lo confirma y lo autorizan **Comercial y Cobranza con dos firmas de personas distintas**. Para eso F1 trae usuarios, grupos y permisos por planta; sincronización de productos, clientes, agentes y almacenes desde el bridge; ficha técnica y clasificación del producto; listas con búsqueda, filtros y agrupaciones; chatter guardado; y los datos para la revisión R1 con la operación.

---

## 2. Lee esto antes de tocar código (en este orden)

1. `AGENTS.md` (raíz): reglas del repositorio. **Obligatorio.**
2. `.specify/features/003-pedidos-de-venta/spec.md`: historias, requisitos (FR-001 a FR-032 y FR-031a), criterios y la tabla final **"Exploración y cambios"**, que registra todo lo que cambió después de ratificar.
3. `.specify/features/003-pedidos-de-venta/plan.md` (secciones Común y L2) y `research.md` (R-01 a R-11).
4. `.specify/features/003-pedidos-de-venta/data-model.md`: entidades, tablas, permisos iniciales y estados del pedido.
5. `.specify/features/003-pedidos-de-venta/contracts/api-f1.md` y `contracts/api-listas.md`: las rutas HTTP de PolyConecta.
6. `.specify/features/003-pedidos-de-venta/tasks.md`: **la sección L2 es tu lista de trabajo.** La tabla "Dependencias y orden" dice qué va antes.
7. `.specify/features/003-pedidos-de-venta/quickstart.md`: cómo se verifica la fase al final.
8. `docs/diseno/decisiones.md`: las decisiones **D-145 a D-156** son de esta fase y mandan sobre cualquier otro documento.
9. `docs/diseno/07-contratos-visuales.md`: cómo se arma toda pantalla. Las skills `.agents/skills/odoo-design-system` y `odoo-model-philosophy` ayudan con la interfaz.

Si dos documentos se contradicen: manda `docs/diseno/` y, dentro de él, la decisión más reciente de `decisiones.md`.

---

## 3. Reglas que no se negocian

1. **Rama propia.** Trabaja en `003-pedidos-de-venta-l2`. Nunca hagas commit en `main` ni en `003-pedidos-de-venta`. La integración la hace el coordinador o el usuario.
2. **Un commit por tarea**, con su id al inicio del mensaje (`L2-T022: casos de uso del pedido`). En español.
3. **Hecho es verificado.** Una tarea se marca `[x]` en `tasks.md` solo si sus pruebas corrieron y pasaron, y si es de interfaz, se vio en navegador (Playwright, como las pruebas de `PolyConecta.Web/e2e/`).
4. **Ninguna prueba se debilita ni se borra para que pase.** Si una prueba existente choca con una decisión ya tomada, se sustituye como dice la tarea y se explica en el commit.
5. **No toques las carpetas de L1:** `PolyConecta.Contpaq/`, `tests/Contpaq.Bridge.Tests/`, `tests/PolyConecta.Contract.Tests/`, `docs/contpaq/`, `docs/contratos/`, `scripts/vps/`. `PolyConecta.Presentation/` es de solo lectura.
6. **Las reglas de negocio viven en `Domain` y `Application`**, nunca en la interfaz ni en los controladores (CT-11). La API devuelve las acciones disponibles con su razón y la web solo las pinta.
7. **Ningún paquete nuevo** sin registrarlo antes en "Exploración y cambios" de `spec.md`. El único común que L2 toca es `Directory.Packages.props`.
8. **Lo que cambie el diseño** se registra en "Exploración y cambios" (camino `L2`). Si cambia una decisión validada, también en `docs/diseno/decisiones.md` con el **siguiente número libre (D-157 en adelante)**.
9. **Documentación, interfaz y commits en español.**
10. **Componente visual nuevo:** primero su contrato en `docs/diseno/07-contratos-visuales.md` y su ejemplo en la galería `/catalogo`, después la pantalla (CT-24).

---

## 4. Estado de git

| | |
| :--- | :--- |
| Rama | `003-pedidos-de-venta-l2` |
| Sale de | `003-pedidos-de-venta` (ya integró la rama de L1 hasta `6cc61c5`) |
| Último commit con tarea | `a092caa` L2-T021 |
| Commit de este handoff | `WIP L2: handoff para retomar (L2-T022 y L2-T023 a medias)`, encima de `a092caa` |
| Dónde está la rama | En un *worktree* de git: `/Users/emilio/Development/Sandbox/Polyconecta/.claude/worktrees/agent-a9fcca3e254117a41` |
| Remoto | **No está en GitHub.** Solo existe en esta máquina |

**Cómo retomar:**
- **Opción A, en el mismo worktree:** abre esa carpeta y trabaja ahí. Ya está en la rama correcta.
- **Opción B, en la carpeta principal del repositorio** (`/Users/emilio/Development/Sandbox/Polyconecta`): una rama no puede estar abierta en dos carpetas a la vez. Primero quita el worktree con `git worktree remove .claude/worktrees/agent-a9fcca3e254117a41` (desde la carpeta principal, ya que todo está en commit) y después `git switch 003-pedidos-de-venta-l2`.
- Antes de seguir, integra lo último de la rama de la spec: `git merge 003-pedidos-de-venta`. Trae el supervisor del bridge (L1-T011), que no toca tus carpetas.

---

## 5. Tareas

### Hechas (marcadas `[x]`, con pruebas en verde)

| Tarea | Qué es | Commit |
| :--- | :--- | :--- |
| L2-T001 | Paquete de Identity 10.0.12 | `0efa101` |
| L2-T002 | Dominio de seguridad: plantas, usuarios, grupos, catálogo de permisos y los diez grupos iniciales | `4729589` |
| L2-T003 | Configuraciones de `plt`, credenciales con Identity y sembradores | `bee8ef5` |
| L2-T004 | `ICurrentUser` con usuario, nombre visible y grupo ejercido | `8187dbf` |
| L2-T005 | Decorador de autorización por grupo y planta | `980080e` |
| L2-T006 | Reglas de fila y almacén genérico de agregados (`IAlmacen<T>`) | `6e83e94` |
| L2-T007 | Migración `F1_Seguridad` y siembra al arrancar la API | `66d1573` |
| L2-T008 | Autenticación con cookie y `SesionController` | `a841b9b` |
| L2-T009 | Casos de uso y API de usuarios y grupos; ligar el usuario a su agente de CONTPAQi | `5038f90`, `dc2c635` |
| L2-T013 | Catálogos de `inv` y `ven`, migración `F1_Catalogos` | `8c5939e` |
| L2-T014 | Puerto `IBridgeLecturas` y `BridgeLecturasHttp` | `835a9b2` |
| L2-T015 | Sincronización de catálogos, migración `F1_Sincronizacion` | `29a535a` |
| L2-T016 | Sincronización periódica y `SincronizacionController` | `d12dbfb` |
| L2-T017 | API de catálogos de Inventario y Ventas | `83a8914` |
| L2-T019 | Dominio del pedido de venta | `a26902b` |
| L2-T020 | Pruebas de dominio del pedido | `8e0f972` |
| L2-T021 | Configuración de `ven` y migración `F1_Pedidos` | `a092caa` |
| L2-T022 | Casos de uso del pedido de venta | `3746fcd` |
| L2-T023 | Controlador y pruebas de integración del pedido de venta | `00111ec` |
| L2-T027 | Contratos y vistas de búsqueda de las cinco listas de F1 | pendiente commit |
| L2-T036 | Rutas de la web con prefijo de módulo (D-155) | `0e7fa96` |

### Pendientes (en este orden)

| Tarea | Qué es | Notas |
| :--- | :--- | :--- |
| L2-T028 | Consulta de listas en el servidor y **listas híbridas** (D-151) | Implementación de `IConsultaDeLista<T>`, `ListasController` y pruebas. Ver `contracts/api-listas.md` |
| L2-T010 | Sesión en la web: proxy de desarrollo, guardia, interceptor y diálogo de inicio de sesión que no pierde la captura | El `HttpClient` y la guardia van en las rutas perezosas, no en la raíz (peso, ver §7) |
| L2-T011 | Contratos visuales nuevos: dos paneles en árbol (`pc-odoo-dual-list`) e inicio de sesión | Primero 07 y la galería, después las pantallas |
| L2-T012 | Pantallas de inicio de sesión, usuarios y grupos | Bajo `/plataforma/...` (D-155) |
| L2-T018 | Pantallas de productos (con ficha técnica), clientes y sincronización | `/inventario/productos`, `/ventas/clientes`, `/plataforma/sincronizacion` |
| L2-T029 | `OrigenHttp<T>`, favoritos por la API y **estado de la lista en la URL** | |
| L2-T024, L2-T025 | Web del pedido sobre la API: lista, kanban, formulario y "Nuevo" | Rutas `/ventas/pedidos/:id` (D-154, D-155). Moneda del cliente, domicilio de envío y agente propuestos (D-146, D-149, D-153) |
| L2-T026, L2-T030 a L2-T032 | Chatter guardado y favoritos | Migración `F1_ChatterYFavoritos` |
| L2-T033 a L2-T035 | Datos de R1, guion de la revisión (con la medición de SC-001) y documentación | |

Si no cabe todo: primero se recorta la copia de grupos (US2, escenario 7) y después la ficha técnica pasa a F2. Se registra en "Exploración y cambios".

---

## 6. Decisiones de la fase que más afectan a L2

| Decisión | Qué dice |
| :--- | :--- |
| D-145 | Solo pedido libre; el pedido capturado en CONTPAQi no entra por sincronización |
| D-146 | Moneda y tipo de cambio en el maestro, propuestos con la moneda del cliente; precio unitario por línea en la captura |
| D-147 | Editar un pedido con firmas (también Autorizado) revoca la autorización, con aviso antes de guardar y registro en el chatter |
| D-148 | Grupos configurables; permisos en árbol Módulo › Documento o funcionalidad › Acción, asignados con dos paneles. Comercial y Cobranza leen el pedido; el Administrador lee, confirma, cancela y revoca |
| D-149, D-150 | Domicilios del cliente: uno fiscal y N de envío; el pedido elige uno de envío. Sincronización por lectura completa (no hay `modified_since`) |
| D-151 | Listas híbridas: un conjunto de hasta 5,000 filas se trae una vez y todo se resuelve en el navegador; arriba, el servidor |
| D-152 | Espejo de existencias en PolyConecta, pero **se construye en F2**, no ahora |
| D-153 | Agente del pedido = agente de CONTPAQi ligado al usuario de AC |
| D-154 | Rutas de documento por id, nunca por folio |
| D-155 | Rutas de la web con prefijo de módulo (`/ventas/pedidos/15`); la API sigue la misma división |
| D-156 | Lecturas de CONTPAQi confirmadas (afecta al bridge, no a L2) |

---

## 7. Decisiones técnicas que ya tomó L2 (respétalas)

1. **Identity solo para credenciales** (`IdentityUserContext`, sin roles de Identity); los grupos son del dominio.
2. **Infrastructure referencia el marco `Microsoft.AspNetCore.App`** (no es un paquete) para la cookie y `IHttpContextAccessor`.
3. **`CurrentUserDesdeCookie` no depende de `PolyDbContext`**: el interceptor de auditoría depende de `ICurrentUser` y se formaría un ciclo.
4. **Agregados con `OwnsMany`**: asignaciones del usuario, permisos del grupo, líneas y firmas del pedido.
5. **Acceso a datos desde Application** por un puerto genérico `IAlmacen<T>`; `Application` no referencia EF Core.
6. **Concurrencia del pedido**: el guardado compara la `rowVersion` que tenía el usuario aunque solo cambien líneas o firmas (`IAlmacen.ExigirVersion`); los índices únicos de la firma son el segundo seguro.
7. **Peso de la web**: el límite de la carga inicial es **89 kB y hoy está en 88.4 kB**. `provideHttpClient()` en la raíz lo rompe (94.6 kB). El `HttpClient`, los interceptores y la guardia de sesión se proveen en las rutas perezosas de cada módulo. Cualquier cosa nueva en la carga inicial lo rompe; `npm run verificar-build` lo comprueba.
8. **Pruebas con sesión**: las pruebas de integración que llaman rutas protegidas usan un cliente con sesión de `ApiDePrueba`. El callback del bridge (`/api/v1/plataforma/bridge/callbacks`) **no** exige sesión: se autentica con su firma.

---

## 8. Cómo correr y verificar

Requisitos: SDK de .NET 10, Docker (las pruebas de integración levantan SQL Server 2022 con Testcontainers) y Node 24.16.

```bash
dotnet build Polyconecta.slnx
dotnet test Polyconecta.slnx                 # hoy: 277 en total, 222 correctas, 0 fallidas, 55 omitidas
cd PolyConecta.Web
npm test                                     # hoy: 110 de 110
npm run build && npm run verificar-build     # hoy: 88.4 kB de 89
./run.sh                                      # desde la raíz: compila, prueba y levanta web (:9000) y API (:9020)
```

Las 55 omitidas son la suite de contrato y las de integración que necesitan un bridge: se omiten solas sin `BRIDGE_URL`. Para correrlas, levanta el bridge simulado **desde la carpeta `PolyConecta.Contpaq`** (si no, no lee su `appsettings.json`) con `BridgeConfig__Mode=Simulated` y `BridgeConfig__DashboardPort=9032`, y define `BRIDGE_URL=http://localhost:9032` y `BRIDGE_CALLBACK_SECRET` igual a `BridgeConfig__CallbackSecret`. El simulador ya trae el catálogo `1.1` (clientes con moneda y domicilios, agentes, `id_erp`, clasificación).

---

## 9. Riesgos y dudas abiertas

- **La web todavía no tiene inicio de sesión** (L2-T010 a L2-T012 pendientes) y la API ya exige sesión. Hasta entonces, las pantallas siguen sobre estado en memoria y no llaman a la API.
- **Presupuesto de 89 kB casi agotado** (ver §7, punto 7).
- **Guiones de escenario de Pedidos**: dejan de compararse contra el prototipo y se sustituyen por pruebas extremo a extremo contra la API (R-10). El auditor y los guiones ya traducen las rutas con prefijo de módulo (L2-T036).
- **Horas**: L2 tenía 47 h en el plan y el alcance creció (grupos configurables, ficha técnica, domicilios, agentes). Si no cabe, aplica el orden de recorte de §5.

---

## 10. Al terminar

1. Corre todo lo de §8 en verde y las pruebas `e2e/f1` de la web.
2. Borra este archivo en tu último commit.
3. Entrega un reporte con: rama y último commit; una tabla de tareas (hecha, pendiente o bloqueada, con motivo); los resultados de las pruebas; lo que registraste en "Exploración y cambios" y en `decisiones.md`; y riesgos o dudas para el usuario.
4. No integres en `003-pedidos-de-venta` ni en `main`: eso lo hace el coordinador o el usuario.
