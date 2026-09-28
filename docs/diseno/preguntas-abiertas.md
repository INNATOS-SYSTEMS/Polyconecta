# Preguntas abiertas

Lo que no se ha decidido y bloquea o condiciona la construcción. Cuando se resuelva una, pásala a [decisiones.md](decisiones.md) y bórrala de aquí.

## Operación y producto

| # | Pregunta | Bloquea |
| :---: | :--- | :--- |
| P-01 | **Autenticación**: ¿usuarios propios de PolyConecta, directorio corporativo (AD/LDAP/SSO) o los de CONTPAQi? | Identidad (Fase 1) |
| P-02 | **Titulares**: ¿quiénes son hoy Comercial, Crédito y Cobranza, Almacenista, Calidad y Tráfico? | Asignación de roles |
| P-03 | **Revocar autorización**: ¿puede cualquiera de los dos firmantes, solo el Administrador, o nadie una vez generados los documentos? | Firmas (Fase 2) |
| P-04 | ¿Alguien ejerce hoy Comercial y Cobranza a la vez? Si existe, la regla RF-4 lo bloquea. | Firmas |
| P-05 | ¿Un Planner puede ver las órdenes de la otra planta en solo lectura, o no debe verlas? | Reglas de fila |
| P-06 | ¿El alcance del Almacenista es por planta o por almacén (MP distinto de PT)? | Reglas de fila |
| P-07 | ¿Sustituir un producto requiere una segunda autorización? | Abastecimiento |
| P-08 | **Suplencias**: si falta el titular de un rol, ¿hay suplente designado o reasigna el Administrador? | Identidad |
| P-09 | ¿La terminal de báscula tendrá login propio para atribuir el pesaje a quien lo captura? | Captura de pesaje |
| P-10 | ¿Quién captura y consulta las incidencias? Hoy es una propuesta de diseño. | Incidencias |
| P-11 | ¿Se mantiene la **importación de componentes desde Excel** (punto 10 del informe) ahora que la lista es plana y editable? | Orden de fabricación |
| P-12 | ¿Cuál es la enumeración definitiva de estados de la orden de fabricación? (el prototipo usa Borrador / Planeado / En progreso / Hecho; la recolección habla de "Lista para producir") | Modelo de dominio |
| P-13 | **Convención de nombres**: prefijo `STC` o `SC` para Santa Cruz, `PIM/Stock/Rollos` o `PIM/Stock/PT`, `TRANSIT/PIM-STC` o `TRANS/PIM-SC` | Catálogo de ubicaciones |
| P-14 | ¿Cuál es el catálogo real de centros de trabajo? (la arquitectura dice `WC-EXT-01`; los mockups, `COEXT-001`) | Planeación |
| P-15 | ¿Qué formato tiene el lote final en Santa Cruz (`IV214-26-C01` en la arquitectura) bajo la convención nueva `R001-IV310-26`? | Numeración |
| P-16 | ¿Cuál es el valor por defecto de la tolerancia del balance de masa? (el código usa 2 %) | Cierre técnico |
| P-17 | **Montemorelos**: la constitución (Principio V) pide disparar una cotización a la Razón Social 2 al elegir esa ruta en la Fase 1, pero difiere el intercompany completo a la Fase 2. ¿Qué entra en la Fase 1? | Logística |
| P-18 | **Tecnología de la presentación**: `docs/sdd/ESPECIFICACION_MIGRACION_ANGULAR_PRESENTATION.md` (28-sep, fuera de esta consolidación) propone replicar la UI en Angular, contra D-29 (Blazor Server). Además supone que la presentación ya consume la API, y hoy no es así. ¿Se adopta? | Presentación |

## Técnicas (se resuelven con la matriz del SDK)

Todas se verifican con [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](../contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) y `tools/sdk-lab` antes de congelar el diseño afectado.

| # | Pregunta | Bloque | Si falla |
| :---: | :--- | :---: | :--- |
| T-01 | ¿El SDK hace traspaso entre almacenes con lote (MP ↔ WIP)? | B | WIP pasa a ser ubicación interna de PolyConecta; se revierte D-22 |
| T-02 | ¿Admite varios lotes por movimiento y fraccionar cantidad? | C-02, C-03 | Solo se reserva el lote completo; un movimiento por lote |
| T-03 | ¿Admite devolución parcial con cantidad manual? | D-02 | Devolución total o nada |
| T-04 | ¿Soporta backorder? | E | El backorder vive solo en PolyConecta |
| T-05 | ¿CONTPAQi clasifica productos? | A-05 | PolyConecta mantiene su propia clasificación |
| T-06 | ¿Leer existencias directo alcanza o hace falta una proyección? | F-04, F-05 | Proyección propia de existencias |
| T-07 | ¿La remisión creada por SDK puede enlazarse al pedido de origen? | nuevo caso | El pedido queda "pendiente de surtir" en CONTPAQi |
| T-08 | Corregir el documento huérfano cuando falla el movimiento | G-01 | — |
| T-09 | ¿Qué motor de persistencia se usa? (recomendación: PostgreSQL, ya referenciado) | ADR | — |

## Interfaz

| # | Pregunta |
| :---: | :--- |
| U-01 | Render agrupado en las listas: las agrupaciones ya se declaran, pero falta dibujarlas. |
| U-02 | Los favoritos por usuario dependen del modelo de usuarios (P-01). |
