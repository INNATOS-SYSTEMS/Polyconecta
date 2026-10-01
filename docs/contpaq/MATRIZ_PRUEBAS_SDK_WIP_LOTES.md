# MATRIZ DE PRUEBAS TÉCNICAS — SDK CONTPAQi para WIP, Lotes y Traspasos

**Proyecto:** PolyConecta · **Fecha:** 21 de septiembre de 2026 · **Estatus:** Ejecutada el 30-sep-2026 contra `adPOLYEMPAQUES` (ambiente de pruebas del VPS); bloque S (segunda ronda) ejecutado el 1-oct-2026, salvo S-04. Ver el registro al final

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

## Bloque S — Segunda ronda: operación continua y casos pendientes

Agregado el 30-sep-2026, después de la primera ejecución. Cubre las preguntas que quedaron abiertas en [preguntas-abiertas.md](../diseno/preguntas-abiertas.md) y las decisiones D-80, D-88, D-91, D-92 y D-102. Misma regla de verificación: `rc = 0` no basta; toda prueba se confirma por SQL.

| ID | Objetivo | Procedimiento | Criterio de aceptación | Si falla |
| :--- | :--- | :--- | :--- | :--- |
| **S-01** | **Latencia con sesión de larga duración** (T-13, D-91, D-92) | En **un solo proceso**: iniciar sesión y `fSetNombrePAQ` una vez, `fAbreEmpresa` una vez, y ejecutar 20 pares Salida + Entrada seguidos. Medir por separado el inicio del SDK, la apertura de la empresa y cada par | Después del arranque, cada par tarda **segundos** (meta de D-92). El arranque se paga una sola vez | La meta de segundos no es alcanzable: revisar D-92 y dimensionar la cola en minutos |
| **S-02** | Causa de los bloqueos (T-13, H-7) | Repetir S-01 con Comercial abierto y cerrado en la misma sesión, y tras reiniciar el servidor. Registrar en qué llamada ocurre la espera | Se identifica la llamada que bloquea y la condición que lo provoca | Escalar a soporte de CONTPAQi con la evidencia |
| **S-03** | **Bridge como servicio de Windows** (pregunta H-03, D-88) | Registrar `sdklab` como servicio con una **cuenta de usuario real** (no LocalSystem), con perfil cargado, y ejecutar `sdk-open` y un par Salida + Entrada | Funciona sin sesión iniciada en el escritorio | Se descarta el servicio; pasar a S-04 |
| **S-04** | Tarea programada con inicio de sesión automático (pregunta H-03) | Usuario de Windows dedicado con inicio de sesión automático y una tarea "al iniciar sesión" que levanta el proceso. Reiniciar el servidor sin intervención | Tras el reinicio, el proceso queda corriendo y procesa un par sin que nadie toque el servidor | El bridge requiere intervención tras cada reinicio: riesgo operativo a registrar |
| **S-05** | Reinicio diario (D-91, D-98) | Con el proceso de S-03 o S-04 corriendo, provocar el apagado programado y el arranque siguiente | Cierra empresa y SDK (`fTerminaSDK`) y la siguiente sesión arranca sin la lentitud de H-7 | Sesiones colgadas: definir cómo se detectan y se liberan |
| **S-06** | **Referencia corta para reconciliar** (D-80) | Crear un par con `CREFERENCIA` de 20 caracteres derivada de la `idempotency_key` (la llave completa no cabe: el campo mide 20). Buscar el documento por SQL con esa referencia | La referencia se guarda completa y es única y localizable en `admDocumentos` | Guardar la llave en otro campo del documento (observaciones o campo extra) y verificarlo igual |
| **S-07** | Reconciliar un par interrumpido (D-80, G-04) | Crear la Salida, detener el proceso antes de la Entrada, y reintentar con el algoritmo de reconciliación: leer por referencia qué existe y completar solo lo que falta | Queda exactamente una Salida y una Entrada, con las cantidades y el costo esperados | El algoritmo de D-80 no alcanza: rediseñar antes de A-14 |
| **S-08** | Borrar un documento huérfano (D-80, G-01) | Reproducir G-01 (documento sin movimientos) y eliminarlo con `fBuscarDocumento` + `fBorraDocumento` | El documento desaparece o queda cancelado, sin afectar existencias ni folios de otros documentos | Los huérfanos se cancelan en lugar de borrarse; registrar cómo se ven en CONTPAQi |
| **S-09** | **Alta de almacén** (T-10) | La referencia del SDK no documenta ninguna función de alta de almacenes (hay `fInserta*` para productos, clientes y clasificaciones, no para almacenes). Buscarla en la documentación oficial vigente; si existe, probarla con `LAB-ALM` | Existe una función documentada y el almacén aparece en `admAlmacenes` | Los almacenes se crean a mano por la UI al inicializar (contingencia de T-10) |
| **S-10** | Alta de conceptos propios (D-89) | Verificar si `fInsertaConceptoDocto` o equivalente existe; si no, crear por la UI una Salida y una Entrada de PolyConecta y usarlas en un par | Los conceptos propios funcionan en el par, con su propia numeración de folios | Usar los conceptos existentes y aceptar el folio desbordado de H-5 |
| **S-11** | **Consumo desde WIP** (T-11) | Registrar el consumo de un cierre como Salida desde WIP con concepto propio, sin almacén `Produccion`. Comparar con un traspaso WIP → `Produccion` | Queda documentado cuál refleja mejor el consumo en existencias y costo. La operación hoy usa "Salida materia prima MAQUINA N" (B-01) | Decidir con Contabilidad cuál usar |
| **S-12** | Entrada de producción (T-11) | Registrar la entrada de un rollo PT con su lote nuevo y costo, y la de un subproducto de scrap | El lote aparece en `admCapasProducto` del almacén PT con la cantidad y el costo capturados | Bloquea `CIERRE_PRODUCCION` |
| **S-13** | **Remisión ligada al pedido** (T-07) | Crear un pedido de prueba, y una remisión cuyo movimiento lleve como origen el movimiento del pedido (`admMovimientos.CIDMOVTOORIGEN`, "conversión"). Probar por la estructura de alto nivel y, si no lo admite, con `fInsertarMovimiento` + `fSetDatoMovimiento` | `CUNIDADESPENDIENTES` del pedido baja en lo remisionado y el pedido deja de estar pendiente de surtir | El pedido queda "pendiente de surtir" en CONTPAQi (contingencia de T-07) |
| **S-14** | **Alta de pedido libre** (T-12) | `fAltaDocumento` de un pedido con dos líneas, precio unitario y moneda (D-74), y lectura del folio asignado | El pedido existe con sus líneas, precio y moneda, y el folio devuelto permite reconocerlo en la sincronización | El pedido libre queda solo interno (contingencia de T-12) |
| **S-15** | **Compras con lote** (T-16, D-102) | SQL: identificar los conceptos de compra usados en 2026, y si sus movimientos de MP tienen capas con número de lote (`admMovimientosCapas`) | Queda documentado si la MP comprada trae lote, con qué nomenclatura y con qué concepto | Si no trae lote, PolyConecta tendría que lotificar al recibir (ver T-16) |
| **S-16** | Lectura incremental de compras (D-102) | SQL: leer los documentos de compra afectados desde una marca de tiempo o id, dos veces seguidas | La segunda lectura trae solo lo nuevo; no se pierde ni se duplica ninguna compra | Definir otra marca para la lectura incremental |
| **S-17** | **Cerrar el pedido ya remisionado** (P-23, D-113) | Con el pedido de S-14 y la remisión de S-13: (a) fijar por SDK `CUNIDADESPENDIENTES` del movimiento del pedido con `fSetDatoMovimiento`; (b) si no persiste, probar `fSaldarDocumento` o `fCancelaDocumento` sobre el pedido; verificar por SQL y en la UI | El pedido deja de aparecer pendiente de surtir sin afectar la remisión ni existencias | Facturación cierra el pedido a mano, o se acepta el pendiente |

> **S-01 decide la meta de latencia de D-92** y **S-03/S-04 deciden cómo corre el bridge**. Van primero. S-15 y S-16 son solo lectura y pueden correr en paralelo desde el inicio.

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
| **Sesión y latencia del bridge** (D-88, D-91, D-92) | S-01 a S-05 | La meta de segundos se revisa; el bridge requiere intervención tras reinicios |
| **Reconciliación** (D-80) | S-06, S-07, S-08 | Rediseñar la ejecución por pasos antes de corregir el gateway |
| **Inicialización** (D-43, D-89) | S-09, S-10 | Almacenes y conceptos se crean por la UI |
| **Cierre de producción y remisión** (T-07, T-11) | S-11, S-12, S-13 | `CIERRE_PRODUCCION` y `REMISION` sin diseño cerrado |
| **Pedido libre y compras** (T-12, T-16, D-102) | S-14, S-15, S-16 | Pedido libre interno; lotificación de MP en PolyConecta |

---

## Orden de ejecución recomendado

1. **A-01 → A-05** — sin esto nada más corre. A-05 puede ejecutarse en paralelo, es solo lectura.
2. **B-01 → B-05** — decide si ⑥ es viable. **Punto de control: si B falla, hay que reunirse antes de seguir.**
3. **C-02 y C-03 primero dentro del bloque C** — son las de mayor probabilidad de cambiar el diseño; conviene saberlo pronto.
4. **D y E** — dependen de que B y C pasen.
5. **F** — independiente de todo lo anterior, solo lectura. Puede ejecutarse desde el día 1 y desbloquea SPEC-007 por separado.
6. **G** — al final, con el flujo completo funcionando.
7. **S** (segunda ronda): S-15 y S-16 en paralelo (solo lectura) → S-01 y S-02 → S-03 o S-04 → S-05 → S-06 a S-08 → S-09 a S-14. Pendientes para la ventana de mantenimiento: S-04, el movimiento de S-09 y S-17.

> **F es independiente de B/C/D.** Si el bloque B falla y WIP no puede ser almacén contable, SPEC-007 sigue siendo implementable: solo necesita leer existencias. Conviene ejecutar F en paralelo desde el inicio para no acoplar el destino de las dos specs.

---

## Registro de resultados

| ID | Fecha | Ejecutó | Resultado | Código de error | Evidencia (SQL / captura) | Notas |
| :--- | :--- | :--- | :---: | :--- | :--- | :--- |
| A-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | sdk-open → opened=true | Requiere fInicioSesionSDKCONTPAQi(usuario, contraseña) + fSetNombrePAQ("CONTPAQ I COMERCIAL"). Con fInicializaSDK: rc=41719 "No existe último usuario"; sin credenciales abre ventana de autenticación y bloquea |
| A-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=126202 "La ruta de datos no es valida." | sdk-open ruta inexistente, 3 corridas | Código y mensaje estables |
| A-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | admAlmacenes: 90 · WIP; doc 184282 | Ya existía (alta manual por UI) y acepta movimientos |
| A-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | admAlmacenes | Un solo WIP; no hay WIP por planta |
| A-05 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | admClasificaciones 25–30 | "TIPO DE PRODUCTOS" (21 valores, 65 % de productos) separa SEM/PT/MP/INSUMOS; no distingue bolsa / rollo impreso / liso / maestro |
| B-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | admConceptos docDe=34 | Conceptos 36 "Traspasos" y 362021 "Traspasos PT PIM a SC", 0 usos. La operación usa pares Entrada/Salida y "Salida materia prima MAQUINA N" |
| B-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ❌ / ✅ | fAltaDocumento rc=0 (doc 184280, concepto 36, folio 1); fAltaMovimiento rc=132303 "Defina el almacén para consignar productos desde la Configuración General o desde el catálogo de clientes." | spec B-02a; snapshot antes-B02; docs 184281 (Salida, concepto 3535012, folio 297809) y 184282 (Entrada, concepto 34, a WIP 90) | Traspaso nativo ❌: Un traspaso con un solo movimiento (almacén origen) no es posible por la estructura de alto nivel: tMovimiento solo tiene aCodAlmacen. CONTPAQi genera el movimiento destino oculto (CTIPOTRASPASO=3) y busca el destino en la configuración de consignación · Como par Salida + Entrada ✅: Traspaso como par Salida + Entrada. No es atómico: si falla la Entrada, la Salida queda hecha |
| B-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | snapshots antes-B02b / despues-B02b-sal; sql | Cantidad ✅: MP0004 almacén 9 76,250 → 76,240; WIP 0 → 10. Valor ❌ por error del experimento: la Salida valió 201.32 en total (CCOSTOESPECIFICO es el costo TOTAL del movimiento) y la Entrada se capturó con 201.32 como costo UNITARIO (2,013.25). Regla para el bridge: costo unitario de la Entrada = CCOSTOESPECIFICO de la Salida ÷ CUNIDADES. *Corregido el 30-sep; el primer registro decía que el valor se conservaba* |
| B-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | admDocumentos.CAFECTADO=1 | Los documentos de almacén quedan afectados y mueven existencia al crearse, sin fAfectaDocto_Param |
| B-05 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | admMovimientos | Resuelto con el par: cada documento lleva un solo movimiento con su propio almacén (CTIPOTRASPASO=1). El traspaso nativo necesita el destino y la estructura de alto nivel no lo admite |
| C-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | doc 184284 movs 308023/308024; capas 3354/3355 en WIP | Un lote completo por movimiento aparece en WIP con su existencia y costo |
| C-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 (dos fAltaMovimientoSeriesCapas sobre el mismo movimiento) | doc 184283 mov 308022; admMovimientosCapas 29084/29085 | 150 kg en un movimiento: 100 de 250223PT01 + 50 de 090223PT01. Costo por capa exacto: 6,071.824. El gateway actual solo manda un lote por movimiento |
| C-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | diff antes-C02 / despues-C02-sal | Fraccionamiento: 250223PT01 525 → 425; 090223PT01 1,975 → 1,925 |
| C-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | admCapasProducto 3354/3355 | CIDCAPAORIGEN=0: con el par Salida + Entrada CONTPAQi no guarda el linaje entre capas. Lo debe llevar PolyConecta (LotGenealogy) |
| C-05 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | capas 1994 (almacén 9) y 3354 (WIP) | Mismo número de lote en dos almacenes: capas distintas con existencias independientes |
| C-06 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | sumas de capas = unidades | No fue necesario fCalculaMovtoSerieCapa |
| C-07 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ❌ | rc=0 | doc 184285 mov 308025; diff antes-C07 / despues-C07 | CONTPAQi no rechaza: reescribe en silencio las unidades del movimiento a la suma de capas (50 pedidas → 30 registradas). El bridge debe validar antes y verificar después |
| D-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | rc=0 | docs 184286 (Salida de WIP) y 184287 (Entrada a almacén 9) | Cantidad ✅: MP0004 vuelve a 76,250 en el almacén 9 y 0 en WIP. Valor ❌ heredado de B-03: quedan +1,811.92 en el almacén 9 y 1,811.80 en WIP con 0 unidades. Se corrige restaurando la línea base |
| D-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | docs 184288 (Salida de WIP) y 184289 (Entrada a almacén 9) | Devolución de 87.5 kg capturados a mano del lote 250223PT01; WIP queda con 12.5 kg. Valor correcto usando el costo unitario de la capa |
| D-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ❌ | — | diff antes-D02 / despues-D02 | La devolución NO regresa a la capa original (1994, 395 kg): crea la capa 3356 con el mismo número de lote. Cada devolución multiplica capas de un lote; el visor debe agrupar por número de lote |
| D-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=1000090 "Proceso cancelado." | doc 184288; snapshots antes-D04 / despues-D04 | fAfectaDocto_Param(false) falla de forma explícita y no cambia nada. Las cancelaciones se modelan con documento inverso (RF-6) |
| D-05 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | rc=0 | doc 184290 | CONTPAQi acepta sacar 5 kg de un almacén con 0: WIP queda en −5. No valida existencia; PolyConecta y el bridge deben validar antes de enviar (coincide con F-06) |
| E-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | docs 184291–184296 (tres pares LAB-E01) | Tres tandas de 100 kg de MP0010 a WIP: existencia 300; valor salido del almacén 9 = 6,650.03 = valor entrado a WIP. Regla de costo validada: costo unitario de la Entrada = CCOSTOESPECIFICO ÷ CUNIDADES de la Salida (22.1667585, distinto del promedio simple del almacén) |
| E-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | rc=0 | pendientes 3535012 → 0; pendientes 34 → 300 (MP0010, almacén 90) | Mide unidades de documentos CONTPAQi no relacionados con un documento posterior, no el pendiente de una solicitud. El backorder vive en PolyConecta (contingencia T-04) |
| E-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | capas 3354 y 3355 en WIP (bloque C) | Tandas de lotes distintos conviven en WIP con existencias independientes |
| F-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | sql | Existencia = CENTRADASPERIODO12 − CSALIDASPERIODO12 del ejercicio (acumulados que incluyen saldo inicial). Falta cotejo con la UI |
| F-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | sql | 2,319 capas de lote con existencia. Falta cotejo con la UI |
| F-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | sql | 222/223 pares producto-almacén cuadran con la suma de lotes. Excepción: NA321210 en MP PIM (399,450 vs 424,450) |
| F-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | — | tiempo | Existencias de un almacén completo: indistinguible de SELECT 1 (~5.5 s, todo de SSH) |
| F-05 | — | — | ⏳ | — | — | Requiere modificar una existencia desde la UI de CONTPAQi y volver a consultar; pendiente de ejecución manual |
| F-06 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ⚠️ | — | sql | 4,025 existencias negativas en 3,706 productos; concentradas en Almacen Uno (3,511, −641 M) y Almacen PT (301, −178 M). Ninguna capa de lote negativa |
| G-01 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ❌ | — | doc 184280 sin movimientos | Comprobado en la práctica: tras fallar fAltaMovimiento queda un documento huérfano (orphanDocumentLeftBehind=true) |
| G-02 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 en ambos | docs 184298 y 184299 (LAB-G02) | El SDK no es idempotente: el mismo documento enviado dos veces se crea dos veces. La idempotency_key del bridge es indispensable |
| G-03 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | A rc=0; B fAltaMovimiento rc=28 "La operación no es aplicable." | doc 184301; capa 3355 50 → 10 | Dos Salidas simultáneas de 40 kg sobre un lote de 50: una gana y la otra falla sin dejar documento ni negativos. El código 28 es genérico |
| G-04 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ❌ | — | doc 184300: 32 de 40 movimientos, afectado | Interrumpir el proceso deja el documento a medias y con existencias ya movidas. Sin transacción: al reintentar, el bridge debe reconciliar lo escrito antes de reenviar |
| G-05 | 2026-09-30 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | doc 184297 | 12.345 kg se guardan sin redondeo (capturadas y registradas); WIP 300 − 12.345 = 287.655 |
| S-01 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | docs 184303–184342 (`LAB-S01-NN-S/E`); snapshots antes-S01b / despues-S01b | **20 pares Salida + Entrada en una sola sesión, en 47 s.** Arranque de la sesión, una vez: iniciar SDK 3.7 s + abrir empresa 3.5 s. Par: promedio **2.3 s** (mín. 2.2, máx. 3.1; el primero es el más lento). `fAltaDocumento` ≈ 0.85 s, `fAltaMovimiento` ≈ 0.2–0.3 s. Almacén 9: 49,700 → 49,600; WIP: 253.655 → 353.655; valor salido 2,216.6758 = valor entrado 2,216.676. Primer intento (madrugada): ❌ bloqueado 30 min por la causa de S-02 |
| S-02 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | — | bitácora en vivo (`evidence/progress.log`) y enumeración de ventanas durante el bloqueo | **Causa de los bloqueos (H-6, H-7): ventanas de ingreso que nadie ve.** La empresa pide **dos** inicios de sesión: (1) usuario de **Comercial** (`SUPERVISOR`) con `fInicioSesionSDK` **antes** de `fSetNombrePAQ`; sin él, `fSetNombrePAQ` abre "Ingreso a CONTPAQi COMERCIAL" (clase `TDlgLoginUsuario`) y espera. (2) usuario **centralizado** (`USUARIO`) con `fInicioSesionSDKCONTPAQi` **después** de `fSetNombrePAQ`; sin él, `fAbreEmpresa` espera a la ventana "Ingreso a CONTPAQi" del proceso `SDKCONTPAQNG`, necesaria porque la empresa está ligada a Contabilidad. Con los dos, contraseñas vacías, no aparece ninguna ventana. La primera ronda usaba solo el segundo, antes de `fSetNombrePAQ`, y las esperas de minutos eran la ventana pendiente |
| S-03 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ❌ | S4U: `fAbreEmpresa` rc=110117 "No se pudo establecer la sesión de usuario en CONTPAQi CONTABILIDAD…"; SYSTEM: sin respuesta | tareas `PolyConecta-sdklab-s4u` y `-sys` (ya quitadas) | **El bridge no puede correr como servicio.** Sin escritorio (S4U, como un servicio con cuenta de usuario) el inicio de sesión de Contabilidad falla con error explícito; como SYSTEM, `SDKCONTPAQNG` arranca en la sesión 0 y se queda esperando su ventana. Solo funciona en una sesión iniciada de Windows (D-88) |
| S-04 | — | — | ⏳ | — | — | Para la ventana de mantenimiento (requiere reiniciar el servidor compartido). Procedimiento en `tools/sdk-lab/EJECUCION_EN_VPS.md`, sección "Ventana de mantenimiento" |
| S-05 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | probe9, s01b y las corridas S-06 a S-14 | Sesiones consecutivas arrancan en ~7 s y cierran limpio con `fTerminaSDK`. Un proceso detenido a la fuerza a mitad del inicio de sesión deja vivo `SDKCONTPAQNG` con su ventana pendiente: hay que cerrarlo antes de reintentar |
| S-06 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | doc 184343 | `CREFERENCIA` de 20 caracteres (`LAB-S06-0123456789AB`) se guarda completa en el documento y el movimiento, y se localiza por SQL. **El SDK devuelve folio 0 en la estructura**: el folio real (297844) se lee por SQL tras crear el documento |
| S-07 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | docs 184344 (`LAB-S07-01-S`) y 184345 (`LAB-S07-01-E`) | Reconciliación de un par interrumpido: el reintento lee por referencia, encuentra la Salida sin Entrada, toma el costo unitario de la Salida (44.33352 ÷ 2) y crea solo la Entrada. Un segundo reintento no encuentra nada que hacer. Queda una Salida y una Entrada con el mismo valor |
| S-08 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | alta: `fAltaMovimiento` rc=120115 "El Almacén solicitado no existe."; borrado: rc=0 | doc 184346 (huérfano provocado) | `fBuscarDocumento` + `fBorraDocumento` eliminan el documento sin movimientos. Su folio (297846) queda como hueco. CONTPAQi asigna el siguiente id como máximo + 1, así que el id borrado se reutilizó en S-11 |
| S-09 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ⚠️ | rc=0 | `admAlmacenes` 20 (`LAB-WIP-PIM`) y 21 (`LAB-WIP-SC`) | **Sí hay alta de almacenes por SDK**: `fInsertaAlmacen` → `fSetDatoAlmacen` (código, nombre) → `fGuardaAlmacen`. No están en nuestra referencia; la DLL 11.5.1.0 las exporta y ARSoftware.Contpaqi.Comercial las usa en ese orden. La fecha de alta queda vacía (1899-12-30): hay que fijar `CFECHAALTAALMACEN`. Falta mover material a los almacenes nuevos (no se ejecutó) |
| S-10 | 2026-10-01 | Claude (exports de MGWServicios.dll) | ❌ | — | exports de la DLL | No existe función para dar de alta conceptos (`fInsertaConceptoDocto` no está exportada; solo `fEditaConceptoDocto`, `fSetDatoConceptoDocto` y `fGuardaConceptoDocto`). Los conceptos propios se crean por la UI de CONTPAQi |
| S-11 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | doc 184346 (`LAB-S11-CONSUMO`) | El consumo se registra como **Salida desde WIP** con el concepto que ya usa la operación (353 "Salida materia prima MAQUINA 3"): 5 kg al costo de WIP (22.16676/kg), afectado. No hace falta un almacén `Produccion` |
| S-12 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | rc=0 | doc 184347; capa 3357 | La entrada de producción (concepto 34, Almacén PT) crea la capa del lote nuevo `LAB-R001-S12` con 25 kg y el costo capturado (30) |
| S-13 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ⚠️ / ❌ | remisión rc=0; liga: todas rc=0 sin efecto | doc 184349 (remisión `3SIVA4`, folio 5083) | La remisión creada por SDK queda **pendiente de facturar** (`CUNIDADESPENDIENTES` = 10), igual que una capturada a mano: Facturación la convierte en factura en CONTPAQi ✅. **Ligarla a un pedido no es posible**: `fBuscarIdDocumento` + `fBuscarIdMovimiento` + `fEditarMovimiento` + `fSetDatoMovimiento("CIDMOVTOORIGEN")` + `fGuardaMovimiento` devuelven 0 y no guardan nada (sin posicionar antes el documento, `fBuscarIdMovimiento` termina en violación de acceso). Dato de la operación: en CONTPAQi casi no hay pedidos (1 en 2023, 0 en 2024 y 2025, 1 en 2026); el flujo real es remisión → factura (1,161 movimientos de factura convertidos de remisión en 2026) y factura directa |
| S-14 | 2026-10-01 | Claude (sdklab por tarea interactiva) | ✅ | línea 1 rc=0; línea 2 rc=135617 "El producto está inactivo…" | doc 184348 (pedido concepto 2, folio 3) | El pedido se da de alta con cliente, moneda (dólar, `CIDMONEDA` 2), tipo de cambio 18.5 y precio unitario; CONTPAQi calcula impuestos (total 0.58 con IVA). La segunda línea falló por producto inactivo: el bridge debe validar que el producto esté activo. Que un documento acepte varias líneas ya lo mostró G-04 |
| S-15 | 2026-09-30 | Claude (sdklab sql por SSH) | ⚠️ | — | sql sobre admConceptos / admMovimientos / admMovimientosCapas / admCapasProducto, 2026 | Las compras son `CIDDOCUMENTODE = 19` con 35 conceptos "CONTRARECIBO …" (3,037 documentos en 2026). **La materia prima no trae lote**: ningún producto `MP%` tiene capas de lote en `admCapasProducto`, y ninguna compra de MP de 2026 lleva capa de lote. **La resina comprada entra casi toda al almacén "Gastos"** (20 movimientos, 428,775 unidades, "PIM CONTRARECIBO MATERIAS PRIMAS"); solo 2 movimientos entraron a "Almacen Materia Prima PIM" (enero). La existencia de MP está repartida en 9 almacenes: Gastos 2.39 M, Almacen Materia prima 2.18 M, Almacen Materia Prima PIM 0.56 M, entre otros |
| S-16 | 2026-09-30 | Claude (sdklab sql por SSH) | ⚠️ | — | sql sobre admDocumentos (compras 2026) | `CFECHA` no sirve de marca incremental: 1,037 de 3,037 compras tienen un id mayor que la anterior y una fecha menor (captura con fecha atrasada; por ejemplo, fecha 31-jul capturada el 9-sep). `CIDDOCUMENTO` crece con la captura y sirve para detectar documentos nuevos; los cambios posteriores (edición, cancelación) se detectan con `CTIMESTAMP`, que es texto `MM/dd/yyyy HH:mm:ss:fff` y hay que convertir a fecha para compararlo. Falta repetir la lectura con compras nuevas |

### Entorno de la ejecución

Windows Server 2022 (20348) · CONTPAQi Comercial 11.5.1.0 · SDK `MGWServicios.dll` 11.5.1.0 · empresa `adPOLYEMPAQUES` (ambiente de pruebas; línea base `adPOLYEMPAQUES.baseline.bak`, 30-sep 18:51) · documentos de prueba 184280–184301, referencias `LAB-*`. Pendientes de cotejo manual en la UI: F-01, F-02, F-05. A-04 requiere un WIP por planta.

### Hallazgos fuera de la matriz
- H-1 · El interop usaba MGW_SDK.dll (Factura Electrónica); la de Comercial es MGWServicios.dll. Corregido en ContpaqiSdkNative.cs.
- H-2 · El SDK solo funciona en la sesión interactiva; por SSH o servicio se bloquea sin error.
- H-3 · El bridge no inicia sesión (fInicioSesionSDKCONTPAQi) y usa fInicializaSDK: contra esta empresa fallaría o quedaría bloqueado.
- H-4 · Las estructuras tDocumento, tMovimiento y tSeriesCapas del interop no correspondían a la referencia (tamaños de cadena, aSistemaOrigen double, aObservaciones inexistente, orden de tSeriesCapas). fAltaDocumento fallaba con rc=130241 "El concepto del documento es obligatorio". Corregidas en ContpaqiSdkNative.cs.
- H-5 · El concepto 34 "Entrada al Almacén" tiene un folio capturado en 2022 (211,912,336); el siguiente folio generado fue 211,912,349,212,714. PolyConecta debería usar conceptos propios para sus Salidas y Entradas.
- H-6 · La primera Salida por SDK tardó varios minutos en completarse; la Entrada, segundos. Pendiente saber si apareció una ventana en la sesión.
- H-7 · Algunas operaciones por SDK tardan varios minutos, o no terminan: se bloquean ANTES de conectar a SQL (ninguna sesión en adPOLYEMPAQUES durante el bloqueo; sin ventanas en la sesión; Comercial cerrado). Procesos detenidos a la fuerza no liberan su sesión del SDK. Reiniciar AppKeyLicenseServer_CONTPAQi no eliminó la lentitud (siguiente operación: 214 s). El despachador del bridge necesita timeouts largos y siempre cerrar la sesión (fTerminaSDK).
- H-8 · El documento 184302 (concepto 36 "Traspasos", folio 2, sin referencia ni movimientos, creado el 30-sep a las 23:53) no lo creó la matriz: es un huérfano de otro origen.
