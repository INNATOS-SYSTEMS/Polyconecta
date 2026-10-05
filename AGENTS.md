# Guía para agentes

## Fuentes de verdad

- **Diseño vigente**: `docs/diseno/`. Si otro documento lo contradice, manda este.
- **Hechos**: `docs/assesment/` y `docs/references/`. No se editan; una decisión posterior que los contradiga se registra en `docs/diseno/decisiones.md`.
- **Principios**: `.specify/memory/constitution.md`. Solo se enmiendan con aprobación explícita.
- **Reglas de construcción**: `docs/diseno/06-constitucion-tecnica.md` (reglas `CT-NN`). El avance por fase del plan de trabajo está en `docs/ROADMAP.md`.
- **CONTPAQi**: toda afirmación sobre el SDK o las tablas `adm*` se respalda en `docs/contpaq/` o en documentación oficial verificada (Principio VII). Lo que dependa de la matriz del SDK sin ejecutar es un supuesto, no un hecho.

## Reglas de trabajo

- No escribas directo en tablas `adm*` de CONTPAQi: toda escritura va por outbox y bridge (Principio II).
- Una decisión que cambie el diseño vigente o el contrato va a `docs/diseno/decisiones.md`; una duda sin resolver, a `docs/diseno/preguntas-abiertas.md`. No dejes decisiones solo en una spec, un commit o una conversación.
- Una fase del plan es una sola spec (CT-34, CT-43). Lo que surja al construirla se registra en su sección "Exploración y cambios" y se hace en esa misma spec: un cambio menor no abre otra.
- Cada spec se trabaja en su propia rama (`NNN-<fase>`) y cualquier otro cambio, en una rama propia. Nunca hagas commit directo en `main`: solo se integra a `main` lo que ya está terminado y verificado (CT-44).
- Las specs de Spec Kit (`.specify/features/`) son temporales: al terminar la fase, integra en `docs/diseno/` lo que haya cambiado y borra la spec.
- Una tarea está hecha cuando se ejecutó y se verificó en navegador o con pruebas, no cuando compila.
- Todo documento se diseña con sus dos modos: ligado (lo genera otro documento) y libre ("Nuevo", sin origen). El origen nunca es precondición (Principio X de la constitución).
- El prototipo en `PolyConecta.Presentation/Services/` es referencia de comportamiento, no de modelo: el modelo es `docs/diseno/04-modelo-de-dominio.md`.
- La interfaz sigue los patrones de Odoo 19 (skills `odoo-design-system` y `odoo-model-philosophy` en `.agents/skills/`).
- La documentación se escribe en español.

## Comandos

```bash
./run.sh                                   # compila, prueba y levanta todo
dotnet build Polyconecta.slnx
dotnet test Polyconecta.slnx
```
