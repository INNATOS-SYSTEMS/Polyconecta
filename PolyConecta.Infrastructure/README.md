# PolyConecta.Infrastructure

Persistencia y outbox. Depende solo de `PolyConecta.Domain`.

## Contenido actual

| Carpeta | Contenido |
| :--- | :--- |
| `Persistence/` | `PolyDbContext`, configuraciones de entidades y `IDataSeeder` |
| `Repositories/` | Implementaciones de los contratos del dominio |
| `Outbox/` | `OutboxPublisher` e `IOutboxProcessor`: encolan eventos para el bridge de CONTPAQi |
| `Common/` | `IUnitOfWork` |

El proveedor de Npgsql (PostgreSQL) está referenciado pero **no se usa**, y hay que cambiarlo por el de SQL Server. La API registra `UseInMemoryDatabase` y todavía no hay migraciones.

## Destino

**SQL Server** (D-49), en una base propia separada de las de CONTPAQi, con migraciones, outbox transaccional en la misma unidad de trabajo que el cambio de negocio y semillas de catálogos (almacenes, ubicaciones y tipos de operación de [03-almacenes-y-operaciones.md](../docs/diseno/03-almacenes-y-operaciones.md)). La semilla inicial también reserva todas las ubicaciones como almacenes en `admAlmacenes` (D-43, sujeto a T-10 y T-11 en [preguntas abiertas](../docs/diseno/preguntas-abiertas.md)).
