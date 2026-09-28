# PolyConecta.Infrastructure

Persistencia y outbox. Depende solo de `PolyConecta.Domain`.

## Contenido actual

| Carpeta | Contenido |
| :--- | :--- |
| `Persistence/` | `PolyDbContext`, configuraciones de entidades y `IDataSeeder` |
| `Repositories/` | Implementaciones de los contratos del dominio |
| `Outbox/` | `OutboxPublisher` e `IOutboxProcessor`: encolan eventos para el bridge de CONTPAQi |
| `Common/` | `IUnitOfWork` |

El proveedor de Npgsql (PostgreSQL) está referenciado pero **no se usa**: la API registra `UseInMemoryDatabase`. Tampoco hay migraciones todavía.

## Destino

PostgreSQL con migraciones, outbox transaccional en la misma unidad de trabajo que el cambio de negocio y semillas de catálogos (almacenes, ubicaciones y tipos de operación de [03-almacenes-y-operaciones.md](../docs/diseno/03-almacenes-y-operaciones.md)). El motor de persistencia está pendiente de un ADR (T-09 en [preguntas abiertas](../docs/diseno/preguntas-abiertas.md)).
