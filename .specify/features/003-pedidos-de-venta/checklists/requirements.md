# Lista de calidad de la spec: Pedidos de venta (F1)

**Propósito**: validar que la spec está completa antes de `/speckit-clarify` o `/speckit-plan`
**Creada**: 2026-10-08
**Spec**: [spec.md](../spec.md)

## Contenido

- [x] Enfocada en el valor para la operación (pedido, firmas, catálogos)
- [x] Secciones obligatorias completas
- [~] Sin detalles de implementación: la spec nombra entidades, rutas del contrato y piezas de la base común, como la de F0. Es deliberado: las fases comparten un modelo y un contrato fijados en `docs/diseno/` y los requisitos los citan

## Completitud

- [x] Sin marcadores `[NEEDS CLARIFICATION]`: el alcance lo decidió el usuario (D-145)
- [x] Requisitos verificables y sin ambigüedad
- [x] Criterios de éxito medibles
- [x] Escenarios de aceptación por historia
- [x] Casos límite identificados
- [x] Alcance acotado (tabla Dentro / Fuera)
- [x] Dependencias y supuestos identificados

## Lista para el plan

- [x] Cada requisito tiene su escenario o criterio
- [x] Las historias cubren el flujo principal (pedido libre con dos firmas)
- [ ] Ratificada por los dos líderes, con el contrato `1.1` (FR-003)

## Notas

- Pendiente de ratificar: la propuesta de contrato `1.1` (clasificación, moneda y domicilios). D-146 la validó el usuario el 8-oct.
- P-28 se resuelve en el plan, no en la spec.
