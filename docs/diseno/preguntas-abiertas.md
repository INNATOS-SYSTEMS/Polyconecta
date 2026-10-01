# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

- Resueltas el 28-sep-2026: P-01 a P-18 y T-09 (D-32 a D-51).
- Resueltas el 29-sep-2026: P-19 (D-74) y P-20 (D-67 a D-73).
- P-02, P-14 y P-16 pasaron a datos de puesta en marcha (D-75) y viven en el [roadmap](../ROADMAP.md).
- I-01 y U-01 eran trabajo planeado, no preguntas: están en el alcance de los módulos del roadmap.
- Resueltas el 30-sep-2026 con la matriz del SDK: T-01 (D-79), T-02 (D-82), T-03 (D-83), T-04 (D-85), T-05 (D-86) y T-08 (D-80). T-06 se acotó (D-87). P-21 (D-90). T-15 (D-95). T-16 (D-106, D-107).
- Resueltas el 1-oct-2026 con el bloque S: T-10 (D-110), T-11 (D-111), T-13 (D-108, D-109). T-07 y T-12 dieron su respuesta técnica y abrieron P-22.

## Infraestructura

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| H-01 | **Hosting de producción** de PolyConecta (API, Angular y SQL Server 2022): servidor propio aparte del de CONTPAQi, el mismo VPS o nube administrada. Debe cumplir CT-05 (instancia separada de CONTPAQi) y tener conectividad con el bridge | Piloto del primer módulo; ambiente de producción (D-77) |
| H-02 | **Respaldos** de la base de PolyConecta. Se define con H-01. Requisito mínimo ya fijado: respaldo completo diario y de logs de transacciones, con una prueba de restauración antes del piloto | Piloto del primer módulo |
| H-03 | **Inicio de sesión automático del bridge** (D-108). Ya se sabe que el bridge no puede ser servicio (S-03): corre en una sesión iniciada de Windows. Falta probar en una ventana de mantenimiento el usuario dedicado con inicio de sesión automático y la tarea que levanta el bridge al iniciar sesión, reiniciando el servidor (S-04). El servidor actual es compartido con SQL Server de otro proyecto | Cierre integrado de cualquier módulo; piloto |

## Ventas

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| P-22 | **¿El pedido se escribe en CONTPAQi?** La operación casi no usa pedidos en CONTPAQi (1 en 2023, 0 en 2024 y 2025, 1 en 2026); su flujo es remisión → factura o factura directa. El SDK sí da de alta un pedido con precio, moneda y tipo de cambio (S-14), pero **no puede ligarle la remisión** (S-13): un pedido escrito por PolyConecta quedaría "pendiente de surtir" para siempre. La remisión creada por PolyConecta sí queda pendiente de facturar, como hoy. Opciones: (a) el pedido vive solo en PolyConecta y en CONTPAQi solo se escribe la remisión (se retira `ALTA_PEDIDO` y la sincronización de pedidos, D-53); (b) se escribe el pedido y alguien lo cierra a mano en CONTPAQi; (c) se mantiene D-53 tal cual | Spec de Ventas (módulo 2) y comando `REMISION` |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-06 | ¿La lectura directa refleja al momento un cambio hecho en la UI de CONTPAQi? Falta ejecutar F-05 y cotejar F-01 y F-02 con la UI (D-87) | F-05 | Proyección propia de existencias con estrategia de invalidación |
| T-14 | **Verificar A-04**: el alta de almacenes por SDK ya funciona y dos almacenes WIP de prueba coexisten (S-09). Falta mover material a ellos para confirmar que aceptan movimientos (D-24, D-43) | A-04 | — (se crean por la UI si T-10 falla) |
