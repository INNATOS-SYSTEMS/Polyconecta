# Registro de decisiones

Decisiones tomadas **después** del informe de validación del diagrama operativo (`docs/assesment/INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md`). Este registro es la razón de que el diseño vigente difiera del informe en algunos puntos.

**Estado**: *Validada* = confirmada con el usuario o con la operación · *Propuesta* = derivada del código, los mockups o la arquitectura, pendiente de validar con la operación · *Adoptada* = decisión técnica ya aplicada en el código.

## 1. Qué cambió del informe de validación

El informe enumera 17 puntos. Estos siguen vigentes sin cambios: 2 (código de PT por especificación), 3 (estados del pedido), 4 (ficha multinivel), 12 (scrap clasificado), 15 (traspaso en 2 pasos), 16 (registro dual en bolseo) y 17 (4 reglas universales). Los demás se modificaron así:

| Punto del informe | Qué decía | Qué rige ahora | Decisión |
| :--- | :--- | :--- | :--- |
| 1 y 5 · Captura técnica | Campos de usuario en el pedido de CONTPAQi | La ficha técnica vive en PolyConecta y la captura AC ahí | D-01 |
| 6 · OT en Excel | Cero Excel; ingesta vía SDK | Sigue sin Excel, pero la ficha se captura en PolyConecta, no se ingiere | D-01 |
| 7 · Controles de firma | Botones "Validar" separados de Ventas y Crédito | Botón único "Autorizar" con dos firmas de personas distintas | D-15 |
| 8 y 9 · Jerarquía | OM → OF → WO, tres niveles | Una sola orden autorreferenciada; la planeación sustituye a la WO | D-05, D-06 |
| 10 · BoM de extrusión | Excel obligatorio con capas A/B/C | Lista plana editable de componentes; si sigue el Excel está por confirmar | D-07 |
| 11 · Slots y calidad | Inspección precargada por slot | Los slots siguen; la calidad es un documento propio | D-09 |
| 13 · Balance de masa | Descuento consolidado desde MP al cierre | Descuento consolidado **desde WIP** al cierre; se suma la invariante de entrada | D-22 |
| 14 · Nomenclatura de lotes | `IV214-26-R001` | `R001-IV310-26`; se conserva la regla `.S` | D-10 |

## 2. Decisiones

| ID | Fecha | Decisión | Estado | Origen |
| :--- | :--- | :--- | :--- | :--- |
| D-01 | 2026-09-18 | La ficha técnica (bloques Rollo y PT) vive en PolyConecta, ligada al producto por su código ERP. Ni los campos de usuario del documento ni los del producto de CONTPAQi alcanzan. | Validada | Redefinición de dominio, rev. 1 y 2 |
| D-02 | 2026-09-18 | El pedido es maestro + detalle (`SalesOrderLine`). La meta de producción y la tolerancia van por línea, no en el catálogo. `PtSpecification` siempre referencia una `RollSpecification`. | Validada | Rev. 2 |
| D-03 | 2026-09-18 | La unidad base es siempre KG; la unidad de venta es configurable por producto (`PackagingUnit`) y elegible por línea. | Validada | Rev. 3, CFDI S-26234 |
| D-04 | 2026-09-18 | Precio, IVA y totales no se modelan en PolyConecta. | Validada | Rev. 3 |
| D-05 | 2026-09-19 | Una sola `ManufacturingOrder` autorreferenciada (`OriginOrderId`). La raíz es el proceso que se entrega al cliente. Todas las órdenes usan el mismo formulario. | Validada | Rev. 4, mockups y reunión del 18-sep |
| D-06 | 2026-09-19 | La `WorkOrder` deja de ser documento y pasa a `PlanningLine` embebida en la orden, repartible entre máquinas y días. | Validada | Rev. 4 |
| D-07 | 2026-09-19 | Los componentes son una lista plana editable (clave, producto, cantidad, unidad), sin capas A/B/C. | Validada | Rev. 4 |
| D-08 | 2026-09-19 | Nueva pestaña Subproductos: productos de scrap por proceso, con almacén destino. | Validada | Rev. 4 |
| D-09 | 2026-09-19 | El control de calidad es un documento propio (`QC/2026/000X`) con tabla de controles por lote y acciones por línea y globales. | Validada | Rev. 4 |
| D-10 | 2026-09-19 | El lote se nombra `R{3 dígitos}-{folio del pedido}` (`R001-IV310-26`). | Validada | Rev. 4 |
| D-11 | 2026-09-19 | La logística tiene tres documentos: traslado y recepción (dos mitades del traspaso, 5 estados, recepción parcial por lote) y entrega (un paso, 4 estados). | Validada | Rev. 4 |
| D-12 | 2026-09-19 | Incidencia (paro de máquina) como entidad global, no ligada a una orden. | Validada | Rev. 4 |
| D-13 | 2026-09-19 | El pedido suma OC del cliente, agente y Contpaq ID de solo lectura. Cada proceso de la línea lleva su planta y su producto. | Validada | Rev. 4 |
| D-14 | 2026-09-22 | La decisión de fabricar, entregar o traspasar vive en el catálogo como **ruta configurada**, con comportamiento MTSO. No es un cálculo de pantalla. | Validada | Reglas de abastecimiento, revisión del 22-sep |
| D-15 | 2026-09-21 | La autorización lleva dos firmas (Comercial y Cobranza) con un único botón, y es el disparador del motor. | Validada | Abastecimiento ③ |
| D-16 | 2026-09-21 | La reserva se compromete al autorizar, lote por lote, reasignando kg en el último lote. | Validada | Abastecimiento ③ ⑤ |
| D-17 | 2026-09-21 | La ruta se resuelve en tres niveles con herencia (clasificación → producto → línea). La de la línea gana y AC puede forzar la fabricación. | Validada | Abastecimiento ⑥ ⑦ |
| D-18 | 2026-09-21 | La entrega se genera por el total; en parcialidades se pregunta por el backorder. | Validada | Abastecimiento ⑧ |
| D-19 | 2026-09-21 | Atención a Clientes decide el traspaso interplanta. | Validada | Abastecimiento ⑨ |
| D-20 | 2026-09-21 | No hay un criterio cerrado de sustitución: el sistema asiste y registra. Reasignar material entre clientes se hace con re-lotificación explícita. | Validada | Abastecimiento ① ② |
| D-21 | 2026-09-21 | El visor de disponibilidad agrupa por clasificación y es solo de consulta. | Validada | Abastecimiento ④ |
| D-22 | 2026-09-21 | **WIP es un almacén contable en CONTPAQi.** La recolección MP→WIP genera traspaso; el consumo al cierre se descuenta desde WIP. Modifica la regla 7.2 de la arquitectura de almacenes. | Validada | Recolección ⑥ |
| D-23 | 2026-09-21 | El Almacenista declara y valida lo que sale; el Planner solo solicita. | Validada | Recolección ⑦ |
| D-24 | 2026-09-21 | La recolección nace con la OF en borrador y se libera al confirmarla. Admite parcialidades con backorder y devolución re-pesada a mano. No hay arrastre de saldo entre OF. WIP es uno por planta. | Validada | Recolección ⑧–⑬ |
| D-25 | 2026-09-22 | La recolección es una operación de traslado más (`PIM-REC-OUT` / `PIM-REC-RET`), no un documento especial. | Validada | Recolección, revisión del 22-sep |
| D-26 | 2026-09-23 | Las vistas de búsqueda declarativas se implementan en todos los modelos a la vez. | Validada | Búsqueda ① |
| D-27 | 2026-09-23 | El alcance por planta es una regla de fila, no un filtro. Un filtro nunca amplía lo que restringe una regla de fila. | Validada | Búsqueda ② |
| D-28 | 2026-09-23 | Seguridad Usuario × Rol × Planta en dos capas. Nadie aporta dos firmas del mismo pedido. Las acciones prohibidas se muestran deshabilitadas con su razón. | Propuesta | Usuarios, roles y permisos |
| D-29 | 2026-09-15 | La presentación se construye en Blazor Server con render interactivo (no WASM ni HTML estático), aprovechando el `ChatterHub` de SignalR. | Adoptada | Consolidación del shell |
| D-30 | 2026-09-23 | Se retiran los duplicados legados del dominio (`MasterOrder`, `SubOrder`, `RolloMaestro`). | Adoptada | Redefinición de dominio |
| D-31 | 2026-09-28 | La documentación se compacta en `docs/diseno/` y se retiran las specs de feature y el roadmap. Las futuras specs parten de estos documentos. | Adoptada | Este registro |
