# MATRIZ DE PRUEBAS TÉCNICAS — SDK CONTPAQi para WIP, Lotes y Traspasos

**Proyecto:** PolyConecta · **Fecha:** 21 de septiembre de 2026 · **Estatus:** Pendiente de ejecución

**Propósito:** Verificar contra una instalación real de CONTPAQi Comercial Premium que el SDK soporta las operaciones que asumen SPEC-007 (abastecimiento, hoy [02-flujo-y-reglas.md §2](../diseno/02-flujo-y-reglas.md)) y SPEC-008 (recolección a WIP, hoy [§4](../diseno/02-flujo-y-reglas.md)). Las referencias `SPEC-00X FR-NNN` de esta matriz corresponden a los identificadores `[00X-FR-NNN]` de esos documentos. **Ninguna de las dos specs debe pasar a implementación productiva antes de cerrar los bloques B, C y D de esta matriz** — son supuestos, no hechos verificados.

**Código bajo prueba:** `PolyConecta.Contpaq/Infrastructure/Sdk/ContpaqiSdkGateway.cs` (creación de documento), `ContpaqiSdkNative.cs` (interop).

---

## Entorno requerido

| Requisito | Detalle |
| :--- | :--- |
| Instalación | CONTPAQi Comercial Premium con `MGW_SDK.dll` accesible (Win32, proceso x86) |
| Empresa | **Copia de respaldo de producción**, nunca la empresa viva. Varias pruebas dejan documentos afectados no reversibles. |
| Productos | Al menos un SKU con control de lotes activo (`CTIPOCAPA = 3`) y existencia real en dos lotes distintos |
| Almacenes | `MP` existente + **`WIP` creado para la prueba** (ver A-03) |
| Herramientas | Acceso SQL directo a la BD de la empresa para verificación independiente (`admCapasProducto`, `admExistenciaCosto`, `admAlmacenes`) |

**Regla de verificación:** el resultado del SDK (`err = 0`) **no es evidencia suficiente**. Toda prueba se valida además por consulta SQL directa y por lo que muestra la UI de CONTPAQi. Se han observado casos de funciones que devuelven éxito sin persistir.

**Convención de resultado:** ✅ Pasa · ⚠️ Pasa con limitación (documentar) · ❌ Falla (bloquea la decisión asociada)

---

## Bloque A — Entorno y catálogo

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **A-01** | Conectividad base | `fInicializaSDK()` → `fAbreEmpresa(ruta)` | Ambas devuelven `0`. Registrar versión exacta del SDK y del ejecutable. | Bloquea todo |
| **A-02** | Diagnóstico de errores | Provocar un error conocido (empresa inexistente) y leer `fError()` | El mensaje es legible y el código es estable entre corridas | Degrada diagnóstico de todos los bloques |
| **A-03** | **Alta del almacén WIP** | Crear el almacén `WIP` — por SDK si existe función, si no por la UI de CONTPAQi | El almacén existe en `admAlmacenes` y es seleccionable en movimientos | **Decisión ⑥ inviable como está**: reevaluar si WIP es contable |
| **A-04** | Un WIP por planta | Crear `WIP-PIM` y `WIP-STC` | Ambos coexisten y son distinguibles por `CCODIGOALMACEN` | Revisar FR-001 de SPEC-008 |
| **A-05** | Clasificación de producto | Consultar `admProductos.CIDVALORCLASIFICACION1..6` y `admClasificacionesValores` | Existe una clasificación utilizable para agrupar bolsas / rollo impreso / rollo liso / rollo maestro / MP | **SPEC-007 FR-002 sin fuente de datos**: habría que mantener la clasificación en PolyConecta |

> **A-05 es la prueba que decide la arquitectura del visor.** Si CONTPAQi ya clasifica los productos, el visor lee; si no, PolyConecta necesita su propio `ProductCategory` y un mapeo mantenido a mano.

---

## Bloque B — Traspaso entre almacenes (sin lote)

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **B-01** | Identificar el concepto de traspaso | Listar conceptos de documento disponibles en la empresa | Se identifica el `CCODIGOCONCEPTO` del traspaso entre almacenes y si requiere uno o dos documentos (salida + entrada) | Bloquea B-02 en adelante |
| **B-02** | Traspaso simple MP → WIP | `fAltaDocumento` (concepto traspaso) + `fAltaMovimiento` con `aCodAlmacen` origen y destino | `err = 0` y el documento existe con folio | **Decisión ⑥ inviable** |
| **B-03** | Afectación de existencias | Tras B-02, consultar `admExistenciaCosto` para ambos almacenes | MP baja exactamente la cantidad movida y WIP sube la misma cantidad | ⑥ inviable |
| **B-04** | Afectar el documento | `fAfectaDocto_Param(concepto, serie, folio, true)` | `err = 0` y la existencia solo se mueve tras afectar | Revisar si el traspaso requiere afectación explícita |
| **B-05** | **Semántica de `aCodAlmacen`** | Verificar cómo se expresan origen y destino en `tMovimiento`, que tiene **un solo** campo `aCodAlmacen` | Queda documentado si el traspaso requiere **dos movimientos** (uno negativo en origen, uno positivo en destino) o si el concepto lo resuelve | **Cambio de diseño en el bridge**: `MovementPayload` necesitaría `codigo_almacen_destino` |

> **B-05 es un hallazgo del código actual, no una hipótesis.** `tMovimiento` (`ContpaqiSdkNative.cs:33`) expone un único `aCodAlmacen`, y `MovementPayload` un único `codigo_almacen`. Un traspaso tiene dos extremos. O el concepto de documento los infiere, o el contrato del bridge está incompleto para SPEC-008.

---

## Bloque C — Lotes (el bloque crítico)

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **C-01** | Traspaso con un lote completo | `fAltaMovimiento` + `fAltaMovimientoSeriesCapas` con `aLote = R001`, `aUnidades` = total | El lote aparece en WIP en `admCapasProducto` con la existencia correcta | Bloquea toda la trazabilidad por lote |
| **C-02** | **Multi-lote en un movimiento** | Un `fAltaMovimiento` de 150 kg + **dos** llamadas a `fAltaMovimientoSeriesCapas` sobre el mismo `aIdMovimiento`: `R001`/100 y `R002`/50 | Ambas devuelven `0` y la suma de capas cuadra con las unidades del movimiento | **Bloquea US-4 de SPEC-007.** Habría que emitir un movimiento por lote |
| **C-03** | **Fraccionamiento de lote** | Tomar 50 kg de un lote `R002` que tiene 95 kg | El lote queda con 45 kg disponibles en `admCapasProducto.CEXISTENCIA` del almacén origen | **Bloquea decisión ⑤ (re-lotificación) y FR-009 de SPEC-007** |
| **C-04** | Linaje de capa en destino | Tras C-01, consultar `admCapasProducto.CIDCAPAORIGEN` de la capa creada en WIP | Apunta a la capa del almacén origen | Se pierde trazabilidad lote↔lote en el traspaso; la reconstruiría PolyConecta |
| **C-05** | Mismo lote en dos almacenes | Verificar `R001` presente en MP y WIP simultáneamente | Son capas distintas con existencias independientes, sin colisión de número de lote | Revisar nomenclatura de lote de SPEC-002 |
| **C-06** | Recálculo | `fCalculaMovtoSerieCapa` tras las altas de capa | Las unidades del movimiento cuadran con la suma de capas | Posible descuadre silencioso |
| **C-07** | Suma de capas ≠ unidades | Provocar deliberadamente `aUnidades` del movimiento ≠ suma de capas | CONTPAQi **rechaza** la operación | Si la acepta, el bridge debe validar antes de enviar |

> **C-02 y C-03 son las dos pruebas que más pueden cambiar el diseño.** El gateway actual (`ContpaqiSdkGateway.cs:520`) llama a `fAltaMovimientoSeriesCapas` **una sola vez por movimiento** y le pasa `aUnidades = movPayload.Unidades`, es decir, asume *un lote por movimiento por la cantidad completa*. Reservar 150 kg tomando dos lotes — el caso normal en Polyempaques, con rollos de peso variable — no está soportado por el código tal como está hoy.

---

## Bloque D — Devolución y reversa

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **D-01** | Traspaso inverso WIP → MP | Repetir B-02 con almacenes invertidos | Existencias vuelven al estado previo | Bloquea FR-009 de SPEC-008 |
| **D-02** | **Devolución por cantidad manual** | Surtir 300 kg y devolver **287.5 kg** (cifra capturada, no el saldo teórico) | El traspaso inverso acepta la cantidad arbitraria; WIP queda con 12.5 kg de saldo | **Bloquea decisión ⑩.** Revisar si la devolución debe ser total o nada |
| **D-03** | Devolución al lote de origen | Devolver material de un lote fraccionado | La existencia regresa **a la capa original**, no crea una capa nueva duplicada | Proliferación de capas; afecta el visor |
| **D-04** | Desafectación | `fAfectaDocto_Param(..., false)` sobre un traspaso ya afectado | Revierte la afectación, o falla de forma explícita | Si no revierte, **la cancelación debe modelarse como documento inverso, nunca como borrado** |
| **D-05** | Existencia insuficiente | Intentar devolver más de lo que hay en WIP | CONTPAQi rechaza o permite negativo — documentar cuál | El bridge debe validar previamente |

---

## Bloque E — Parcialidades y backorders

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **E-01** | N traspasos parciales | Surtir 300 kg en tres documentos de 100 kg | Los tres coexisten; la existencia en WIP acumula 300 | Bloquea US-3 de SPEC-008 |
| **E-02** | Unidades pendientes | `fObtieneUnidadesPendientes(concepto, producto, almacén, unidades)` | Devuelve el pendiente real y sirve para calcular el backorder | El backorder se calcula íntegramente en PolyConecta (aceptable) |
| **E-03** | Parcialidad con lotes distintos | Tanda 1 del lote `R001`, tanda 2 del `R002` | Ambas capas conviven en WIP de forma independiente | Revisar el modelo de `WipBalance` |

---

## Bloque F — Lectura de existencias para el visor (SPEC-007)

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **F-01** | Existencia por producto y almacén | Consultar `admExistenciaCosto` (`CTIPOEXISTENCIA = 1`) | Coincide con lo que muestra la UI de CONTPAQi | Bloquea FR-001 de SPEC-007 |
| **F-02** | Existencia por lote | Consultar `admCapasProducto.CEXISTENCIA` donde `CTIPOCAPA = 3` | Coincide con el desglose por lote de la UI | Bloquea FR-003 (desglose por lote) |
| **F-03** | Consistencia entre ambas | Sumar `CEXISTENCIA` de las capas de un producto/almacén y comparar con F-01 | Cuadran exactamente | Define cuál de las dos es la fuente de verdad del visor |
| **F-04** | Latencia | Medir la consulta de existencias de todo el catálogo por almacén | **< 2 s** para uso interactivo en captura de pedido | Requiere caché o proyección materializada en PolyConecta |
| **F-05** | Frescura | Modificar existencia por la UI y volver a consultar | El cambio se refleja de inmediato | Definir estrategia de invalidación de caché |
| **F-06** | Existencia negativa | Buscar productos con existencia negativa en la empresa real | Se documenta cuántos hay y cómo los trata el visor | El visor podría prometer material inexistente |

> El disponible del visor es `existencia CONTPAQi − reservas PolyConecta`. **CONTPAQi no conoce las reservas**: son estado propio de PolyConecta. F-03 decide sobre qué cifra se resta.

---

## Bloque G — Robustez del bridge

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **G-01** | **Fallo a media transacción** | Forzar que `fAltaMovimiento` falle **después** de un `fAltaDocumento` exitoso | No queda documento huérfano en CONTPAQi | **Defecto ya presente en el código** — ver nota |
| **G-02** | Idempotencia | Reenviar la misma transacción con idéntico `idempotency_key` | No se duplica el documento | Riesgo de traspasos dobles ante reintento |
| **G-03** | Concurrencia | Dos traspasos simultáneos sobre el mismo lote | Uno gana; el otro falla de forma limpia, sin existencia negativa | Serializar los traspasos en el bridge |
| **G-04** | Reconexión | Cerrar CONTPAQi durante una transacción | El circuit breaker abre y la transacción va a DLQ recuperable | Pérdida silenciosa de movimientos |
| **G-05** | Decimales | Traspasar `12.345` kg | Se preserva la precisión que la unidad del producto permite | Descuadres acumulativos en el balance de masa |

> **G-01 no es hipotético.** En `ContpaqiSdkGateway.cs:490` y `:513`, cuando `fAltaDocumento` tiene éxito y luego falla `fAltaMovimiento`, el código llama a `FailTransaction` y hace `return` **sin eliminar el documento ya creado**. El resultado es un documento sin movimientos en CONTPAQi que nadie concilia. Con traspasos de WIP esto se vuelve frecuente, porque cada surtido es una transacción multi-línea.

---

## Trazabilidad prueba → decisión de spec

| Decisión / requisito | Pruebas que lo sostienen | Consecuencia si fallan |
| :--- | :--- | :--- |
| **⑥ WIP es almacén en CONTPAQi** (SPEC-008) | A-03, A-04, B-02, B-03, B-04 | WIP pasa a ser ubicación interna de PolyConecta; la regla 7.2 de la arquitectura no cambia |
| **⑤ Re-lotificación / fraccionamiento** (SPEC-007 FR-009) | C-02, C-03, D-03 | Solo se puede reservar el lote completo; hay que repensar la US-4 |
| **⑧ Surtido parcial con backorder** (SPEC-008 US-3) | E-01, E-02, E-03 | El backorder vive solo en PolyConecta (degradación aceptable) |
| **⑩ Devolución con cantidad manual** (SPEC-008 FR-009b) | D-02, D-03, D-05 | La devolución sería total o nada |
| **Visor por clasificación** (SPEC-007 FR-002) | A-05, F-01, F-02, F-03, F-04 | PolyConecta mantiene su propio catálogo de clasificación y una proyección de existencias |
| **Trazabilidad por lote** (transversal) | C-01, C-04, C-05 | PolyConecta reconstruye el linaje por su cuenta |

---

## Orden de ejecución recomendado

1. **A-01 → A-05** — sin esto nada más corre. A-05 puede ejecutarse en paralelo, es solo lectura.
2. **B-01 → B-05** — decide si ⑥ es viable. **Punto de control: si B falla, hay que reunirse antes de seguir.**
3. **C-02 y C-03 primero dentro del bloque C** — son las de mayor probabilidad de cambiar el diseño; conviene saberlo pronto.
4. **D y E** — dependen de que B y C pasen.
5. **F** — independiente de todo lo anterior, solo lectura. Puede ejecutarse desde el día 1 y desbloquea SPEC-007 por separado.
6. **G** — al final, con el flujo completo funcionando.

> **F es independiente de B/C/D.** Si el bloque B falla y WIP no puede ser almacén contable, SPEC-007 sigue siendo implementable: solo necesita leer existencias. Conviene ejecutar F en paralelo desde el inicio para no acoplar el destino de las dos specs.

---

## Registro de resultados

| ID | Fecha | Ejecutó | Resultado | Código de error | Evidencia (SQL / captura) | Notas |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| A-01 | | | | | | |
| A-02 | | | | | | |
| A-03 | | | | | | |
| A-04 | | | | | | |
| A-05 | | | | | | |
| B-01 | | | | | | |
| B-02 | | | | | | |
| B-03 | | | | | | |
| B-04 | | | | | | |
| B-05 | | | | | | |
| C-01 | | | | | | |
| C-02 | | | | | | |
| C-03 | | | | | | |
| C-04 | | | | | | |
| C-05 | | | | | | |
| C-06 | | | | | | |
| C-07 | | | | | | |
| D-01 | | | | | | |
| D-02 | | | | | | |
| D-03 | | | | | | |
| D-04 | | | | | | |
| D-05 | | | | | | |
| E-01 | | | | | | |
| E-02 | | | | | | |
| E-03 | | | | | | |
| F-01 | | | | | | |
| F-02 | | | | | | |
| F-03 | | | | | | |
| F-04 | | | | | | |
| F-05 | | | | | | |
| F-06 | | | | | | |
| G-01 | | | | | | |
| G-02 | | | | | | |
| G-03 | | | | | | |
| G-04 | | | | | | |
| G-05 | | | | | | |
