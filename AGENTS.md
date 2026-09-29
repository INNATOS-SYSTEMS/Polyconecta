# Guía para agentes

## Fuentes de verdad

- **Diseño vigente**: `docs/diseno/`. Si otro documento lo contradice, manda este.
- **Hechos**: `docs/assesment/` y `docs/references/`. No se editan; una decisión posterior que los contradiga se registra en `docs/diseno/decisiones.md`.
- **Principios**: `.specify/memory/constitution.md`. Solo se enmiendan con aprobación explícita.
- **CONTPAQi**: toda afirmación sobre el SDK o las tablas `adm*` se respalda en `docs/contpaq/` o en documentación oficial verificada (Principio VII). Lo que dependa de la matriz del SDK sin ejecutar es un supuesto, no un hecho.

## Reglas de trabajo

- No escribas directo en tablas `adm*` de CONTPAQi: toda escritura va por outbox y bridge (Principio II).
- Una decisión nueva va a `docs/diseno/decisiones.md`; una duda sin resolver, a `docs/diseno/preguntas-abiertas.md`. No dejes decisiones solo en una spec, un commit o una conversación.
- Las specs de Spec Kit (`.specify/features/`) son temporales: al terminar la feature, integra en `docs/diseno/` lo que haya cambiado y borra la spec.
- Una tarea está hecha cuando se ejecutó y se verificó en navegador o con pruebas, no cuando compila.
- El prototipo en `PolyConecta.Presentation/Services/` es referencia de comportamiento, no de modelo: el modelo es `docs/diseno/04-modelo-de-dominio.md`.
- La interfaz sigue los patrones de Odoo 19 (skills `odoo-design-system` y `odoo-model-philosophy` en `.agents/skills/`).
- La documentación se escribe en español.

## Comandos

```bash
./run.sh                                   # compila, prueba y levanta todo
dotnet build Polyconecta.slnx
dotnet test Polyconecta.slnx
```
