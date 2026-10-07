# Prueba técnica de la spec 011 (7-oct)

Código de referencia para P1. Se corrió en una copia de `PolyConecta.Web` con las librerías instaladas, en la ruta `/prueba-tecnica`. **No se compila desde aquí.** Al empezar P1 se instalan las dependencias y se toma lo que sirva.

| Archivo | Qué prueba |
| :--- | :--- |
| `origen.ts` | `OrigenDeLista<T>` y su versión en memoria: página, orden, filtros y agrupación con totales |
| `tabla-prueba.ts` | TanStack Table 9 en modo servidor: ordenar, paginar, filas por página, filtrar, agrupar con grupos que se abren bajo pedido, ocultar y reordenar columnas, seleccionar y exportar a Excel |
| `kanban-prueba.ts` | Kanban con el drag & drop de Angular CDK: transición válida, inválida (la tarjeta regresa con el motivo) y con diálogo (firma) |
| `campos-prueba.ts` | Spartan `brain`: combobox con filtro y teclado, y calendario en español dentro de un popover |
| `prueba.spec.ts`, `playwright.config.ts` | Las 10 pruebas de Playwright, todas en verde |
| `prueba-tecnica.png` | Captura de la página con un grupo abierto |

La línea `window.__tabla = this` de `tabla-prueba.ts` fue solo para depurar; no va en P1.
