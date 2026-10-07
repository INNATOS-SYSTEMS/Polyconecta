# Research: Contratos visuales y componentes (spec 011)

Decisiones técnicas para construir la spec. Las de alcance están en la spec (D-134 a D-136 y la sesión de aclaraciones del 7-oct). La prueba técnica (`prueba-tecnica/`) respalda R-01 a R-06.

---

## R-01 · Versiones y dependencias

**Decisión**: instalar en `PolyConecta.Web`, con versión exacta (CT-36):

| Paquete | Versión | Para qué |
| :--- | :--- | :--- |
| `@tanstack/angular-table` | 9.2.6 | Lógica de la tabla de lista |
| `@angular/cdk` | 22.2.2 | Drag & drop del kanban, overlay, diálogo y menú |
| `@spartan-ng/brain` | 1.6.1 | Combobox, calendario, popover y pestañas, sin estilos |
| `@lucide/angular` | 1.52.0 | Íconos |
| `write-excel-file` | 4.1.1 | Exportar a `.xlsx` en el navegador |

**Consecuencias**:
- npm instala Tailwind 4, `tw-animate-css` y `clsx` porque Spartan las declara obligatorias. **No se importan** en ningún CSS ni TS. Una prueba del build (P1) verifica que el CSS final no tiene reglas de Tailwind.
- Se agregan a la lista de dependencias aprobadas de la regla de autonomía 4 de la spec.

**Alternativas**: PrimeNG, descartado por su licencia con llave (D-135). SheetJS, descartado por dos vulnerabilidades conocidas (aclaración del 7-oct).

---

## R-02 · Tabla: TanStack 9 en modo servidor

**Decisión**: un componente `odoo-list` que envuelve `injectTable` con estas opciones fijas:

```ts
manualSorting: true, manualPagination: true, manualFiltering: true,
autoResetPageIndex: false, autoResetExpanded: false,
sortDescFirst: false, enableSubRowSelection: false,
```

- El estado (orden, página, columnas visibles, orden de columnas, selección y grupos abiertos) vive en signals del componente. De ahí sale una `ConsultaLista` (data-model §1); cada cambio pide una vista nueva al `OrigenDeLista`.
- **Agrupar:** la agrupación de TanStack es del navegador y no sirve en modo servidor. Los grupos que devuelve el origen se pintan como filas sintéticas con `getSubRows` y expansión; abrir un grupo pide sus filas con el filtro del grupo. Hay varios niveles: abrir un grupo de primer nivel con un segundo `agruparPor` devuelve subgrupos, no filas.
- **Guías:** TanStack 9 cambió su API respecto de la 8. Se usan las guías del paquete (`node_modules/@tanstack/*/skills/`), no ejemplos de internet.

**Por qué**: es la única forma de que el mismo componente funcione con el origen en memoria ahora y con el HTTP en F1 (aclaración del 7-oct).

**Alternativas**: modo navegador ahora y servidor en F1, descartado en la aclaración.

---

## R-03 · Kanban con Angular CDK

**Decisión**: un componente `odoo-kanban` con `CdkDropListGroup` y una `CdkDropList` por etapa.
- Recibe la colección, las etapas y el mapa de transiciones (data-model §3). Lee del mismo `OrigenDeLista` que la lista, una consulta por etapa.
- Al soltar busca la transición; si no existe, la tarjeta no se mueve y el motivo se muestra en la columna.
- Si la transición pide datos, abre su diálogo con el `Dialog` del CDK y la tarjeta solo se mueve si se confirma.
- La transición la ejecuta el mismo método del servicio de estado que usa el botón del formulario, así que no se duplica ninguna regla.
- Para documentos sin estado, como las incidencias, el kanban agrupa por un campo y no se puede arrastrar.

**Por qué**: la prueba técnica pasó los tres casos (válida, inválida y con diálogo).

---

## R-04 · Campos con Spartan `brain`

**Decisión**:
- **Selección de registro (`odoo-many2one`):**
  - se arma con `BrnCombobox` y `BrnPopover` como directivas del mismo elemento, igual que el `helm` de Spartan;
  - las directivas se importan **una por una**, porque `BrnComboboxImports` trae `BrnCombobox` y `BrnComboboxMultiple` con el mismo selector;
  - "Buscar más…" abre un diálogo con la lista completa (`odoo-list` en modo selección).
- **Fecha (`odoo-date`):** `BrnCalendar` dentro de `BrnPopover`, con `provideNativeDateAdapter` y `provideBrnCalendarI18n` en español. La configuración incluye `labelPrevious`, `labelNext` y `labelWeekday`, porque por omisión vienen en inglés. La semana empieza en lunes. El encabezado del mes lo pinta el componente.
- **Pestañas (`odoo-tabs`):** `BrnTabs`.
- **Estilos que pone la aplicación:** `brain` no trae ninguno. Las opciones filtradas (`[data-hidden]`), el aviso "Sin resultados" (`[data-empty]`), los estados del calendario (`data-selected`, `data-today`, `data-outside`) y el foco se estilizan en `src/styles/app.css`, con las clases `o_*`.

---

## R-05 · Número, moneda y cantidad con unidad

**Decisión**: `odoo-number`, propio, con `Intl.NumberFormat('es-MX')`.
- Muestra el valor con formato; al enfocar lo deja editable sin separadores.
- La moneda se muestra con su código ISO.
- La cantidad lleva su unidad base al lado, sin editarla (D-127).

**Por qué**: no hay primitiva en `brain` para esto, y `Intl` cubre el formato sin dependencias.

---

## R-06 · Exportar a Excel

**Decisión**:
- Se importa `write-excel-file/browser` **dinámicamente** al pulsar "Exportar", para no cargarlo con la lista. Se usa `toFile('<lista>-<fecha>.xlsx')`.
- La exportación pide al origen todas las filas del filtro actual, o solo las seleccionadas, y escribe las columnas visibles en su orden, con los números como números.

**Por qué**: en la prueba técnica la página con todas las librerías pesa 87 kB comprimida; la librería de Excel no debe sumarse a cada lista.

---

## R-07 · Íconos: de Bootstrap Icons a Lucide

**Decisión**:
- Un componente `odoo-icon` recibe un nombre del catálogo de PolyConecta y pinta el ícono Lucide, con trazo 2 y tamaño por contexto: 16 px en botones, 18 px en botones de ícono y 15 px en botones inteligentes.
- La tabla de equivalencias vive en el componente. Hay 45 íconos de Bootstrap en 34 archivos; cada parte (P1 a P8) migra los de sus pantallas.
- Al terminar la P8 se quita del `index.html` la hoja de Bootstrap Icons, que viene del CDN.

**Por qué**: un catálogo propio evita que cada pantalla elija ícono por su cuenta y deja el cambio de set en un solo lugar.

---

## R-08 · Favoritos

**Decisión**:
- Interfaz `AlmacenDeFavoritos` (data-model §2). En esta spec la implementa `FavoritosEnNavegador`, sobre `localStorage`, con la llave `polyconecta.favoritos.<lista>`. Cada lectura y escritura va en `try/catch`.
- Si el navegador no permite guardar, la lista funciona igual y "Guardar favorito" avisa que no se pudo.
- En F1, `FavoritosEnServidor` guarda en la base por usuario (`SavedSearch` de 04 §3) y lo del navegador no se migra.

---

## R-09 · Paridad y pruebas

**Decisión**:
- Se borran `npm run parity`, `e2e/parity/parity.spec.ts`, `e2e/parity/informe.ts` y el `globalTeardown` que genera su informe.
- `e2e/parity/rutas.ts` se mueve a `e2e/soporte/`, porque lo usa la auditoría (`e2e/auditoria/botones.spec.ts`).
- `e2e/parity/navigation.spec.ts` (enlace directo, folio inexistente, atrás y adelante) se mueve a `e2e/auditoria/` y pasa a correr con `npm run audit`. Hoy solo corre con `npm run parity`.
- `npm run scenarios` y `npm run audit` siguen comparando textos, URL y flujos con el prototipo.
- Cada componente nuevo tiene pruebas unitarias (Vitest) y una prueba de Playwright en la galería.
- Las pruebas de la galería corren solo contra Angular, en `e2e/catalogo/`, y entran a `npm run scenarios`.

**Por qué**: el prototipo no tiene los componentes nuevos (kanban en 6 listas, combobox, calendario), así que no hay contra qué comparar píxeles.

---

## R-10 · Entrega por partes

**Decisión**:
- Cada parte es un PR de `011-contratos-visuales` a `main` (CT-44); después del merge, la rama trae `main` y sigue.
- **P1 integra la base y Pedidos.** Las pantallas que todavía no se migran siguen con los componentes viejos, que no se borran hasta que no quede ninguna pantalla que los use.
- El documento `07-contratos-visuales.md` crece con cada parte.

**Por qué**: P1 debe estar en `main` antes del 12-oct para que F1 diseñe sobre ella (aclaración del 7-oct).

**Riesgo**: mientras duran P2 a P8 conviven dos tablas y dos kanbans. Se acepta porque cada parte cierra un flujo completo.
