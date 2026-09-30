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
| [06-constitucion-tecnica.md](diseno/06-constitucion-tecnica.md) | Reglas de construcción: topología, stack, capas, datos y paridad, contrato del bridge, pruebas y cierre de módulos |
| [decisiones.md](diseno/decisiones.md) | Registro de decisiones y qué cambió del informe de validación |
| [preguntas-abiertas.md](diseno/preguntas-abiertas.md) | Lo que falta decidir o verificar |

El orden de construcción, por módulos y caminos, con su tablero de avance, está en [ROADMAP.md](ROADMAP.md).

## 2. Hechos, `assesment/` y `references/`

Registro de lo que dijo la operación. **No se editan**; si una decisión posterior los contradice, la contradicción se registra en `decisiones.md`.

| Documento | Qué es |
| :--- | :--- |
| [INFORME OPERATIVO AS-IS.md](assesment/INFORME%20OPERATIVO%20AS-IS.md) | Diagnóstico de la operación actual por área |
| [INFORME OPERATIVO TO-BE.md](assesment/INFORME%20OPERATIVO%20TO-BE.md) | Propuesta preliminar del sistema |
| [INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md](assesment/INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md) | Flujo validado con la operación y matriz de 17 ajustes |
| [references/poly_empaques_logo_blanco.svg](references/poly_empaques_logo_blanco.svg) | Logotipo |

Los mockups de pantallas, el diagrama de secuencia generado, las capturas de UX de Odoo, las notas de la reunión del 18-sep y el CFDI de referencia (S-26234) se retiraron el 28-sep-2026. Lo que se decidió a partir de ellos está en `diseno/` (D-01 a D-13), y siguen en el historial de git.
## 3. Referencia técnica de CONTPAQi, `contpaq/`

| Documento | Qué es |
| :--- | :--- |
| [Referencia_BD_CONTPAQi.md](contpaq/Referencia_BD_CONTPAQi.md) | Tablas `adm*` de CONTPAQi Comercial Premium |
| [Referencia_SDK_CONTPAQi.md](contpaq/Referencia_SDK_CONTPAQi.md) | Funciones del SDK |
| [MATRIZ_PRUEBAS_SDK_WIP_LOTES.md](contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) | Pruebas contra un CONTPAQi real de WIP, lotes y traspasos, con sus resultados del 30-sep-2026 (decisiones D-79 a D-89) |

La constitución del proyecto está en [`.specify/memory/constitution.md`](../.specify/memory/constitution.md).

## Cómo se agrega una especificación nueva

1. Parte del documento de `diseno/` que corresponda y de las preguntas abiertas que la bloquean.
2. Créala con `/speckit-specify`; queda en `.specify/features/NNN-nombre/`.
3. Al terminar la feature, integra en `diseno/` lo que haya cambiado, registra las decisiones y borra la spec. Así `diseno/` sigue siendo la única fuente.
