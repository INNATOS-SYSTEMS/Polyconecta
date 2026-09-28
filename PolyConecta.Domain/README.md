# PolyConecta.Domain

Núcleo del sistema: entidades de negocio, value objects, servicios de dominio y contratos de repositorio. **No depende de ningún otro proyecto** ni de frameworks de persistencia, HTTP o UI.

## Contenido actual

| Carpeta | Contenido |
| :--- | :--- |
| `Entities/` | `Product`, `StockLot`, `ManufacturingOrder` (autorreferenciado con `ParentId`), `Bom`/`BomLine`, `StockLocation`, `PolyLocation`, `StockPicking`/`StockMove`, `QualityCheck`/`StockScrap`, `RawMaterialCatalog`/`SupplierProductMapping`, `MassBalanceAudit`, `LotGenealogy`, `OutboxMessage` |
| `Services/` | `MassBalanceService` (tolerancia por defecto: 2 %), interfaces de servicios de dominio |
| `Repositories/` | Contratos de repositorio (órdenes, materiales, proveedores, outbox) |
| `ValueObjects/` | `Folio` |
| `Events/` | `IDomainEventPublisher` |

## Destino

El modelo objetivo y la lista exacta de lo que falta están en [docs/diseno/04-modelo-de-dominio.md](../docs/diseno/04-modelo-de-dominio.md). En particular, `Bom`/`BomLine` con capas A/B/C, las banderas `SalesApproved`/`CreditApproved` en la orden y el value object `Folio` responden a decisiones ya sustituidas (D-07, D-15 y numeración centralizada).
