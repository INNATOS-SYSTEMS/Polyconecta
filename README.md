# PolyConecta

Motor de ejecución de manufactura y ruteo de inventario para **Polyempaques**: plantas de Apodaca (PIM), Santa Cruz (SC) y Montemorelos (MTM).

PolyConecta lleva el pedido desde CONTPAQi hasta la entrega. Genera las órdenes de fabricación, controla la recolección de materia prima, el pesaje por rollo, la calidad y el balance de masa, y mueve el material entre plantas. **CONTPAQi Comercial Premium sigue siendo el sistema de registro contable**: PolyConecta le escribe solo cuando un documento se valida o se cierra, siempre a través de un bridge con outbox. La experiencia de usuario toma como referencia Odoo 19 Enterprise.

## Estado

El proyecto está al **inicio de la construcción**. Existe un prototipo navegable que demuestra el flujo completo con estado en memoria, un bridge de CONTPAQi funcional y un dominio parcial. El diseño está consolidado y validado con la operación; lo que falta decidir está en [preguntas abiertas](docs/diseno/preguntas-abiertas.md). La construcción avanza módulo a módulo por dos caminos (integración con CONTPAQi y PolyConecta independiente), según la [constitución técnica](docs/diseno/06-constitucion-tecnica.md) y el [roadmap](docs/ROADMAP.md), que tiene el tablero de avance por módulo.

## Por dónde empezar

1. [docs/README.md](docs/README.md): índice de la documentación y cómo se relacionan sus capas.
2. [Módulos y roles](docs/diseno/01-modulos-y-roles.md): el mapa del sistema.
3. [Flujo y reglas](docs/diseno/02-flujo-y-reglas.md): cómo funciona.
4. [Arquitectura técnica](docs/diseno/05-arquitectura-tecnica.md): qué hay en el código y qué deuda tiene.
5. [Constitución](.specify/memory/constitution.md): principios no negociables.

## Estructura

```
PolyConecta.Domain/          Entidades y reglas de negocio, sin dependencias
PolyConecta.Infrastructure/  EF Core, repositorios, outbox
PolyConecta.Api/             API REST (:9020)
PolyConecta.Presentation/    Prototipo Blazor Server (:9000), estado en memoria; se reemplaza por Angular
PolyConecta.Contpaq/         Bridge x86 hacia CONTPAQi (:5005)
tests/                       Pruebas de dominio, integración y bridge
tools/sdk-lab/               Laboratorio para probar el SDK contra un CONTPAQi real
docs/                        Diseño vigente, hechos del assessment y referencia de CONTPAQi
.specify/                    Constitución y plantillas de Spec Kit
scripts/                     Ejecutar, empaquetar y desplegar
```

## Ejecutar

Requiere un SDK de .NET 9 o superior con el runtime ASP.NET Core 8.

```bash
./run.sh                 # compila, prueba y levanta Presentation (:9000) + API (:9020)
./run.sh --with-bridge   # además levanta el bridge de CONTPAQi (:5005)
```
