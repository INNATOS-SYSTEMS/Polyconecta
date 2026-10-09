# Handoff · F1 (spec 003) tras integrar L2 · 9-oct

Para retomar en una sesión nueva. Se retira al cerrar la spec (C-T008), como `handoff-l1.md` y `handoff-l2.md`.

## Dónde está

- Rama `003-pedidos-de-venta` (checkout principal), commit de integración `4a5c92d` "Integra el camino L2 de F1". Tiene 48 commits sin push; el usuario hace push y merge en GitHub.
- L1 integrado antes (`6cc61c5`, `34c495e`). L2 integrado completo (L2-T001 a L2-T036 marcadas).
- Worktrees aún presentes: `.claude/worktrees/agent-a9fcca3e254117a41` (rama `003-pedidos-de-venta-l2`, ya integrada) y `.claude/worktrees/agent-a1428a14b041ac060` (rama `003-pedidos-de-venta-l1`, ya integrada). Se pueden quitar con `git worktree remove` cuando el usuario lo apruebe.
- Puede seguir corriendo `./run.sh --with-bridge` lanzado desde el worktree de L2 (puertos 9000, 9020, 9030). Para detenerlo: `pkill -f scripts/run.sh; pkill -f PolyConecta.Api.csproj; pkill -f PolyConecta.Contpaq.csproj; pkill -f "ng serve"`. El contenedor `polyconecta-sql` (14333) es compartido y se deja arriba.

## Tras la limpieza (9-oct, tarde)

- Las ramas `003-pedidos-de-venta-l1` y `-l2` y sus worktrees se borraron (ya estaban integradas). El trabajo sigue en `003-pedidos-de-venta-limpieza`, que sale de `003-pedidos-de-venta` y se integra a ella.
- Las pantallas de F1 se rehicieron sobre la propuesta aprobada P-01 a P-17 (D-157; tablero https://claude.ai/artifact/Vt8o8ef4t9FuziZMKrvmyw, página "F1 · Propuesta"): Pedido con `pc-pedido-hoja`; Usuarios, Grupos, Productos, Clientes y Clasificaciones con `pc-hoja-registro`; Sincronización como lista; inicio de sesión sin componente propio. Edición en su lugar (D-164) y bitácora en catálogos (D-165).
- Ninguna pantalla nueva se implementa sin aprobar su propuesta en el tablero (D-157).

## Lo que falta de F1

| Tarea | Quién | Qué |
| :--- | :--- | :--- |
| L1-T005 | En el VPS | Publicar el bridge, un solo inicio del SDK, sonda una hora, tiempo límite forzado y reinicio diario |
| L1-T008 | En el VPS | Suite de contrato de lecturas contra el bridge real; medir la lectura completa de productos y clientes |
| L1-T009 | En el VPS | Cotejo de T-06 (F-05) y F-01/F-02 contra la UI de CONTPAQi |
| C-T007 | Cualquiera | Quickstart completo (§6 depende del VPS); resultado de cada sección en "Exploración y cambios" |
| C-T009 | Con la operación | Revisión R1 con `docs/revisiones/R1.md`; medir SC-001 y anotarlo en su tabla |
| C-T008 | Al final | Cerrar la spec: pasar a `docs/diseno/` lo de "Exploración y cambios", borrar la spec en su rama antes del PR |

## Cómo verificar (todo pasaba al integrar)

```bash
./run.sh --with-bridge          # compila, migra, 330 pruebas .NET (55 omitidas) y levanta todo
cd PolyConecta.Web
npm test                        # Vitest, 154
npm run build && npm run verificar-build   # carga inicial ≤ 89 kB (90 990 de 91 136 bytes)
npm run e2e                     # e2e/f1, 24 (pedido-real y chatter van contra la API real)
npm run scenarios               # 20, contra el prototipo Blazor (:9010)
npm run audit                   # 25
npm run chatter                 # 2
npx playwright test --config e2e/catalogo/playwright.config.ts   # 26
npx playwright test --config e2e/tablero/playwright.config.ts    # 28
```

Escenarios, auditor, tablero, galería, chatter y `npm run e2e` necesitan la API arriba: desde F1 toda ruta salvo `/login` exige sesión y entran con un usuario de R1 (`e2e/soporte/sesion-global.ts`, contraseña `LOCAL_R1_PASSWORD` de `.env.local`). Antes del tablero y de las pruebas reales hay que sincronizar catálogos (como `sistemas`) y tener pedidos (`scripts/dev/sembrar-pedidos.sh 30`).

## Cosas que no son obvias

- **Carga inicial al filo.** Con esbuild, el código de un paquete que también alcanza `main` entra a la carga inicial en cuanto una ruta perezosa lo usa (`@angular/common/http`, `CommonModule`, operadores de rxjs). Por eso la web no usa `HttpClient` ni `CommonModule` (pipes `currency`/`number` sustituidos por `importe()`/`n2()`), la sesión se carga desde `SesionAcciones` (no `SesionState`) y la guardia va en cada archivo de rutas con `conSesion()`, no en `app.routes`. Medir con bytes exactos, no con el kB redondeado.
- **Cliente de la API:** `core/sesion/api.ts` (`pedirApi`, `respuestaApi`, `ErrorApi`); el `401` lo maneja lo que registra `proveerClienteApi()`.
- **Listas HTTP:** la vista sale de `GET …/vista` (`OrigenHttp.vista()`); las pruebas simuladas necesitan `simularListas(page)` de `e2e/soporte/listas.ts` (vistas, favoritos y chatter vacíos) o caen en `/login`.
- **Pruebas de integración:** `ApiDePrueba` apaga la siembra de R1 aunque `run.sh` exporte `Seguridad__DatosR1__Contrasena`; si no, chocan los usuarios.
- **Base local compartida:** la del contenedor la usan el checkout principal y los worktrees; ya tiene `F1_ChatterYFavoritos`, los usuarios de R1 y pedidos de prueba. `ac1`, `comercial1` y `cobranza1` se crearon a mano antes del sembrador (nombres "Atención Clientes 1", "Comercial 1"…) y se les puso la contraseña de R1.
- **El pedido semilla `IV310-26` ya no existe** (Pedidos vive en la API, R-10): el botón "Pedido" de la OF en memoria está deshabilitado y el corredor de escenarios lo normaliza a «pedido».
- Todo lo decidido o corregido está en "Exploración y cambios" de `spec.md` (filas del 2026-10-09).

## Pendientes menores vistos, sin tarea

- `OdooList.cargar` no atrapa el error de una consulta fallida (queda un "Unhandled rejection" en consola tras un `401`).
- Hay 665 advertencias de compilación en .NET (sobre todo xUnit1051 y CA1848).
