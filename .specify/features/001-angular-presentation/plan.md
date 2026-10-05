# Implementation Plan: Presentación en Angular

> **Una sola sección, sin L1 ni L2.** CT-34 pide una sección por líder porque las fases del plan tienen dos caminos. Esta spec no es una fase del plan (D-118) y todo su trabajo cae en carpetas del camino 2 (`PolyConecta.Web/` y `PolyConecta.Api/`), así que el plan no se parte.

**Branch**: `001-angular-presentation` | **Date**: 2026-10-05 | **Spec**: [spec.md](spec.md)

**Status**: Plan completo. Sigue `/speckit-tasks`.

---

## Summary

Replicar en Angular 22 el prototipo `PolyConecta.Presentation` (Blazor Server) con paridad visual y de comportamiento, habilitar "Nuevo" en 9 documentos y llevar el chatter en vivo a `PolyConecta.Api`. El enfoque es portar los cinco servicios de estado del prototipo a servicios con signals, conservando nombres, datos semilla y reglas. Las páginas se reconstruyen sobre los componentes que deja la tarea 0.5 de la spec 002. La paridad se prueba contra el prototipo corriendo en `:9000` con Playwright: capturas por ruta (≤ 1 % de píxeles) y guiones de texto por escenario.

La investigación de [research.md](research.md) cambió cuatro supuestos de la spec. Están en la sección "Exploración y cambios" de la spec:

1. El chatter del prototipo **no** está en vivo.
2. El pedido, el traslado, la recepción y la entrega del prototipo son **un solo documento** cada uno.
3. El estado de Blazor **se pierde** al recargar.
4. Las fases 0 y 2A de esta spec las hace la tarea 0.5.

## Technical Context

**Language/Version**: TypeScript 6.0.3 con Angular 22.2.1 (D-68), Node 24.16 (D-69, `.nvmrc`). La API, en .NET 10 tras la tarea 0.3 de la spec 002.

**Primary Dependencies**: `@angular/*` 22.2.1, `bootstrap` 5.3.2, `bootstrap-icons` 1.11.3, `@microsoft/signalr` 10.0.11, `@playwright/test` 1.63.0, `pixelmatch` 7.2.0, `pngjs` 7.0.0. Las versiones van exactas (CT-36). `vitest` y `jsdom` **requieren aprobación**: ver research R-06.

**Storage**: estado en memoria del navegador. No se usa `sessionStorage` (research R-03).

**Testing**:
- Unitarias de reglas con el runner oficial de Angular 22 (`ng test`, Vitest).
- Paridad visual y guiones de escenario con Playwright contra Blazor (`:9000`) y Angular (`:4200`).
- Prueba de integración del hub en `tests/PolyConecta.Api.Tests` si existe tras la tarea 0.3; si no, la cubre una prueba de Playwright de dos pestañas.

**Target Platform**: Chromium de escritorio a 1600×900 para la paridad. La aplicación corre en cualquier navegador moderno.

**Project Type**: aplicación web de una sola página (SPA), más un hub de SignalR en la API existente.

**Performance Goals**: un mensaje del chatter llega a otra pestaña en menos de 1 s (SC-005).

**Constraints**:
- La paridad visual tolera ≤ 1 % de píxeles distintos por ruta, y el umbral no se sube (regla de autonomía 8).
- `PolyConecta.Presentation/` es de solo lectura (D-60).
- No hay llamadas a la API REST (FR-010).

**Scale/Scope**: 18 páginas en 19 rutas, 13 componentes, 5 servicios de estado (unas 1,660 líneas de C#) y 9 formularios de modo libre.

## Constitution Check

*Se revisa antes de investigar y otra vez después del diseño.*

| Principio o regla | Cómo se cumple | Antes | Después |
| :--- | :--- | :---: | :---: |
| I · CONTPAQi es el sistema de registro | La réplica no escribe en CONTPAQi ni en la API: simula los folios (FR-010) | ✅ | ✅ |
| II · Escritura solo por outbox y bridge | No aplica: no hay escrituras reales | ✅ | ✅ |
| IV · Hard-stop de calidad | Se porta la regla del prototipo con su prueba (FR-008). En modo libre, traslado y entrega solo ofrecen lotes liberados | ✅ | ✅ |
| VII · Respaldo técnico | Nada de esta spec depende del SDK ni de las tablas `adm*` | ✅ | ✅ |
| IX · Odoo 19 | Se replican los componentes Odoo del prototipo sin cambios de estilo (FR-002, FR-005) | ✅ | ✅ |
| X · Documentos libres | 9 documentos con "Nuevo" y sus restricciones justificadas (FR-012). El diseño convierte los documentos únicos del prototipo en colecciones (R-02) | ⚠️ | ✅ |
| CT-07 · `Web → Api` | La réplica no llama a la API, salvo el hub de SignalR | ✅ | ✅ |
| CT-09 · Módulos por capa | `src/app/features/<modulo>/` | ✅ | ✅ |
| CT-24 · Sistema de diseño | Esta spec lo define junto con la tarea 0.5 | ✅ | ✅ |
| CT-28 · Regla sin prueba no está construida | Cada regla de FR-008 y de FR-012 lleva prueba unitaria | ✅ | ✅ |
| CT-36 · Versiones exactas | `package.json` sin `^` ni `~` y `.nvmrc` en 24.16.0 | ✅ | ✅ |
| CT-43 · Exploración dentro de la spec | Los cambios de la investigación están en la spec, en "Exploración y cambios" | ✅ | ✅ |
| CT-44 · Una rama por spec | Todo en `001-angular-presentation`; se integra a `main` al cerrar | ✅ | ✅ |

**El ⚠️ inicial** venía de que el prototipo tiene un solo pedido, un traslado, una recepción y una entrega, así que "Nuevo" no tenía dónde guardar un segundo documento. Se resuelve en el diseño (R-02, [data-model.md](data-model.md)) sin romper la paridad: la colección arranca con el mismo documento semilla.

## Dependencias con la spec 002

| Lo que necesita la 001 | Lo entrega | Efecto |
| :--- | :--- | :--- |
| Proyecto `PolyConecta.Web`, dependencias, estilos, layout y 12 componentes (fases 0 y 2A) | Tarea 0.5 (6 – 8 oct) | La 001 empieza su fase 1 sobre lo que deje la 0.5 (R-05) |
| Los tipos que usan los componentes (`StockOperationLine`, `LotBalance`, `ProductionLot`, `ProductRef`, `LotAllocation`) | Tarea 0.5, solo los tipos en `src/app/core/models/` | La fase 1 agrega los servicios y los datos semilla sin cambiar esos tipos |
| API en .NET 10 | Tarea 0.3 (5 – 6 oct) | La fase 2B (hub) espera a la API migrada; SC-006 se verifica sobre .NET 10 |
| `run.sh` adaptado a .NET 10 | Tarea 0.3 | La fase 5 agrega `--with-angular` sobre esa versión |

**Rama.** `001-angular-presentation` sale de `main`. Mientras la 002 no se integre a `main`, la 001 solo avanza en documentación y en lo que no dependa del proyecto Angular. Cuando la 002 se integre, la 001 trae `main` a su rama y sigue (ver la pregunta abierta 2 en la spec).

## Project Structure

### Documentación (esta spec)

```text
.specify/features/001-angular-presentation/
├── spec.md          # spec ratificada, con "Exploración y cambios"
├── plan.md          # este archivo
├── research.md      # decisiones de la investigación (R-01 a R-08)
├── data-model.md    # modelos y servicios de estado en TypeScript
├── quickstart.md    # cómo verificar la feature
├── contracts/
│   ├── chatter-hub.md   # contrato del hub de SignalR
│   └── parity.md        # interfaz de npm run parity y de los guiones
├── bloqueos.md      # lo crea la fase 1 al primer bloqueo (regla de autonomía 6)
└── tasks.md         # /speckit-tasks
```

### Código

```text
PolyConecta.Web/                      # proyecto Angular (lo crea la tarea 0.5)
├── .nvmrc                            # 24.16.0
├── package.json                      # versiones exactas; scripts build, test, parity, scenarios
├── angular.json
├── src/
│   ├── styles/app.css                # copia sin cambios de PolyConecta.Presentation/wwwroot/css/app.css
│   ├── index.html                    # mismas fuentes y CDN que App.razor
│   └── app/
│       ├── app.routes.ts             # las 19 rutas de FR-004
│       ├── core/
│       │   ├── models/               # tipos de data-model.md (los de los componentes, de la 0.5)
│       │   ├── state/                # inventory, stock-operation, operational-flow, ui-view (fase 1)
│       │   ├── search/               # search-view.ts y las vistas por modelo (fase 1)
│       │   ├── seed/                 # datos semilla idénticos al prototipo (fase 1)
│       │   └── chatter/              # cliente SignalR (fase 2B)
│       ├── shared/                   # los 13 componentes (12 de la 0.5; PocSalesOrderForm en 3A)
│       └── features/
│           ├── plataforma/           # dashboard en `/` (3A)
│           ├── ventas/               # pedidos (3A)
│           ├── inventario/           # inventario actual en `/inventario` y `/ventas/inventario` (3A)
│           ├── produccion/           # fabricación, captura masiva, incidencias (3B)
│           ├── calidad/              # calidad (3B)
│           └── logistica/            # recolecciones, traslados, recepción, entregas (3C)
├── e2e/
│   ├── parity/                       # capturas por ruta y comparación (FR-017)
│   └── scenarios/                    # guiones de US-2 contra las dos aplicaciones (FR-018)
└── parity-report/                    # informe; ignorado por git

PolyConecta.Api/
├── Hubs/ChatterHub.cs                # copia del hub del prototipo (fase 2B)
└── Program.cs                        # AddSignalR, MapHub y CORS para :4200

scripts/run.sh                        # --with-angular (fase 5); ./run.sh de la raíz delega en él
```

**Decisión de estructura.** Los módulos siguen CT-09, con los nombres de las áreas del dominio. El dashboard (`/`) no pertenece a ningún módulo en el prototipo (la barra superior no muestra menú en esa ruta), así que va en `plataforma/`. Las cuatro operaciones de almacén van juntas en `logistica/`, aunque en el prototipo cuelgan del menú "Inventario", para que el agente 3C tenga una sola carpeta. El reparto de las fases 3 y 4 por agente se mantiene:

- **3A:** `plataforma/`, `ventas/` e `inventario/`
- **3B:** `produccion/` y `calidad/`
- **3C:** `logistica/`

## Fases ajustadas

La tabla de "Ejecución por agentes" de la spec queda así (el cambio está en "Exploración y cambios"):

| Fase | Agentes | Trabajo | Empieza cuando |
| :--- | :---: | :--- | :--- |
| 0 · Base | — | **La hace la tarea 0.5** (002). La 001 solo agrega el arnés de paridad (FR-017) si la 0.5 no lo trae | La 0.5 está integrada |
| 1 · Estado | 1 | Servicios de estado, búsqueda y datos semilla con sus pruebas (FR-006 a FR-011), sobre los tipos de la 0.5 | Fase 0 |
| 2A · Componentes | — | **La hace la tarea 0.5**, salvo `PocSalesOrderForm`, que pasa a 3A (R-04) | — |
| 2B · Chatter | 1 | Hub en la API y cliente SignalR (FR-015, FR-016) | La API está en .NET 10 (0.3) |
| 3 · Páginas | 3 en paralelo | Igual que en la spec, más `PocSalesOrderForm` en 3A | Fase 1 |
| 4 · Modo libre | 3 en paralelo | Igual que en la spec, sobre las colecciones de R-02 | Fase 3 |
| 5 · Cierre | 1 | Igual que en la spec | Fase 4 y 2B |

## Complexity Tracking

| Desviación | Por qué hace falta | Alternativa más simple descartada |
| :--- | :--- | :--- |
| Colecciones donde el prototipo tiene un solo documento (pedido, traslado, recepción, entrega) | Sin colección, "Nuevo" (FR-012, Principio X) no tiene dónde crear un segundo documento | Reemplazar el documento único al crear uno nuevo: rompe el documento semilla y la paridad de US-2 |
| Chatter en vivo, que el prototipo no tiene | Lo pide D-58 y US-4 | Dejarlo local como en Blazor: incumple D-58 |
