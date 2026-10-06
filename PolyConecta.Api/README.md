# PolyConecta.Api

API REST en ASP.NET Core. Depende de `PolyConecta.Domain` y `PolyConecta.Infrastructure`.

- Base: `http://localhost:9020/api/v1`
- Swagger: `http://localhost:9020/swagger` (el contrato se genera en vivo; no hay un OpenAPI versionado)

## Contenido actual

| Controlador | Endpoints |
| :--- | :--- |
| `OrdersController` | `POST /orders/master` (orden raíz y sus hijas), `GET /orders/master/{id}`, `PATCH /orders/{id}/status` |
| `RollsController` | `POST /rolls/capture` (pesaje como `StockLot`), `GET /rolls/{folio}` |
| `LocationsController` | `GET /locations`, `POST /transfers/move` (con hard-stop de calidad) |
| `RawMaterialsController` | `GET` y `POST /raw-materials`, `POST /raw-materials/{id}/mappings` (códigos de proveedor) |

Además: `ProblemDetailsMiddleware`, respuestas envueltas en `ApiResponse` y `PagedResult`, y contratos CQRS en `Application/`. Corre sobre **EF InMemory**. Hoy la presentación no consume esta API.

## Destino

Un contrato por caso de uso (confirmar, autorizar, validar, cerrar…), autenticación con permisos por rol, paginación y filtros declarativos. Los casos de uso pasan a `PolyConecta.Application` (CT-08) y se construyen por módulo según el [roadmap](../docs/ROADMAP.md).
