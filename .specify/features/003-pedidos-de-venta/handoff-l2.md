# Handoff L2 · spec 003 (camino 2)

- **Rama**: `003-pedidos-de-venta-l2` (sale de `003-pedidos-de-venta` en `4ba929e`; el worktree venía en `main` y se reinició a esa rama antes de empezar).
- **Worktree**: `/Users/emilio/Development/Sandbox/Polyconecta/.claude/worktrees/agent-a9fcca3e254117a41`
- **Último commit**: el de este archivo (`WIP L2: handoff`), encima de `bee8ef5`.

## Tareas

| Tarea | Estado | Commit |
| :--- | :--- | :--- |
| L2-T001 paquete Identity 10.0.12 | Hecha `[x]` (restore verificado) | `0efa101` |
| L2-T002 dominio de seguridad (Plant, User, Group, Permission, Permisos.cs, GruposIniciales) | Hecha `[x]`; 10 pruebas nuevas en `tests/PolyConecta.Domain.Tests/Plataforma/SeguridadTests.cs`, dominio 32/32 | `4729589` |
| L2-T003 configuraciones plt, `CredencialUsuario`, `PolyDbContext : IdentityUserContext`, `SembradorSeguridad` | Código hecho y compila; **sin marcar**: falta verificar la siembra contra SQL Server, que necesita la migración de L2-T007 | `bee8ef5` |
| L2-T004 `ICurrentUser` ampliado | **En curso** (en el commit WIP) | WIP |
| L2-T005 a L2-T035 | Pendientes | — |

### Punto exacto de L2-T004

Hecho (sin compilar todavía):
- `Application/Common/Puertos.cs`: `ICurrentUser` con `UserName`, `UserId`, `NombreVisible`, `GrupoEjercido`, `EsSuplente`, `Role => GrupoEjercido` (miembro por omisión) y `EjercerGrupo(grupo, esSuplente)`.
- `Infrastructure/Common/Servicios.cs`: `SistemaCurrentUser` ya implementa lo nuevo y es `class` no sellada con miembros virtuales.
- `AuditoriaInterceptor` guarda `user.GrupoEjercido` en `StateTransitionLog.Role`.
- `Infrastructure/DependencyInjection.cs`: `AddHttpContextAccessor()` y `AddScoped<ICurrentUser, CurrentUserDesdeCookie>()`.

**Falta (la compilación está rota hasta hacerlo):**
1. Crear `Infrastructure/Plataforma/Identidad/CurrentUserDesdeCookie.cs`: hereda de `SistemaCurrentUser`, lee de `IHttpContextAccessor` los claims del usuario (id de dominio, usuario, nombre visible) que pondrá la cookie al entrar (L2-T008). **No debe depender de `PolyDbContext`**: el `AuditoriaInterceptor` depende de `ICurrentUser` y se crearía un ciclo. Sin `HttpContext` o sin sesión, se comporta como "sistema".
2. Actualizar `tests/Compartido/Entorno.cs` (`UsuarioFijo`) con los miembros nuevos; `BaseComunTests` espera `Role == "Planner"`, así que `UsuarioFijo` debe dejar el rol recibido como `GrupoEjercido` inicial.
3. `dotnet build Polyconecta.slnx` y `dotnet test`.

## Pruebas (último punto en que corrieron)

- Línea base antes de tocar nada: .NET 130 en total, 101 correctas, 29 omitidas (contrato e integración sin `BRIDGE_URL`), 0 fallidas.
- Después de L2-T002: `PolyConecta.Domain.Tests` 32/32.
- Después de L2-T003: `dotnet build` sin errores. No se corrió la suite completa después de L2-T003.
- Web: `npm run build` + `npm run verificar-build` en la línea base: **88.9 kB de 89 kB**.
- No hay procesos levantados (ni API, ni web, ni bridge en :9032). Docker solo tiene el contenedor `polyconecta-sql` de `run.sh`, que ya existía.

## Decisiones tomadas (todavía no registradas en "Exploración y cambios")

1. **HttpClient fuera de la carga inicial.** Agregar `provideHttpClient()` en `app.config.ts` sube la carga inicial a 94.6 kB (límite 89). Plan: envolver todas las rutas en un `loadChildren` perezoso cuyo route `providers` tenga `provideHttpClient(withFetch(), withInterceptors([...]))` y la guardia de sesión; los servicios HTTP se proveen en esa ruta (no `providedIn: 'root'`, porque el inyector raíz no tendría HttpClient). "Cerrar sesión" del systray (que está en el árbol raíz) navega a una ruta `/salir` dentro de las rutas perezosas. Hay que registrarlo en "Exploración y cambios" (camino L2).
2. **Infrastructure agrega `<FrameworkReference Include="Microsoft.AspNetCore.App" />`** (no es un paquete) para la cookie, `SignInManager` y `IHttpContextAccessor`. Está en el commit de L2-T001; conviene anotarlo en "Exploración y cambios".
3. **Agregados con `OwnsMany`**: las asignaciones del usuario y los permisos del grupo son colecciones de propiedad (EF las borra al quitarlas; el bucle de `PolyDbContext` deja Restrict solo en las llaves que no son de propiedad). El plan es usar lo mismo en líneas y firmas del pedido.
4. **Concurrencia del pedido**: el repositorio fijará `OriginalValue` de `RowVersion` con la que manda el cliente y marcará el maestro como modificado, para que dos firmas simultáneas den `DbUpdateConcurrencyException` → `409 DOCUMENTO_MODIFICADO`; los índices únicos de la firma son el segundo seguro.
5. **Acceso a datos desde Application**: `Application` no referencia EF Core. Plan: un puerto genérico `IAlmacen<T>` (por id, listar con `Expression`, agregar, exigir versión) implementado en Infrastructure, y la consulta de listas como servicio de Infrastructure sobre `VistaDeBusqueda<TEntidad, TFila>` declarada en Application con una proyección `Expression<Func<TEntidad, TFila>>` (las columnas son propiedades de la fila; se construyen expresiones dinámicas por nombre).
6. **Ruta web del pedido**: se queda `/pedidos/:id` (tasks.md dice cambiar `pedidos/:folio` por `pedidos/:id`), no `/ventas/pedidos/:id` como el ejemplo de D-154. Duda abierta abajo.
7. `ReglaDeNegocioException(codigo, razon)` nueva en `Domain/Common`, para traducir a `409` con `code` y `razon`.
8. Nombres de plantas sembradas: PIM "PIM (Apodaca)", SC "Santa Cruz", MTM "Montemorelos".

Nada registrado todavía en `spec.md` ni en `decisiones.md`.

## Siguiente paso para retomar

1. `git switch 003-pedidos-de-venta-l2` en el worktree.
2. Terminar L2-T004 (los tres puntos de arriba), compilar y correr `dotnet test --solution Polyconecta.slnx`.
3. Rehacer el commit: la tarea L2-T004 va en su propio commit (el WIP puede quedar o aplastarse en la rama propia, nunca en `main` ni en `003-pedidos-de-venta`).
4. Seguir con L2-T005 (decorador de autorización), L2-T006 (reglas de fila) y L2-T007 (migración `F1_Seguridad` con `dotnet ef migrations add F1_Seguridad --project PolyConecta.Infrastructure --startup-project PolyConecta.Api`), y verificar ahí la siembra de L2-T003 para marcarla.
5. Registrar en "Exploración y cambios" las decisiones 1 y 2.

## Bloqueos y dudas

- **Ruta web**: ¿`/pedidos/:id` (como dice tasks.md y el menú actual) o `/ventas/pedidos/:id` (ejemplo de D-154)?
- **Pruebas de integración existentes** (`BridgeCallbackTests`, `CicloCompletoTests`) van a recibir `401` cuando la API exija sesión por omisión (FR-009). Plan: `ApiDePrueba` siembra un usuario y da un cliente con sesión y `X-Requested-With`; no se debilitan, se adaptan (se explica en el commit de L2-T008).
- **Presupuesto de 89 kB**: queda 0.1 kB; cualquier cosa en la carga inicial lo rompe (ver decisión 1).
- Las pruebas contra el simulador con campos `1.1` (L2-T014, L2-T016) quedarán pendientes de correr hasta integrar la rama de L1.
