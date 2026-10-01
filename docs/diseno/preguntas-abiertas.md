# Preguntas abiertas

Lo que no se ha decidido o verificado y condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

- Resueltas el 28-sep-2026: P-01 a P-18 y T-09 (D-32 a D-51).
- Resueltas el 29-sep-2026: P-19 (D-74) y P-20 (D-67 a D-73).
- P-02, P-14 y P-16 pasaron a datos de puesta en marcha (D-75) y viven en el [roadmap](../ROADMAP.md).
- I-01 y U-01 eran trabajo planeado, no preguntas: están en el alcance de los módulos del roadmap.
- Resueltas el 30-sep-2026 con la matriz del SDK: T-01 (D-79), T-02 (D-82), T-03 (D-83), T-04 (D-85), T-05 (D-86) y T-08 (D-80). T-06 se acotó (D-87). P-21 (D-90). T-15 (D-95). T-16 (D-106, D-107).
- Resueltas el 1-oct-2026 con el bloque S: T-10 (D-110), T-11 (D-111), T-13 (D-108, D-109). T-14 se cerró en la ventana de mantenimiento (S-09). T-07 y T-12 dieron su respuesta técnica; P-22 se resolvió con D-113 y abrió P-23, que se resolvió con D-114 (por validar).

## Infraestructura

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| H-01 | **Hosting de producción** de PolyConecta (API, Angular y SQL Server 2022): servidor propio aparte del de CONTPAQi, el mismo VPS o nube administrada. Debe cumplir CT-05 (instancia separada de CONTPAQi) y tener conectividad con el bridge | Piloto del primer módulo; ambiente de producción (D-77) |
| H-02 | **Respaldos** de la base de PolyConecta. Se define con H-01. Requisito mínimo ya fijado: respaldo completo diario y de logs de transacciones, con una prueba de restauración antes del piloto | Piloto del primer módulo |
| H-03 | **Inicio de sesión de Contabilidad en el usuario del bridge** (D-108). El inicio de sesión automático ya funciona (S-04): tras reiniciar, `polyconecta-bridge` entra solo y la tarea corre sin intervención. Pero en su sesión `fAbreEmpresa` falla al iniciar sesión en Contabilidad (rc=110117), mientras que en la del administrador funciona. Falta saber qué tiene el administrador que el usuario nuevo no tiene: un primer ingreso interactivo a CONTPAQi, o el usuario y contraseña correctos de Contabilidad. Si no se resuelve, el bridge corre en la sesión del administrador | Cierre integrado de cualquier módulo; piloto |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-06 | ¿La lectura directa refleja al momento un cambio hecho en la UI de CONTPAQi? Falta ejecutar F-05 y cotejar F-01 y F-02 con la UI (D-87) | F-05 | Proyección propia de existencias con estrategia de invalidación |
