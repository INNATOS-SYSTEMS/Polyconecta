# PolyConecta.Presentation (prototipo)

Prototipo navegable en **Blazor Server** (.NET 8) que demuestra el flujo operativo completo con la experiencia de Odoo 19. Corre en `http://localhost:9000`.

> **Es un prototipo, no la aplicación.** Todo el estado vive en memoria del servidor y se pierde al reiniciar. No llama a `PolyConecta.Api` y los folios de CONTPAQi (Contpaq ID) son simulados. Sirve de referencia de comportamiento y de UX; el modelo real está en [04-modelo-de-dominio.md](../docs/diseno/04-modelo-de-dominio.md).

## Contenido

| Carpeta | Contenido |
| :--- | :--- |
| `Pages/` | 18 pantallas: pedidos, fabricación, recolecciones, calidad, traslados, recepciones, entregas, inventario actual, incidencias, captura masiva de producción y dashboard |
| `Components/Shell`, `Layout` | Shell de estilo Odoo: barra superior, app launcher y breadcrumb |
| `Components/Forms` | Smart buttons, pipeline de estado, captura de líneas y selectores de lote |
| `Components/Views` | Barra de búsqueda con facetas, paginador y selector de vista |
| `Components/Chatter`, `Hubs/` | Chatter por documento en vivo (`ChatterHub`, SignalR) |
| `Services/` | Estado del prototipo: `OperationalFlowState` (pedido, órdenes, calidad, logística), `StockOperationState` (recolecciones y operaciones), `InventoryState`, `SearchViews` (vistas de búsqueda declarativas) y `UiViewState` |

## Qué demuestra

El recorrido completo de [02-flujo-y-reglas.md](../docs/diseno/02-flujo-y-reglas.md): pedido con autorización de dos firmas, órdenes autorreferenciadas con componentes, subproductos, producción y planeación; recolección a WIP con backorder y devolución; calidad con hard-stop; traslado y recepción en dos pasos; y entrega. El estado de cada módulo está en [01-modulos-y-roles.md](../docs/diseno/01-modulos-y-roles.md).

Hay una propuesta para replicar esta capa en Angular. Está sin decidir (P-18 en [preguntas abiertas](../docs/diseno/preguntas-abiertas.md)).
