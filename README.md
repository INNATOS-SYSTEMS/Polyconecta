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
PolyConecta.Domain/          Entidades, base común (mixins, estados, sincronización) y reglas, sin dependencias
PolyConecta.Application/     Casos de uso con decoradores y puertos (folios, outbox hacia el bridge)
PolyConecta.Infrastructure/  EF Core con SQL Server 2022, migraciones, despachador hacia el bridge
PolyConecta.Api/             API REST (:9200), callback del bridge y hub del chatter
PolyConecta.Web/             Réplica 1:1 del prototipo en Angular 22 (:9000), con modo libre
PolyConecta.Presentation/    Prototipo Blazor Server, .NET 8, referencia de UX sin cambios (solo en pruebas de paridad, :9010)
PolyConecta.Contpaq/         Bridge hacia CONTPAQi (:9030): real en Windows x86 o simulado
docs/contratos/              Contrato bridge-v1, su OpenAPI y sus ejemplos
tests/                       Dominio, aplicación, integración, bridge y suite de contrato
tools/sdk-lab/               Laboratorio para probar el SDK contra un CONTPAQi real
docs/                        Diseño vigente, hechos del assessment y referencia de CONTPAQi
.specify/                    Constitución y specs de Spec Kit
.github/workflows/           CI: dotnet, contrato y web
scripts/                     Ejecutar, empaquetar, desplegar y logins de SQL Server
```

## Ejecutar

Requiere el SDK de .NET 10 (`global.json`), Docker (SQL Server 2022 local y pruebas) y Node 24.16 (aplicación Angular). El runtime de ASP.NET Core 8 solo hace falta para las pruebas de paridad contra el prototipo. Detalle en [05-arquitectura-tecnica.md §5](docs/diseno/05-arquitectura-tecnica.md).

```bash
./run.sh                 # compila, prueba y levanta PolyConecta.Web (:9000) + API (:9200)
./run.sh --with-bridge   # además levanta el bridge (:9030), simulado fuera de Windows
```

Las pruebas de la aplicación Angular (paridad con el prototipo, guiones, auditor y chatter) están en [PolyConecta.Web/README.md](PolyConecta.Web/README.md).
