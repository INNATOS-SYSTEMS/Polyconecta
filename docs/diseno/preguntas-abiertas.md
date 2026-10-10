# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

- Resueltas el 28-sep-2026: P-01 a P-18 y T-09 (D-32 a D-51).
- Resueltas el 29-sep-2026: P-19 (D-74) y P-20 (D-67 a D-73).
- P-02, P-14 y P-16 pasaron a datos de puesta en marcha (D-75) y viven en el [roadmap](../ROADMAP.md).
- I-01 y U-01 eran trabajo planeado, no preguntas: están en el alcance de los módulos del roadmap.
- Resueltas el 30-sep-2026 con la matriz del SDK: T-01 (D-79), T-02 (D-82), T-03 (D-83), T-04 (D-85), T-05 (D-86) y T-08 (D-80). T-06 se acotó (D-87). P-21 (D-90). T-15 (D-95). T-16 (D-106, D-107).
- Resueltas el 1-oct-2026 con el bloque S: T-10 (D-110), T-11 (D-111), T-13 (D-108, D-109). T-14 se cerró en la ventana de mantenimiento (S-09). H-03 se resolvió con D-115. T-07 y T-12 dieron su respuesta técnica; P-22 se resolvió con D-113 y abrió P-23, que se resolvió con D-114 (por validar).
- Resuelta el 5-oct-2026: P-24 (D-124). Abiertas ese día: T-17 y P-25 (D-127).
- Abiertas el 6-oct-2026: H-04 (D-129) y P-26 (D-132). T-17 se difirió en el contrato `1.0` como campo opcional.

## Diseño

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| P-25 | **De dónde salen los kg de los cálculos internos.** D-127 quitó el peso en kg capturado aparte: cada línea lleva una sola cantidad en la unidad base de CONTPAQi. El balance de masa (Principio IV), los componentes, la planeación, la meta de producción (`target_production_kg`), el scrap y el registro dual de bolseo (02 §3 y §4) se expresan en kg. Hay que decidir cómo se obtienen cuando la unidad base no es KG: un factor de la ficha técnica, capturar el peso solo en ciertos documentos (pesaje de rollos, bolseo, scrap) o dar de alta en KG los productos que entran al balance | Balance de masa (F5); captura de producción (F4); componentes y planeación (F2) |
| P-26 | **Precio de la remisión.** El SDK no liga la remisión al pedido (S-13), así que CONTPAQi no toma solo el precio del pedido. ¿La remisión lleva precio por línea, copiado del pedido en PolyConecta, o Facturación lo captura al facturar? El contrato `1.0` ya acepta `precio` opcional por línea (D-132) | `REMISION` (F6, tarea 6.1) |
| P-27 | **La incidencia como documento.** El Principio X la cuenta entre los documentos creables con "Nuevo", pero hoy es un renglón de captura sin formulario, estados ni chatter. ¿Qué campos, estados y reglas tiene como documento, y quién la crea y la cierra? La spec 011 (P2) la deja como está, con la lista nueva y un kanban por centro de trabajo | F4 (Captura de producción) |
| P-28 | **Contrato HTTP de la consulta de listas.** La forma de `OrigenDeLista<T>` quedó fija en la spec 011 ([07 §4.1](07-contratos-visuales.md#41-origen-de-datos-de-lista)). ¿Qué ruta y formato tiene en la API (parámetros de consulta o cuerpo, nombres de campos, paginación de grupos)? Se fija con la primera lista que lea de la API | F1 |
| P-29 | **Acciones contextuales por documento.** La selección ofrece "Acciones" con "Exportar" y las que declare cada pantalla (D-167); Usuarios y Grupos ya archivan y restauran. ¿Cuáles aplican a cada documento (archivar, imprimir, validar en lote) y qué registros se pueden **eliminar**? 04 §1 dice que lo referenciado se archiva y no se borra, así que "Eliminar" queda en el contrato y en la galería, pero ninguna lista de F1 la ofrece hasta decidir, por ejemplo, si un pedido en Borrador que nunca se envió a CONTPAQi se borra. Lo decide cada fase al construir su lista | F1 a F8 |
| P-30 | **Origen del tipo de cambio de `ALTA_PEDIDO`.** PolyConecta no captura tipo de cambio (D-161), pero el contrato `1.0` exige `tipo_cambio` cuando la moneda no es la base ([bridge-v1 §ALTA_PEDIDO](../contratos/bridge-v1.md)). ¿Lo toma el bridge del tipo de cambio del día en CONTPAQi, lo lee PolyConecta de CONTPAQi y lo manda, o el campo se vuelve opcional en el contrato? Se acuerda entre los dos líderes (CT-22). | Pedidos en moneda distinta de la base (`ALTA_PEDIDO`, F2) |

## Infraestructura

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| H-01 | **Hosting de producción** de PolyConecta (API, Angular y SQL Server 2022): servidor propio aparte del de CONTPAQi, el mismo VPS o nube administrada. Debe cumplir CT-05 (instancia separada de CONTPAQi) y tener conectividad con el bridge | Piloto del primer módulo; ambiente de producción (D-77) |
| H-02 | **Respaldos** de la base de PolyConecta. Se define con H-01. Requisito mínimo ya fijado: respaldo completo diario y de logs de transacciones, con una prueba de restauración antes del piloto | Piloto del primer módulo |
| H-04 | **Cómo rotar la contraseña de `sa` sin romper CONTPAQi.** CONTPAQi Comercial se conecta con `sa` (D-129). Falta el procedimiento oficial para cambiarla en su configuración a la vez que en SQL Server, y saber qué más usa `sa` en el servidor (respaldos, otras aplicaciones). Lo confirman Sistemas o el distribuidor de CONTPAQi. Mientras tanto la contraseña anterior sigue en el historial de git (D-51) | La rotación de `sa` (tarea 0.1 de F0) |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-17 | **Costo de la Entrada en `TRASPASO`.** D-79 fija la regla (`CCOSTOESPECIFICO ÷ CUNIDADES` de la Salida). Falta decidir quién la aplica: el bridge al crear la Entrada, leyendo la Salida recién creada, o PolyConecta con un costo que manda en la carga. También, si el resultado del comando devuelve el costo a PolyConecta En el contrato `1.0` el resultado lleva `costo` opcional mientras se decide (D-132) | Contrato | Se define en el contrato antes de implementar `TRASPASO` (F3) |
| T-06 | ¿La lectura directa refleja al momento un cambio hecho en la UI de CONTPAQi? Falta ejecutar F-05 y cotejar F-01 y F-02 con la UI (D-87) | F-05 | Proyección propia de existencias con estrategia de invalidación |
