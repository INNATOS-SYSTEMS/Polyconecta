# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí. Las preguntas P-01 a P-18 y T-09 se resolvieron el 28-sep-2026 (D-32 a D-51); aquí solo queda lo que falta.

## Datos pendientes de la operación

La decisión ya está tomada; falta el dato.

| # | Qué falta | Quién lo entrega | Bloquea |
| :---: | :--- | :--- | :--- |
| P-02 | **Titulares** de Comercial, Crédito y Cobranza, Almacenista, Calidad, Tráfico y Supervisor de turno, con su planta y el suplente de cada rol (D-38) | Dirección | Configurar usuarios. No bloquea la construcción |
| P-14 | **Catálogo de centros de trabajo**: código, proceso, planta y capacidad (D-44) | Planners de PIM y SC | Planeación de órdenes |
| P-16 | **Tolerancia** del balance de masa (D-46) | Producción | Cierre técnico en operación real |
| P-19 | **Precio del pedido libre** (D-53): D-04 dice que PolyConecta no modela precios, pero un pedido dado de alta en CONTPAQi normalmente los lleva. ¿Se toma de la lista de precios de CONTPAQi, lo captura AC o el pedido entra sin precio y Facturación lo completa en CONTPAQi? | Dirección y Facturación | Pedido libre |

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
| T-12 | ¿El SDK da de alta un **pedido** con sus líneas y devuelve su folio, para que la sincronización lo reconozca y no lo vuelva a importar como pedido nuevo (idempotencia por `erp_document_id`)? | nuevo caso | El pedido libre queda solo interno en PolyConecta hasta resolverlo |

## Técnicas internas

| # | Qué falta |
| :---: | :--- |
| I-01 | La spec 001 (`.specify/features/001-angular-presentation/`) deja la réplica con estado en el navegador. Falta la feature que la conecta a la API y adopta el modelo de `04-modelo-de-dominio.md` (D-48, D-60). |

## Interfaz

| # | Pregunta |
| :---: | :--- |
| U-01 | Render agrupado en las listas: las agrupaciones ya se declaran, pero falta dibujarlas. |
