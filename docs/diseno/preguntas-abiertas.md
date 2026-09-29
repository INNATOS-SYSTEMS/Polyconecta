# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

- Resueltas el 28-sep-2026: P-01 a P-18 y T-09 (D-32 a D-51).
- Resueltas el 29-sep-2026: P-19 (D-74) y P-20 (D-67 a D-73).
- P-02, P-14 y P-16 pasaron a datos de puesta en marcha (D-75) y viven en el [roadmap](../ROADMAP.md).
- I-01 y U-01 eran trabajo planeado, no preguntas: están en el alcance de los módulos del roadmap.

## Infraestructura

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| H-01 | **Hosting de producción** de PolyConecta (API, Angular y SQL Server 2022): servidor propio aparte del de CONTPAQi, el mismo VPS o nube administrada. Debe cumplir CT-05 (instancia separada de CONTPAQi) y tener conectividad con el bridge | Piloto del primer módulo; ambiente de producción (D-77) |
| H-02 | **Respaldos** de la base de PolyConecta. Se define con H-01. Requisito mínimo ya fijado: respaldo completo diario y de logs de transacciones, con una prueba de restauración antes del piloto | Piloto del primer módulo |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-01 | ¿El SDK hace traspaso entre almacenes con lote (MP ↔ WIP, origen ↔ tránsito)? | B | WIP y tránsito pasan a ser ubicaciones internas de PolyConecta; se revierten D-22 y parte de D-43 |
| T-02 | ¿Admite varios lotes por movimiento y fraccionar cantidad? | C-02, C-03 | Solo se reserva el lote completo; un movimiento por lote |
| T-03 | ¿Admite devolución parcial con cantidad manual? | D-02 | Devolución total o nada |
| T-04 | ¿Soporta backorder? | E | El backorder vive solo en PolyConecta |
| T-05 | ¿CONTPAQi clasifica productos? | A-05 | PolyConecta mantiene su propia clasificación |
| T-06 | ¿Leer existencias directo alcanza o hace falta una proyección? | F-04, F-05 | Proyección propia de existencias |
| T-07 | ¿La remisión creada por SDK puede enlazarse al pedido de origen? | nuevo caso | El pedido queda "pendiente de surtir" en CONTPAQi |
| T-08 | Corregir el documento huérfano cuando falla el movimiento | G-01 | — |
| T-10 | ¿El SDK puede **dar de alta almacenes**? `docs/contpaq/Referencia_SDK_CONTPAQi.md` no documenta ninguna función para eso; hay que verificarlo en la documentación oficial (Principio VII) | nuevo caso | Los almacenes se crean a mano en CONTPAQi una sola vez al inicializar, con los códigos que dicta PolyConecta |
| T-11 | Si `Produccion`, `Customers` y `Vendors` son almacenes en CONTPAQi (D-43), ¿cómo se registran el consumo, la remisión y la compra? ¿Como traspasos hacia esos almacenes o como documentos de salida y entrada desde el almacén real? | nuevo caso | Se excluyen esas tres ubicaciones de la reserva en `admAlmacenes` |
| T-12 | ¿El SDK da de alta un **pedido** con sus líneas y su **precio unitario y moneda** (D-74), y devuelve su folio, para que la sincronización lo reconozca y no lo vuelva a importar como pedido nuevo (idempotencia por `erp_document_id`)? | nuevo caso | El pedido libre queda solo interno en PolyConecta hasta resolverlo |
