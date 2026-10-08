# Tasks: Contratos visuales y componentes (fase abierta)

> **Secciones por líder (CT-34, D-120).** Todas las tareas son del camino 2 (L2, Luis Alvarado Martinez): la spec no toca el bridge. Los ids llevan la sección (`L2-T001`). Las tareas que nacen de la exploración se agregan con la referencia a su fila en "Exploración y cambios" de `spec.md` (CT-43).

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/componentes.md](contracts/componentes.md), [quickstart.md](quickstart.md) y el código de [prueba-tecnica/](prueba-tecnica/README.md).

**Tests**: la spec los pide (criterios de cierre). Cada componente tiene pruebas unitarias y de la galería; cada parte deja en verde `npm run scenarios` y `npm run audit`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes).
- **[Story]**: historia de usuario. Las partes de entrega de la spec se cubren así:

  | Historia | Parte | Contenido |
  | :--- | :---: | :--- |
  | US1 | P1 | Base, componentes y galería `/catalogo` |
  | US2 | P1 | Pedidos migrado |
  | US3 | P2 | Fabricación, Incidencias y Captura masiva |
  | US4 | P3 | Recolección |
  | US5 | P4 | Calidad |
  | US6 | P5 | Traslados |
  | US7 | P6 | Recepción |
  | US8 | P7 | Entregas |
  | US9 | P8 | Inicio, inventarios y cierre |

- Las rutas son relativas a `PolyConecta.Web/` salvo que digan otra cosa.

---

## Phase 1: Setup (P1)

- [x] L2-T001 Instalar con versión exacta `@tanstack/angular-table@9.2.6`, `@angular/cdk@22.2.2`, `@spartan-ng/brain@1.6.1`, `@lucide/angular@1.52.0` y `write-excel-file@4.1.1` (research R-01) en `package.json`. Verificar que `npm audit` no reporte vulnerabilidades, y anotar en la exploración que Tailwind, `tw-animate-css` y `clsx` quedan en `package-lock.json` como dependencias de Spartan.
- [x] L2-T002 [P] Crear `scripts/verificar-build.mjs` y su script `npm run verificar-build`: falla si el CSS de `dist/` contiene reglas de Tailwind (`--tw-`) o si la carga inicial pasa de lo permitido en el plan (Performance Goals). Agregarlo al trabajo `web` de `.github/workflows/ci.yml`, después de `npm run build` (quickstart §1).
- [x] L2-T003 [P] Mover `e2e/parity/rutas.ts` a `e2e/soporte/rutas.ts` y `e2e/parity/navigation.spec.ts` a `e2e/auditoria/navigation.spec.ts`; actualizar sus imports y los de `e2e/auditoria/botones.spec.ts`. Verificar que `npm run audit` corre la navegación (research R-09).
- [x] L2-T004 Borrar `npm run parity`, `e2e/parity/` y el `globalTeardown` de `e2e/playwright.config.ts`. Actualizar la tabla de scripts de `README.md` y `docs/diseno/05-arquitectura-tecnica.md` §7.5: la paridad de píxeles se retiró (D-135).

---

## Phase 2: Foundational (P1, bloquea todo lo demás)

- [x] L2-T005 Crear `src/app/core/lista/origen.ts` con `ConsultaLista`, `FiltroLista`, `ResultadoLista`, `GrupoLista` y `OrigenDeLista` (data-model §1).
- [x] L2-T006 Crear `src/app/core/lista/origen-en-memoria.ts`: filtros con operadores (`contiene`, `igual`, `entre`, `en`), búsqueda, orden por varios campos, agrupación de varios niveles con totales, ruta de grupo, `ids` y paginación. Partir de `prueba-tecnica/origen.ts`.
- [x] L2-T007 [P] Pruebas en `src/app/core/lista/origen-en-memoria.spec.ts`: cada operador, el orden estable con empates, dos niveles de agrupación, los totales por grupo y del filtro, la paginación de grupos, `ids`, y 1,000 filas en menos de 100 ms (plan, Performance Goals).
- [x] L2-T008 [P] Crear `src/app/core/lista/favoritos.ts` con `Favorito`, `AlmacenDeFavoritos` y `FavoritosEnNavegador`, con la llave `polyconecta.favoritos.<lista>`, todo en `try/catch` (research R-08), y sus pruebas en `favoritos.spec.ts`: guardar, listar, borrar, un solo favorito por omisión y `localStorage` que falla.
- [x] L2-T009 [P] Crear `src/app/core/kanban/kanban.ts` con `EtapaKanban` y `TransicionKanban` (data-model §3).
- [x] L2-T010 [P] Crear `src/app/shared/odoo-icon/` (`pc-odoo-icon`) con el catálogo de nombres de PolyConecta, la equivalencia de los 45 íconos de Bootstrap a Lucide y los tamaños por contexto (research R-07), con sus pruebas.
- [x] L2-T011 [P] Agregar a `src/styles/app.css` la base de los componentes nuevos, con las variables de E3 y las clases `o_*`: `o_btn_icon`; las opciones filtradas (`[data-hidden]`) y el aviso "Sin resultados" (`[data-empty]`) del combobox; los días del calendario (`data-selected`, `data-today`, `data-outside`); las columnas y tarjetas del kanban; el foco (research R-04); y el estado cargando de los botones (`o_btn_loading`: deshabilitado y con el ícono girando, E3).

**Checkpoint**: tipos, origen y favoritos probados. Los componentes se pueden construir en paralelo.

---

## Phase 3: US1 · Componentes y galería (P1)

**Goal**: cada componente del contrato existe, está probado y se ve en `/catalogo` en todos sus estados.

**Independent Test**: quickstart §1, sin tocar ninguna pantalla de negocio.

- [x] L2-T012 [US1] Crear la ruta `/catalogo` en `src/app/features/catalogo/` (`catalogo.routes.ts` y una página con índice por componente) y registrarla en `app.routes.ts`. Los datos de ejemplo van en `features/catalogo/datos.ts`, aparte de la semilla.
- [x] L2-T013 [US1] Crear `src/app/shared/odoo-list/` (`pc-odoo-list`) sobre TanStack Table en modo servidor, con las opciones de research R-02, partiendo de `prueba-tecnica/tabla-prueba.ts`. Debe ordenar, paginar, elegir filas por página, filtrar, agrupar en varios niveles con subtotales, abrir grupos bajo pedido, ocultar y reordenar columnas, seleccionar y lanzar acciones masivas. Tipos de columna: texto, número, moneda, fecha y estado (contrato).
- [x] L2-T014 [US1] Exportar a Excel en `pc-odoo-list`, con `write-excel-file/browser` importado dinámicamente: las seleccionadas o todo el filtro, con las columnas visibles en su orden y los números como números (research R-06).
- [x] L2-T015 [US1] Conectar `pc-odoo-search-panel`, `pc-odoo-pager` y `pc-odoo-view-switcher` a la `ConsultaLista` de la lista, conservando su aspecto: filtros y agrupaciones del panel, cambio de vista y el texto del paginador ("inicio-fin / total"; hoy muestra "1-N / N" porque no pagina). Con la semilla actual, ninguna lista pasa de 80 filas, así que el texto no cambia.
- [x] L2-T016 [US1] Favoritos en el panel de búsqueda: guardar la vista actual con nombre, marcar uno por omisión, aplicarlo y borrarlo, con `FavoritosEnNavegador`. Si guardar falla, avisar.
- [x] L2-T017 [P] [US1] Crear `src/app/shared/odoo-kanban/` (`pc-odoo-kanban`) con CDK: columnas por etapa con contador, etapas plegadas, tarjetas por plantilla, carga por etapa desde el origen, arrastre con transición validada, regreso con motivo y diálogo de la transición (research R-03). Partir de `prueba-tecnica/kanban-prueba.ts`.
- [x] L2-T018 [P] [US1] Crear `src/app/shared/odoo-dialog/` (`pc-odoo-dialog`) sobre el `Dialog` del CDK: título, cuerpo y botones primario y secundario; Esc y clic fuera cancelan; el foco queda atrapado dentro. Incluye el servicio de **aviso flotante** (`AvisosService`, sobre el overlay del CDK): éxito, aviso y error, que se cierran solos a los 4 s salvo el de error.
- [x] L2-T019 [P] [US1] Crear `src/app/shared/odoo-many2one/` (`pc-odoo-many2one`) con las directivas de `BrnCombobox` importadas una por una: busca en el origen, muestra hasta 8 opciones con "Buscar más…" (diálogo con `pc-odoo-list` en selección única) y "Sin resultados", se maneja con teclado e implementa `ControlValueAccessor` (research R-04).
- [x] L2-T020 [P] [US1] Crear `src/app/shared/odoo-date/` (`pc-odoo-date`) con `BrnCalendar` y `BrnPopover`, `provideNativeDateAdapter` y la configuración en español completa (incluidas las etiquetas de accesibilidad), semana desde el lunes, `min` y `max`, e implementando `ControlValueAccessor`. Partir de `prueba-tecnica/campos-prueba.ts`.
- [x] L2-T021 [P] [US1] Crear `src/app/shared/odoo-number/` (`pc-odoo-number`): cantidad con unidad base no editable (D-127), moneda ISO y porcentaje, con `Intl.NumberFormat('es-MX')`. Muestra el valor con formato y lo edita sin separadores (research R-05).
- [x] L2-T022 [P] [US1] Crear `src/app/shared/odoo-tabs/` (`pc-odoo-tabs`) sobre `BrnTabs` y `src/app/shared/odoo-action-menu/` (`pc-odoo-action-menu`) sobre el `Menu` del CDK, con teclado.
- [x] L2-T023 [P] [US1] Crear `src/app/shared/odoo-sync-status/` (`pc-odoo-sync-status`) con los cinco estados de CT-15, el folio o el error de CONTPAQi y "Reintentar" solo si `puedeReintentar` (D-93). Ninguna pantalla lo usa todavía.
- [x] L2-T024 [P] [US1] Pruebas unitarias de cada componente (`*.spec.ts` junto a cada uno): entradas, salidas, estados y teclado, según el contrato.
- [x] L2-T025 [US1] Poner en `/catalogo` cada componente, los 10 nuevos y los existentes (`odoo-topbar`, `main-layout`, `odoo-breadcrumb`, `odoo-search-panel`, `odoo-view-switcher`, `odoo-pager`, `odoo-smart-buttons`, `odoo-status-pipeline`, `odoo-line-capture`, `odoo-chatter-drawer`, `boton-nuevo`, `hoja-nueva`, `lot-picker-modal`, `lot-quantity-picker-modal`, `pagina-no-encontrada` y `pagina-pendiente`), con todos sus estados: vacío, cargando, solo lectura, deshabilitado, error, sin conexión y documento libre, según aplique. Los botones primario, secundario y de ícono van con los seis estados de E3.
- [x] L2-T026 [US1] Pruebas de Playwright de la galería en `e2e/catalogo/` (con su `playwright.config.ts`, solo Angular), a partir de `prueba-tecnica/prueba.spec.ts`: un caso por comportamiento del contrato y la regla "la tabla nunca procesa en el navegador" (cuenta de consultas).
- [x] L2-T027 [US1] Escribir en `docs/diseno/07-contratos-visuales.md` los contratos de los patrones de pantalla (lista, kanban, formulario de documento, "Nuevo", pestañas, detalle y su captura, campos del maestro, avisos y bloqueos, y estado de sincronización), con capturas del tablero, y el contrato de cada componente de L2-T025 (anatomía con captura de la galería, entradas, salidas, estados, comportamientos, reglas, implementación y dónde se usa), y apuntar CT-24 a ese documento.
- [x] L2-T028 [US1] Agregar `/catalogo` a `e2e/tablero/pantallas.ts`, correr `npm run tablero` y subir la galería al lienzo.

**Checkpoint**: quickstart §1 en verde. Con esto, F1 ya puede diseñar componiendo contratos.

---

## Phase 4: US2 · Pedidos (P1)

**Goal**: la lista, el kanban, el formulario y el "Nuevo" de Pedidos usan los componentes nuevos, sin cambiar su estructura.

**Independent Test**: quickstart §2 sobre `/pedidos`, `/pedidos/IV310-26` y `/pedidos/nuevo`.

- [x] L2-T029 [US2] Crear el origen de Pedidos en `src/app/features/ventas/pedidos-origen.ts`: `OrigenEnMemoria` sobre la colección de pedidos de `OperationalFlowState`, con las columnas de la lista actual y el total como sumable.
- [x] L2-T030 [US2] Migrar `features/ventas/pedidos-list/` a `pc-odoo-list` y `pc-odoo-kanban`, con las mismas columnas, el mismo orden y los mismos textos. El kanban usa las etapas y transiciones de Pedidos (data-model §3), con el diálogo de firma al autorizar, que llama al mismo `autorizar()` del formulario.
- [x] L2-T031 [US2] Migrar `features/ventas/pedido-form/`:
  - pestañas a `pc-odoo-tabs`;
  - cantidades y precios a `pc-odoo-number`;
  - fechas a `pc-odoo-date`;
  - acciones secundarias a `pc-odoo-action-menu`;
  - íconos a `pc-odoo-icon`;
  - captura de líneas con `pc-odoo-many2one` para el producto.
- [x] L2-T032 [US2] Rehacer `features/ventas/pedido-nuevo/` con la estructura completa de D-136: etapas en Borrador, cliente con `pc-odoo-many2one`, la pestaña Detalle con captura de líneas (cantidad, unidad base, precio y moneda) y el chatter visible, que se activa al guardar. Un solo "Guardar" crea el pedido con sus líneas. Ajustar `shared/hoja-nueva/` para que sirva a los demás documentos.
- [x] L2-T033 [P] [US2] Pruebas: guion nuevo en `e2e/scenarios/` para el "Nuevo" completo (maestro y líneas en un guardado, y chatter activo después); pruebas unitarias de las reglas que cambian en `core/state/libre/pedido-libre.spec.ts`.
- [x] L2-T034 [US2] Correr quickstart §2 para Pedidos: escenarios, auditoría y tablero en verde, y sin íconos `bi-` en `features/ventas/`. Anotar en `05 §7.5` las diferencias legítimas nuevas.
- [ ] L2-T035 [US2] Abrir el PR de P1 (US1 y US2) a `main` y, con la CI en verde, pedir la aprobación del merge (CT-44). Después del merge, traer `main` a la rama.

**Checkpoint**: P1 en `main` antes del 12-oct.

---

## Phase 5: US3 · Fabricación (P2)

**Goal**: Fabricación (lista, kanban, las tres OF y "Nuevo"), Incidencias (lista y kanban por centro de trabajo) y Captura masiva (campos e íconos) migrados.

**Independent Test**: quickstart §2 sobre las 7 pantallas de la franja Fabricación.

- [x] L2-T036 [US3] Escribir las transiciones de la OF en `src/app/features/produccion/fabricacion-acciones.ts` (aclaración P2): Borrador → Planeado (Confirmar, con componentes) y En progreso → Hecho (Cerrar producción, con el motivo del hard-stop de Calidad o del saldo de WIP). Planeado → En progreso no se arrastra. El formulario y el kanban usan las mismas acciones, como `PedidosAcciones`.
- [x] L2-T037 [US3] Migrar `features/produccion/fabricacion-list/` a `pc-odoo-list` y `pc-odoo-kanban`.
- [x] L2-T038 [US3] Migrar `features/produccion/fabricacion-form/` (pestañas, acciones e íconos) y rehacer `fabricacion-nueva/` con la estructura completa (D-136): Componentes y Subproductos se capturan antes de guardar y se guardan con el maestro; Producción y Planeación se ven deshabilitadas hasta guardar.
- [x] L2-T039 [P] [US3] Migrar `features/produccion/incidencias/` a `pc-odoo-list` y agregar el kanban por centro de trabajo, sin arrastre; la captura en renglón se conserva (P-27). Exporta desde la barra de selección; su "Importar" sigue deshabilitado.
- [x] L2-T040 [P] [US3] Migrar los íconos de `features/produccion/captura-masiva/`, sin kanban. Es una tabla de solo lectura (no captura pesos); "Importar" sigue deshabilitado (fuera de la spec).
- [ ] L2-T041 [US3] Quickstart §2 para Fabricación, la nota en `05 §7.5`, el contrato de uso en `07` y el PR de P2.

---

## Phase 6: US4 · Recolección (P3)

- [x] L2-T042 [US4] Confirmar las transiciones de la recolección contra `StockOperationState` (`validar`, `cancelar`, el backorder) y escribirlas en `src/app/features/logistica/recoleccion-kanban.ts`. El diálogo de "Validar" con cantidades parciales usa `pc-odoo-dialog`.
- [x] L2-T043 [US4] Migrar `features/logistica/recolecciones/` (lista, kanban nuevo y formulario) y rehacer `recoleccion-nueva/` con la estructura completa (D-136). La selección de lotes usa `lot-picker-modal` y `lot-quantity-picker-modal` sobre `pc-odoo-dialog` y `pc-odoo-list`.
- [ ] L2-T044 [US4] Quickstart §2 para Recolección, incluidos los guiones de recolección parcial con backorder y de lote inexistente, y el PR de P3.

---

## Phase 7: US5 · Calidad (P4)

- [x] L2-T045 [US5] Confirmar las transiciones de Calidad (aprobar o rechazar el lote, con el diálogo del resultado) contra `OperationalFlowState` y `calidad-libre.ts`; escribirlas en `src/app/features/calidad/calidad-kanban.ts`.
- [x] L2-T046 [US5] Migrar `features/calidad/calidad-list/` (lista y kanban nuevo) y `calidad-form/`, y rehacer `calidad-nuevo/` y `calidad-libre-form/` con la estructura completa (D-136). El hard-stop se muestra con `pc-odoo-dialog`.
- [ ] L2-T047 [US5] Quickstart §2 para Calidad, incluido el guion del hard-stop, y el PR de P4.

---

## Phase 8: US6 a US8 · Traslados, Recepción y Entregas (P5 a P7)

Las tres comparten `features/logistica/documento/`. La primera parte adapta el componente común y las otras dos lo configuran.

- [x] L2-T048 [US6] Migrar `features/logistica/documento/` (`logistica-list`, el formulario y `logistica-nuevo`) a los componentes nuevos, con la estructura completa en "Nuevo" (D-136), y escribir las transiciones de Traslados en `src/app/features/logistica/traslados-kanban.ts`.
- [ ] L2-T049 [US6] Quickstart §2 para Traslados y el PR de P5.
- [ ] L2-T050 [US7] Escribir las transiciones de Recepción en `src/app/features/logistica/recepcion-kanban.ts`, con la regla de D-56 (solo lotes en `TRANS/*`) visible en el diálogo de validar.
- [ ] L2-T051 [US7] Quickstart §2 para Recepción y el PR de P6.
- [ ] L2-T052 [US8] Escribir las transiciones de Entregas en `src/app/features/logistica/entregas-kanban.ts`, con el hard-stop de Calidad visible.
- [ ] L2-T053 [US8] Quickstart §2 para Entregas y el PR de P7.

---

## Phase 9: US9 · Inicio, inventarios y cierre (P8)

- [ ] L2-T054 [US9] Migrar `features/plataforma/dashboard/` y `features/inventario/inventario-actual/` (las dos rutas) a `pc-odoo-list` y `pc-odoo-icon`.
- [ ] L2-T055 [US9] Borrar los componentes y estilos viejos que ya no usa ninguna pantalla, y quitar Bootstrap Icons de `src/index.html`. Verificar que `grep -rn 'bi-' src/app` esté vacío.
- [ ] L2-T056 [US9] Quickstart §3 completo: build, pruebas, escenarios, auditoría y galería en verde.
- [ ] L2-T057 [US9] Regenerar el lienzo con `npm run tablero` (28 pantallas y la galería) y subirlo.

---

## Phase 10: Polish y cierre de la spec

- [ ] L2-T058 Integrar en `docs/diseno/`:
  - `07-contratos-visuales.md` completo;
  - `05 §7` (sistema de diseño, componentes y verificación sin paridad de píxeles);
  - `06` (tabla de stack y CT-24);
  - `ROADMAP.md` con la fecha y el commit (CT-35).
- [ ] L2-T059 Cerrar la spec: borrar `.specify/features/011-contratos-visuales/` en la rama, abrir el PR de P8 y, con la CI en verde, pedir la aprobación del merge (CT-44).

---

## Dependencies & Execution Order

- **Setup (L2-T001 a T004)** primero. L2-T002 y L2-T003 en paralelo después de L2-T001.
- **Foundational (L2-T005 a T011)** bloquea todo. L2-T005 → L2-T006 → L2-T007; los demás en paralelo.
- **US1** depende de Foundational:
  - L2-T013 → L2-T014, L2-T015 y L2-T016;
  - L2-T017 a L2-T023 en paralelo entre sí;
  - L2-T024 a L2-T028 al final.
- **US2** depende de US1. **P1 = US1 + US2**, un solo PR (L2-T035).
- **US3 a US9** dependen de P1 en `main` y van en ese orden, un PR por parte. US6 antes que US7 y US8, porque adapta el componente común de logística.
- **Polish** después de US9.

### Parallel opportunities (US1)

```text
L2-T017 kanban · L2-T018 diálogo · L2-T019 many2one · L2-T020 fecha · L2-T021 número · L2-T022 pestañas y menú · L2-T023 sincronización
```

Son carpetas distintas y solo dependen de Foundational. L2-T024 (sus pruebas) se reparte igual.

## Implementation Strategy

- **MVP = P1 (US1 + US2)**: la base, la galería y Pedidos en `main` antes del 12-oct. Es lo que F1 necesita para diseñar.
- **Si P1 no alcanza el 12-oct**: integrar US1 sola (base y galería), y US2 en un segundo PR antes de que F1 diseñe la pantalla del pedido (plan, Riesgos).
- **Después, un flujo por PR** (US3 a US9), cada uno antes de la fase que lo usa (spec, Entregas por partes).
