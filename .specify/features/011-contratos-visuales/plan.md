# Implementation Plan: Contratos visuales y componentes (fase abierta)

> **Secciones por líder (CT-34, D-120).** Esta spec es solo del camino 2: no toca el bridge ni CONTPAQi. La sección L1 queda vacía a propósito.

**Branch**: `011-contratos-visuales` | **Date**: 2026-10-07 | **Spec**: [spec.md](spec.md)

**Input**: la spec 011, aclarada el 7-oct, y su prueba técnica.

## Summary

Construir en `PolyConecta.Web` los componentes con contrato que pide la spec: lista con TanStack Table en modo servidor, kanban con CDK, campos con Spartan `brain`, íconos Lucide, estado de sincronización, diálogos, pestañas y menú de acciones. Los datos llegan por un `OrigenDeLista`, en memoria hasta F1. Con esos componentes se arma una galería viva en `/catalogo` y se migran las 28 pantallas flujo por flujo, sin cambiar su estructura. Se entrega en ocho partes; P1 (base, galería y Pedidos) llega a `main` antes del 12-oct.

## Technical Context

**Language/Version**: TypeScript 6.0, Angular 22.2 (D-68), Node 24.16 (D-69)

**Primary Dependencies**:
- nuevas: `@tanstack/angular-table` 9.2.6, `@angular/cdk` 22.2.2, `@spartan-ng/brain` 1.6.1, `@lucide/angular` 1.52.0 y `write-excel-file` 4.1.1 (research R-01);
- se quedan Bootstrap 5.3.2 y las clases `o_*`;
- Bootstrap Icons se retira al cerrar.

**Storage**: memoria de los servicios de estado (`core/state/`); favoritos en `localStorage` (research R-08). Sin API hasta F1.

**Testing**: Vitest (`npm test`) para reglas y componentes; Playwright para la galería (`e2e/catalogo/`), los escenarios y la auditoría contra el prototipo, y el tablero (`npm run tablero`).

**Target Platform**: navegadores de escritorio (Chromium, Edge y Firefox recientes), a 1366 px de ancho o más. La planta no usa terminales móviles en la Fase 1 (Principio VI).

**Project Type**: aplicación web (frontend Angular de una solución .NET).

**Performance Goals**:
- Una lista de 1,000 filas en memoria responde a ordenar, filtrar, agrupar o paginar en menos de 100 ms.
- La carga inicial crece como máximo 15 kB comprimidos respecto de hoy; la librería de Excel no entra en la carga inicial (research R-06).

**Constraints**:
- Sin cambiar la estructura de ninguna pantalla: los escenarios y la auditoría siguen en verde en cada parte.
- Sin librerías con licencia comercial ni vulnerabilidades conocidas.
- Sin reglas de Tailwind en el build.

**Scale/Scope**:
- 28 pantallas: 8 listas de documentos, 7 formularios, 7 hojas de "Nuevo", captura masiva, inicio e inventarios.
- 10 componentes nuevos, 12 existentes por documentar y conectar, y 45 íconos por migrar.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio o regla | Cómo lo cumple | Estado |
| :--- | :--- | :---: |
| II · Outbox, sin escribir en `adm*` | No toca el ERP. El estado de sincronización solo se muestra | ✅ |
| VI · Alcance de Fase 1 | Solo web de escritorio, sin handheld | ✅ |
| VII · Respaldo técnico | Las librerías se probaron (prueba técnica, 10/10) y sus versiones y licencias se leyeron de los paquetes | ✅ |
| IX · Odoo 19 | Lista, kanban, formulario, pipeline, smart buttons y chatter siguen sus patrones; el kanban mueve etapas con transiciones | ✅ |
| X · Documentos libres | "Nuevo" con la estructura completa y las mismas reglas (D-136) | ✅ |
| CT-09 · Carpetas por módulo | Componentes en `shared/`, tipos en `core/lista` y `core/kanban`, galería en `features/catalogo` | ✅ |
| CT-24 · Sistema de diseño | Se amplía con `07-contratos-visuales.md` | ✅ |
| CT-36 · Versiones exactas | Las cinco librerías se fijan exactas | ✅ |
| CT-44 · Ramas | Cada parte es un PR de la rama de la spec a `main` | ✅ |
| Una spec por fase | Fase abierta registrada en D-134, como la 001 (D-118) | ✅ |

Revisada después del diseño: sin cambios. No hay violaciones que justificar.

## Project Structure

### Documentation (this feature)

```text
.specify/features/011-contratos-visuales/
├── spec.md
├── plan.md               # este archivo
├── research.md           # R-01 a R-10
├── data-model.md         # tipos de lista, favoritos y kanban
├── contracts/
│   └── componentes.md    # entradas y salidas de cada componente
├── quickstart.md         # cómo verificar cada parte
├── prueba-tecnica/       # código de referencia de la prueba del 7-oct
└── tasks.md
```

### Source Code

```text
PolyConecta.Web/
├── src/app/
│   ├── core/
│   │   ├── lista/              # OrigenDeLista, OrigenEnMemoria, favoritos (nuevo)
│   │   ├── kanban/             # EtapaKanban, TransicionKanban y mapas por documento (nuevo)
│   │   └── state/              # sin cambios de reglas
│   ├── shared/
│   │   ├── odoo-list/          # nuevo (TanStack)
│   │   ├── odoo-kanban/        # nuevo (CDK)
│   │   ├── odoo-many2one/      # nuevo (Spartan combobox)
│   │   ├── odoo-date/          # nuevo (Spartan calendar)
│   │   ├── odoo-number/        # nuevo
│   │   ├── odoo-tabs/          # nuevo (Spartan tabs)
│   │   ├── odoo-action-menu/   # nuevo (CDK menu)
│   │   ├── odoo-dialog/        # nuevo (CDK dialog)
│   │   ├── odoo-sync-status/   # nuevo (CT-15)
│   │   ├── odoo-icon/          # nuevo (Lucide)
│   │   ├── odoo-search-panel/, odoo-pager/, odoo-view-switcher/   # se conectan a la consulta
│   │   └── hoja-nueva/, boton-nuevo/                              # estructura completa (D-136)
│   ├── features/
│   │   ├── catalogo/           # galería /catalogo (nuevo)
│   │   └── ventas/, produccion/, logistica/, calidad/, inventario/, plataforma/   # se migran por parte
│   └── styles/app.css          # estilos de los componentes con variables (E3)
└── e2e/
    ├── catalogo/               # pruebas de la galería (nuevo)
    ├── soporte/rutas.ts        # movido desde parity/
    ├── auditoria/navigation.spec.ts   # movido desde parity/
    └── parity/                 # se borra
docs/diseno/07-contratos-visuales.md   # nuevo
```

**Structure Decision**: todo en `PolyConecta.Web`, la única capa que cambia, siguiendo la estructura que dejó la spec 001. Fuera de la web solo cambian `docs/diseno/` (07, 05 §7 y CT-24) y `docs/ROADMAP.md`.

## L1 · Camino 1

Sin tareas: la spec no toca el bridge ni CONTPAQi.

## L2 · Camino 2

**Orden de construcción**:
1. **Base (P1).** Dependencias, `core/lista`, `core/kanban`, componentes, estilos e íconos. Cada componente entra a la galería con sus pruebas antes de usarse en una pantalla.
2. **Pedidos (P1).** Lista, kanban, formulario y "Nuevo" con los componentes nuevos. Es el flujo que F1 rediseña, así que va primero.
3. **Un flujo por parte (P2 a P8).** En el orden del proceso: Fabricación (con Incidencias y Captura masiva), Recolección, Calidad, Traslados, Recepción, Entregas e Inicio e inventarios. Cada parte confirma su mapa de transiciones contra los métodos reales del servicio de estado (data-model §3).
4. **Cierre (P8).** Se quitan los componentes viejos sin uso, Bootstrap Icons y la paridad; se terminan `07-contratos-visuales.md` y la integración en `docs/diseno/`.

**Cómo se mantiene la estructura**:
- Una pantalla migrada conserva las mismas regiones, en el mismo orden, con los mismos textos. Los escenarios y la auditoría lo comprueban contra el prototipo.
- Lo que la spec cambia a propósito queda anotado en `05 §7.5` como diferencia legítima: los botones (E3), los íconos, el kanban en 6 listas nuevas y el "Nuevo" completo.

**Riesgos**:
- **Escenarios que comparan texto:** pueden romperse si un componente nuevo cambia un texto visible, como el del paginador, que hoy dice "1-N / N" porque no pagina. Se resuelve igualando el texto del prototipo; si no se puede, se anota como diferencia legítima.
- **Dos componentes conviviendo:** mientras duran P2 a P8 hay dos tablas y dos kanbans. Se acepta (research R-10).
- **P1 contra el reloj:** el plazo es el 12-oct. Si no alcanza, P1 se parte: primero la base y la galería, y Pedidos en un segundo PR antes de que F1 diseñe su primera pantalla.

## Complexity Tracking

Sin violaciones.
