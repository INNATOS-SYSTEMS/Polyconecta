# Contrato: scripts de verificación

Interfaz de los scripts de `PolyConecta.Web/package.json` que dan la feature por hecha (FR-017 a FR-019). Las tareas los implementan; este documento fija qué hacen y qué devuelven.

## `npm run build`

Compila en modo producción. Termina con código 0 y sin errores.

## `npm test`

Corre las pruebas unitarias (`ng test`, research R-06) una vez, sin modo observador. Cubre las reglas de [data-model.md §3](../data-model.md#3-reglas-que-se-prueban-fr-008-fr-012). Termina con código 0 si todas pasan.

## `npm run parity`

| | |
| :--- | :--- |
| Entrada | Opcional: `-- --routes=/pedidos,/fabricacion/BOL-2026-0001` para correr solo esas rutas (cada agente de la fase 3 verifica las suyas) |
| Prepara | Reutiliza Blazor en `:9000` y Angular en `:4200` si responden. Si no, los levanta y los apaga al terminar. La API **no** se levanta |
| Hace | Por cada ruta de FR-004, abre las dos aplicaciones en Chromium a 1600×900, espera las fuentes y el render, oculta el botón "Nuevo" con una máscara y compara las capturas con `pixelmatch` |
| Rutas | Las 19 de FR-004. Las rutas con folio usan un folio de la semilla (`/pedidos/IV310-26`, `/fabricacion/BOL-2026-0001`, `/traslados/PIM/OUT/48213`…), y la lista exacta se fija en las tareas |
| Salida | `PolyConecta.Web/parity-report/index.html`, con la captura de cada lado, el mapa de diferencias y el porcentaje por ruta. El reporte lo ignora git |
| Éxito | Código 0 si todas las rutas quedan en ≤ 1 % de píxeles distintos |
| Falla | Código distinto de 0. Indica la ruta y el porcentaje, o el motivo si no cargaron las fuentes o el CDN |
| Excepciones | Una diferencia legítima se documenta en `parity-report/excepciones.md` con su captura. No se sube el umbral (regla de autonomía 8) |

## `npm run scenarios`

| | |
| :--- | :--- |
| Hace | Corre los guiones de US-2 y US-3 contra las dos aplicaciones y compara los textos de cada punto de control (FR-018) |
| Guion | `e2e/scenarios/<nombre>.scenario.ts`: lista de pasos (`ir`, `pulsar`, `capturar`, `elegir lote`) y de puntos de control, cada uno con un selector de contenedor cuyo texto visible se compara |
| Guiones mínimos | Los escenarios 1 a 6 de US-2. Los de US-3 (modo libre) solo corren contra Angular, porque Blazor no tiene "Nuevo" |
| Éxito | Código 0 si los textos coinciden en todos los puntos de control |

## `npm run chatter`

Prueba de US-4 con dos pestañas de Angular y la API corriendo. Mide que el mensaje llegue en menos de 1 s (SC-005). Con la API apagada, verifica que el panel muestre "Sin conexión en vivo" y que el mensaje se agregue local.
