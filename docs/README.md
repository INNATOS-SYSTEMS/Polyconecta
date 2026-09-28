# Documentación de PolyConecta

La documentación tiene tres capas. Cuando dos documentos se contradicen, **manda el diseño vigente**. Las diferencias con los hechos originales están justificadas en el registro de decisiones.

## 1. Diseño vigente, `diseno/`

La fuente de verdad para construir. Léelo en este orden:

| Documento | Contenido |
| :--- | :--- |
| [01-modulos-y-roles.md](diseno/01-modulos-y-roles.md) | Mapa de módulos, puntos de contacto con CONTPAQi, roles y matriz de permisos |
| [02-flujo-y-reglas.md](diseno/02-flujo-y-reglas.md) | Recorrido del pedido y todas las reglas de negocio: pedido, abastecimiento, fabricación, calidad, recolección y logística |
| [03-almacenes-y-operaciones.md](diseno/03-almacenes-y-operaciones.md) | Almacenes, ubicaciones, tipos de operación, escrituras en CONTPAQi y centros de trabajo |
| [04-modelo-de-dominio.md](diseno/04-modelo-de-dominio.md) | Modelo de entidades objetivo y diferencia con el código actual |
| [05-arquitectura-tecnica.md](diseno/05-arquitectura-tecnica.md) | Proyectos de la solución, estado real y deuda técnica |
| [decisiones.md](diseno/decisiones.md) | Registro de decisiones y qué cambió del informe de validación |
| [preguntas-abiertas.md](diseno/preguntas-abiertas.md) | Lo que falta decidir o verificar |

El orden de construcción está en [PLAN_CONSTRUCCION.md](PLAN_CONSTRUCCION.md).

## 2. Hechos, `assesment/` y `references/`

Registro de lo que dijo y mostró la operación. **No se editan**; si una decisión posterior los contradice, la contradicción se registra en `decisiones.md`.

| Documento | Qué es |
| :--- | :--- |
| [INFORME OPERATIVO AS-IS.md](assesment/INFORME%20OPERATIVO%20AS-IS.md) | Diagnóstico de la operación actual por área |
| [INFORME OPERATIVO TO-BE.md](assesment/INFORME%20OPERATIVO%20TO-BE.md) | Propuesta preliminar del sistema |
| [INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md](assesment/INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md) | Flujo validado con la operación y matriz de 17 ajustes |
| [polyconecta_flujo_tobe_secuencia.html](assesment/polyconecta_flujo_tobe_secuencia.html) | Diagrama de secuencia del TO-BE (generado desde el `.json`) |
| [references/notes/](references/notes/) | Notas de la reunión de descripción de flujos (18-sep-2026) |
| [references/documents/](references/documents/) | CFDI real de referencia (S-26234) |
| [references/](references/) | Referencias de UX de Odoo (smart buttons, vista de lista) y logotipo |
| [mockups/](mockups/) | 19 mockups de pantallas validados con la operación |

## 3. Referencia técnica de CONTPAQi, `contpaq/`

| Documento | Qué es |
| :--- | :--- |
| [Referencia_BD_CONTPAQi.md](contpaq/Referencia_BD_CONTPAQi.md) | Tablas `adm*` de CONTPAQi Comercial Premium |
| [Referencia_SDK_CONTPAQi.md](contpaq/Referencia_SDK_CONTPAQi.md) | Funciones del SDK |
| [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) | Pruebas a ejecutar contra un CONTPAQi real antes de construir WIP, lotes y traspasos |

La constitución del proyecto está en [`.specify/memory/constitution.md`](../.specify/memory/constitution.md).

## Cómo se agrega una especificación nueva

1. Parte del documento de `diseno/` que corresponda y de las preguntas abiertas que la bloquean.
2. Créala con `/speckit-specify`; queda en `.specify/features/NNN-nombre/`.
3. Al terminar la feature, integra en `diseno/` lo que haya cambiado, registra las decisiones y borra la spec. Así `diseno/` sigue siendo la única fuente.
