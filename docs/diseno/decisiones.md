# Registro de decisiones

Decisiones tomadas **después** del informe de validación del diagrama operativo (`docs/assesment/INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md`). Este registro es la razón de que el diseño vigente difiera del informe en algunos puntos.

**Estado**: *Validada* = confirmada con el usuario o con la operación · *Propuesta* = derivada del código, los mockups o la arquitectura, pendiente de validar con la operación · *Adoptada* = decisión técnica ya aplicada en el código.

## 1. Qué cambió del informe de validación

El informe enumera 17 puntos. Estos siguen vigentes sin cambios: 2 (código de PT por especificación), 3 (estados del pedido), 4 (ficha multinivel), 12 (scrap clasificado), 16 (registro dual en bolseo) y 17 (4 reglas universales). Los demás se modificaron así:

| Punto del informe | Qué decía | Qué rige ahora | Decisión |
| :--- | :--- | :--- | :--- |
| 1 y 5 · Captura técnica | Campos de usuario en el pedido de CONTPAQi | La ficha técnica vive en PolyConecta y la captura AC ahí | D-01 |
| 6 · OT en Excel | Cero Excel; ingesta vía SDK | Sigue sin Excel, pero la ficha se captura en PolyConecta, no se ingiere | D-01 |
| 7 · Controles de firma | Botones "Validar" separados de Ventas y Crédito | Botón único "Autorizar" con dos firmas de personas distintas | D-15 |
| 8 y 9 · Jerarquía | OM → OF → WO, tres niveles | Una sola orden autorreferenciada; la planeación sustituye a la WO | D-05, D-06 |
| 10 · BoM de extrusión | Excel obligatorio con capas A/B/C | Lista plana de componentes capturada a mano, sin Excel | D-07, D-41 |
| 11 · Slots y calidad | Inspección precargada por slot | Los slots siguen; la calidad es un documento propio | D-09 |
| 13 · Balance de masa | Descuento consolidado desde MP al cierre | Descuento consolidado **desde WIP** al cierre; se suma la invariante de entrada | D-22 |
| 14 · Nomenclatura de lotes | `IV214-26-R001` | `R001-IV310-26` (rollo) y `C001-IV310-26` (bulto o caja en SC); se conserva la regla `.S` | D-10, D-45 |
| 15 · Traspaso interplanta | CONTPAQi registra el traspaso solo al validar la recepción | Sigue en dos pasos, pero el tránsito es un almacén en CONTPAQi: la salida registra origen → tránsito y la recepción, tránsito → destino | D-43 |

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
| D-24 | 2026-09-21 | La recolección nace con la OF en borrador y se libera al confirmarla *(matizado por D-55: también puede crearse libre)*. Admite parcialidades con backorder y devolución re-pesada a mano. No hay arrastre de saldo entre OF. WIP es uno por planta. | Validada | Recolección ⑧–⑬ |
| D-25 | 2026-09-22 | La recolección es una operación de traslado más (`PIM-REC-OUT` / `PIM-REC-RET`), no un documento especial. | Validada | Recolección, revisión del 22-sep |
| D-26 | 2026-09-23 | Las vistas de búsqueda declarativas se implementan en todos los modelos a la vez. | Validada | Búsqueda ① |
| D-27 | 2026-09-23 | El alcance por planta es una regla de fila, no un filtro. Un filtro nunca amplía lo que restringe una regla de fila. | Validada | Búsqueda ② |
| D-28 | 2026-09-23 | Seguridad Usuario × Rol × Planta en dos capas. Nadie aporta dos firmas del mismo pedido. Las acciones prohibidas se muestran deshabilitadas con su razón. | Validada (28-sep) | Usuarios, roles y permisos |
| D-29 | 2026-09-15 | La presentación se construye en Blazor Server con render interactivo (no WASM ni HTML estático), aprovechando el `ChatterHub` de SignalR. | Sustituida por D-48 | Consolidación del shell |
| D-30 | 2026-09-23 | Se retiran los duplicados legados del dominio (`MasterOrder`, `SubOrder`, `RolloMaestro`). | Adoptada | Redefinición de dominio |
| D-31 | 2026-09-28 | La documentación se compacta en `docs/diseno/` y se retiran las specs de feature y el roadmap. Las futuras specs parten de estos documentos. | Adoptada | Este registro |
| D-32 | 2026-09-28 | **Autenticación con usuarios propios** de PolyConecta (ASP.NET Identity). Se puede migrar a SSO más adelante. | Validada | P-01 |
| D-33 | 2026-09-28 | Pueden **revocar** una autorización cualquiera de los dos firmantes o el Administrador, mientras ningún documento generado haya avanzado (ninguna OF confirmada, ninguna entrega validada). Después se cancela documento por documento. | Validada | P-03 |
| D-34 | 2026-09-28 | Hay una persona que ejerce Comercial y Cobranza. **RF-4 se mantiene**: la segunda firma la da un suplente designado del otro rol. | Validada | P-04 |
| D-35 | 2026-09-28 | Un Planner **solo ve las órdenes de su planta**. Consecuencia: el Planner de SC no abre la OF de origen que está en PIM; la ve en el pedido o se la consulta a PIM. | Validada | P-05 |
| D-36 | 2026-09-28 | El alcance del Almacenista es **por planta** (MP, WIP y PT de su planta). | Validada | P-06 |
| D-37 | 2026-09-28 | La **sustitución** de producto no requiere segunda autorización: AC decide y el sistema exige motivo y registra la evidencia. | Validada | P-07 |
| D-38 | 2026-09-28 | Cada rol puede tener un **suplente designado** de antemano. Todo queda atribuido a quien actúa. | Validada | P-08 |
| D-39 | 2026-09-28 | **No hay handheld, terminal de báscula ni escáner**: todo se captura en el sistema web. El operador anota en diarios y el Planner los vacía. El operador no usa el sistema. | Validada | P-09 |
| D-40 | 2026-09-28 | Nuevo rol **Supervisor de turno**: captura las incidencias (paros). | Validada | P-10 |
| D-41 | 2026-09-28 | **No hay importación de componentes desde Excel**: el Planner los captura a mano. Se retira el botón Importar. | Validada | P-11 |
| D-42 | 2026-09-28 | Estados de la orden de fabricación, estilo Odoo: `Borrador → Confirmada → En progreso → Por cerrar → Hecha`, y `Cancelada`. "Por cerrar" separa la producción terminada del cierre técnico. | Validada | P-12 |
| D-43 | 2026-09-28 | **Convención `SC`** para Santa Cruz (planta, ubicaciones y tipos de operación). **Las ubicaciones las define PolyConecta**; al inicializar se reservan **todas** como almacenes en `admAlmacenes`, incluidas las virtuales. Consecuencia: la salida a tránsito sí escribe en CONTPAQi. | Validada | P-13 |
| D-44 | 2026-09-28 | El catálogo de centros de trabajo lo **levantan los Planners**; los códigos actuales son ejemplos. | Validada | P-14 |
| D-45 | 2026-09-28 | El lote final de Santa Cruz se nombra `C{3 dígitos}-{folio del pedido}` (`C001-IV310-26`), con `.S` en cuarentena. | Validada | P-15 |
| D-46 | 2026-09-28 | La **tolerancia** del balance de masa la define Producción. Es configurable y no tiene valor por defecto hasta que Producción la fije. | Validada | P-16 |
| D-47 | 2026-09-28 | **Montemorelos, todo en Fase 2.** Se modelan la planta y la entidad legal, pero no hay rutas ni documentos intercompany. | Validada | P-17 |
| D-48 | 2026-09-28 | **La presentación se hace en Angular.** Primero una réplica 1:1 del prototipo; después se conecta a la API. Sustituye a D-29. | Validada | P-18 |
| D-49 | 2026-09-28 | La **base de datos** de PolyConecta es **SQL Server** (una base propia, separada de las de CONTPAQi). | Validada | T-09 |
| D-50 | 2026-09-28 | La constitución pasa a la versión 1.5.0: principios I, II, IV, V, VI y IX alineados con D-39, D-43 y D-47. | Adoptada | D-39, D-43, D-47 |
| D-51 | 2026-09-28 | La contraseña de `sa` se rota y el historial de git **no** se reescribe. La cadena de conexión sale del repositorio a la variable de entorno `BridgeConfig__SqlConnectionString` y el bridge usa un login de solo lectura. | Validada | Seguridad |
| D-52 | 2026-09-28 | **Principio de documento libre.** Todo documento operativo puede crearse con "Nuevo", sin documento de origen. El origen es una referencia opcional, no una precondición. Las reglas del documento (estados, permisos, hard-stop, balance de masa, escrituras en CONTPAQi y documentos derivados) aplican igual con o sin origen. | Validada | Filosofía de Odoo |
| D-53 | 2026-09-28 | El **pedido de venta** puede crearse libre en PolyConecta y **el bridge lo da de alta en CONTPAQi**, que sigue siendo el sistema de registro. El pedido capturado en CONTPAQi sigue entrando por sincronización. Sustituye a "PolyConecta no da de alta pedidos a mano". | Validada | D-52 |
| D-54 | 2026-09-28 | Los lotes de una OF **sin pedido** usan el folio de la **OF raíz**, cambiando `/` por `-`: `R001-BOL-2026-0007`, `C001-BOL-2026-0007`. Con pedido no cambia (`R001-IV310-26`). En el nombre de un lote siempre se usa guion medio. | Validada | D-10, D-45 |
| D-55 | 2026-09-28 | Se puede crear una **recolección libre** (MP → WIP sin OF). El material queda en WIP como **saldo sin asignar** y se liga a una OF después, al confirmarla. | Validada | D-24 |
| D-56 | 2026-09-28 | Una **recepción libre** solo puede recibir lotes que ya estén en tránsito (`TRANS/*`). Se mantiene la invariante de tránsito: no se recibe material que no salió de otra planta. | Validada | D-52 |
| D-57 | 2026-09-28 | La constitución pasa a la versión 1.6.0 con el **Principio X, documentos libres** (D-52). | Adoptada | D-52 |
| D-58 | 2026-09-29 | El chatter de la réplica en Angular usa un `ChatterHub` en `PolyConecta.Api` (copia del hub del prototipo, que no se toca), con CORS para Angular. | Validada | Spec 001 |
| D-59 | 2026-09-29 | La réplica habilita el botón **"Nuevo"** con formularios de creación libre en 9 documentos (Principio X). Es la única diferencia permitida con el prototipo. | Validada | Spec 001 |
| D-60 | 2026-09-29 | El prototipo Blazor **se conserva** como referencia hasta que la presentación en Angular se conecte a la API, y no se modifica. | Validada | Spec 001 |
| D-61 | 2026-09-29 | La spec 001 se ejecuta **por fases con agentes en paralelo**, cada uno dueño exclusivo de sus carpetas, con reglas de autonomía y registro de bloqueos. | Validada | Spec 001 |
| D-62 | 2026-09-29 | PolyConecta guarda sus datos en **tablas propias** con su modelo de dominio. La paridad con CONTPAQi vive en columnas `erp_*` y en un catálogo de mapeo, no en tablas espejo de `adm*`. | Validada | Constitución técnica |
| D-63 | 2026-09-29 | El único punto de contacto entre PolyConecta y la integración es el **contrato HTTP del bridge**, versionado, con un **bridge en modo simulado** que lo cumple sin SDK. | Validada | Constitución técnica |
| D-64 | 2026-09-29 | La construcción se organiza en **6 módulos** por dependencia de datos: Plataforma, Catálogos e Inventario, Ventas, Producción, Calidad y Logística. La conversión en Santa Cruz es parte de Producción, no un módulo aparte. | Validada | Roadmap |
| D-65 | 2026-09-29 | Cada módulo tiene **dos cierres**: *cerrado en PolyConecta* (con el simulador) y *cerrado integrado* (contra CONTPAQi de laboratorio). | Validada | Roadmap |
| D-66 | 2026-09-29 | La construcción avanza por **dos caminos** con un líder cada uno: integración CONTPAQi y PolyConecta independiente. | Validada | Roadmap |
