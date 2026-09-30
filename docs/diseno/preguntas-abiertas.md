# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

- Resueltas el 28-sep-2026: P-01 a P-18 y T-09 (D-32 a D-51).
- Resueltas el 29-sep-2026: P-19 (D-74) y P-20 (D-67 a D-73).
- P-02, P-14 y P-16 pasaron a datos de puesta en marcha (D-75) y viven en el [roadmap](../ROADMAP.md).
- I-01 y U-01 eran trabajo planeado, no preguntas: están en el alcance de los módulos del roadmap.
- Resueltas el 30-sep-2026 con la matriz del SDK: T-01 (D-79), T-02 (D-82), T-03 (D-83), T-04 (D-85), T-05 (D-86) y T-08 (D-80). T-06 se acotó (D-87). P-21 (D-90). T-15 (D-95). T-16 (D-106, D-107).

## Infraestructura

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| H-01 | **Hosting de producción** de PolyConecta (API, Angular y SQL Server 2022): servidor propio aparte del de CONTPAQi, el mismo VPS o nube administrada. Debe cumplir CT-05 (instancia separada de CONTPAQi) y tener conectividad con el bridge | Piloto del primer módulo; ambiente de producción (D-77) |
| H-02 | **Respaldos** de la base de PolyConecta. Se define con H-01. Requisito mínimo ya fijado: respaldo completo diario y de logs de transacciones, con una prueba de restauración antes del piloto | Piloto del primer módulo |
| H-03 | **Cómo corre el bridge sin nadie presente** (D-88; pruebas S-03 a S-05). Las credenciales ya se pasan por código: ninguna persona escribe el usuario. Lo que falta saber es si el SDK funciona como **servicio de Windows con una cuenta de usuario real** (perfil cargado), o si exige un escritorio con sesión iniciada; en ese caso, usuario dedicado con inicio de sesión automático. Se investiga primero la vía de servicio (con T-13). Dato: el sincronizador de ARSoftware tampoco corre como servicio. Es un proceso de consola que pide permisos de administrador y se apaga a una hora configurable, lo que apunta a una tarea programada en una sesión iniciada | Cierre integrado de cualquier módulo; piloto |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-06 | ¿La lectura directa refleja al momento un cambio hecho en la UI de CONTPAQi? Falta ejecutar F-05 y cotejar F-01 y F-02 con la UI (D-87) | F-05 | Proyección propia de existencias con estrategia de invalidación |
| T-07 | ¿La remisión creada por SDK puede enlazarse al pedido de origen? | S-13 | El pedido queda "pendiente de surtir" en CONTPAQi |
| T-10 | ¿El SDK puede **dar de alta almacenes**? `docs/contpaq/Referencia_SDK_CONTPAQi.md` no documenta ninguna función para eso; hay que verificarlo en la documentación oficial (Principio VII) | S-09 | Los almacenes se crean a mano en CONTPAQi una sola vez al inicializar, con los códigos que dicta PolyConecta |
| T-11 | Si `Produccion` y `Customers` son almacenes en CONTPAQi (D-43), ¿cómo se registran el consumo y la remisión? (La compra ya no: se registra en CONTPAQi, D-96.) ¿Como traspasos hacia esos almacenes o como documentos de salida y entrada desde el almacén real? Dato de la matriz (B-01): hoy la operación registra el consumo con conceptos "Salida materia prima MAQUINA N", no con traspasos | S-11, S-12 | Se excluyen esas dos ubicaciones de la reserva en `admAlmacenes` |
| T-12 | ¿El SDK da de alta un **pedido** con sus líneas y su **precio unitario y moneda** (D-74), y devuelve su folio, para que la sincronización lo reconozca y no lo vuelva a importar como pedido nuevo (idempotencia por `erp_document_id`)? | S-14 | El pedido libre queda solo interno en PolyConecta hasta resolverlo |
| T-13 | **¿Por qué algunas operaciones del SDK tardan minutos o no terminan?** Se bloquean antes de conectar a SQL, sin ventanas visibles; reiniciar `AppKeyLicenseServer_CONTPAQi` no lo resolvió, y un proceso detenido a la fuerza no libera su sesión (H-6, H-7) | S-01, S-02 | No se cumple la meta de segundos de D-92. Primera hipótesis a medir: el costo está en iniciar el SDK y abrir la empresa en cada operación, y desaparece con la sesión de larga duración (D-91) |
| T-14 | **Verificar A-04 y la creación de almacenes**: hoy la empresa tiene un solo almacén WIP (90). Falta crear `PIM/WIP` y `SC/WIP` y confirmar que coexisten (D-24, D-43) | A-04 | — (se crean por la UI si T-10 falla) |
