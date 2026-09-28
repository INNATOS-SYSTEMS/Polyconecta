<!--
Sync Impact Report:
- Version change: 1.4.0 → 1.5.0
- Modified principles:
  - Principle I: Removed "Handheld Workflows" from the Odoo 19 benchmark scope.
  - Principle II: "handheld capturing" generalized to shop-floor capture.
  - Principle IV: Quality gate now applies to every inter-plant transfer and customer dispatch, not only PIM operation codes.
  - Principle V: Santa Cruz plant code is `SC` (was `STC`). Montemorelos routing and any intercompany document are deferred to Phase 2 (the Phase 1 quotation trigger is removed).
  - Principle VI: There are no handheld devices, scale terminals or scanners. All shop-floor capture is done by the Planner (and shift Supervisor for incidents) in the PolyConecta web application. Montemorelos moved to exclusions.
  - Principle IX: Removed barcode/handheld scanner interfaces from mandatory patterns.
- Removed sections: None.
- Follow-up TODOs: None. Source decisions: docs/diseno/decisiones.md D-32 to D-51 (2026-09-28).
-->

# PolyConecta Project Constitution

## Core Principles

### I. System Architecture & ERP Repository of Record
PolyConecta is a specialized, custom Operational Routing & Inventory Engine. **CONTPAQi Comercial Premium v10+ remains the single master repository of record** for billing, accounting, financial reporting, and official stock balances. No third-party ERP framework (such as Odoo) will be deployed. PolyConecta operates as the intelligence layer for shop-floor routing, stock movements, and production tracking, continuously reflecting transactions into CONTPAQi. Although PolyConecta is a custom application and does not deploy Odoo as an underlying ERP, its User Experience (UI/UX), shop-floor interaction, and operational philosophy across MRP, Inventory Locations, Work Centers, and Quality MUST explicitly take **Odoo 19 Enterprise** as their primary design and interaction benchmark.

### II. Asynchronous ERP Synchronization & Outbox Resilience
Integration with CONTPAQi Premium MUST strictly follow an asynchronous Outbox Pattern with a dedicated single-threaded Integration Bridge (.NET x86 Worker) interacting via `SDK_CONTPAQ.dll`. Direct SQL `INSERT` or `UPDATE` queries to CONTPAQi database tables (`adm*`) are strictly prohibited to preserve database integrity and CFDI compliance. Plant operations and shop-floor capture MUST proceed asynchronously without waiting for synchronous ERP locks.

### III. SKU Catalog Boundaries & Inventory Mapping
- **Materia Prima (MP):** Raw materials (resins, additives, pigments) MUST be strictly standardized under a unified master SKU catalog across all plants, resolving supplier code discrepancies.
- **Producto Terminado (PT):** Finished product codes in CONTPAQi MAY retain customer-specific / specification-group SKUs as required by commercial and invoicing operations. PolyConecta MUST support customer PT SKUs while tracking internal master physical specifications.

### IV. Mass-Balance, Mandatory Production Audits & Quality Hard-Stop
- Raw material consumption MUST be tracked via mass-balance ($\text{Consumo MP} = \sum \text{Rollos} + \text{Scrap}$). A configurable tolerance parameter MUST be provided in system settings.
- **Mandatory Production Audits:** Quality and production audits for manufactured output are strictly mandatory for every production run, regardless of total volume produced.
- **Hard-Stop Quality Gate:** No inter-plant transfer or customer dispatch of a lot may be completed without an explicit digital Quality Release.

### V. Governance, Digital Approvals & Multi-Plant Routing
- Requisition workflows MUST enforce digital sign-offs by all designated mandatory roles (Solicitante, Autorizador, Elaborador). Threshold-based auto-approvals without explicit role authorization are forbidden.
- **Multi-Plant Routing:** Operations across plants (Apodaca `PIM`, Santa Cruz `SC`, Montemorelos `MTM`) MUST follow defined multi-step routes. Montemorelos (Razón Social 2) is modeled as a plant and legal entity, but its routes and intercompany documents are deferred to Phase 2.

### VI. Phase 1 Scope Boundaries
- **Shop Floor Interface:** There are no handheld devices, scale terminals or scanners. Machine operators record production on paper logs at the machine; the Planner transcribes them into the PolyConecta web application, and the shift Supervisor records incidents. Operators do not use the system.
- **Scope Inclusions:** Order-to-Cash integration, roll weighing capture by the Planner, Mass Balance consumption, Hard-Stop Quality Gate, CONTPAQi SDK sync.
- **Scope Exclusions (Deferred):** Peletizado / Re-granulación Order tracking (Phase 2), Montemorelos routing and all intercompany transactions (Phase 2), direct IoT scale integration (Phase 2).

### VII. Mandatory Technical Backing & Reference Verification
All technical architecture, database schema, SDK invocation, and ERP integration decisions MUST be explicitly grounded in and backed by the project's authoritative reference manuals:
- **CONTPAQi Database Reference Manual:** [Referencia_BD_CONTPAQi.md](file:///Users/emilio/Development/Sandbox/Polyconecta/docs/contpaq/Referencia_BD_CONTPAQi.md)
- **CONTPAQi SDK Reference Manual:** [Referencia_SDK_CONTPAQi.md](file:///Users/emilio/Development/Sandbox/Polyconecta/docs/contpaq/Referencia_SDK_CONTPAQi.md)

If any technical specification, API function signature, error code, or database table schema is missing, incomplete, or ambiguous within these reference manuals, web search and official documentation lookup MUST be performed to verify and confirm technical accuracy before ratifying any feature spec, design artifact, or code implementation.

### VIII. Alignment with Existing CONTPAQi Operational Database Reality
All data architecture decisions, field structures, entity definitions, and operational relationships MUST be strictly grounded in the actual operational reality of Polyempaques' existing CONTPAQi database (`adm*` tables).

Whenever any ambiguity arises regarding:
- Product catalogs (`admProductos`)
- Customer structures and client relationships (`admClientes`)
- Document concept definitions (`admConceptos`)
- Document types (Invoices, Sales Orders, Purchase Orders, Delivery Notes / Remisiones, Stock Transfers)
- Warehouse definitions, locations, and inventory layers (`admAlmacenes`, `admCapasProducto`)

The actual CONTPAQi database structure, existing operational records, and reference documentation MUST be consulted and verified before ratifying any feature specification (`spec.md`), architectural design artifact (`.md`), or database schema definition.

### IX. UI/UX & MRP Operational Benchmark: Odoo 19 Enterprise
All user interfaces, interaction models, visual layouts, and operational workflows across PolyConecta MUST emulate the clean, frictionless User Experience (UI/UX) and operational philosophy of **Odoo 19 Enterprise**. Key design and operational patterns MUST include:
- **Navigation & View Patterns:** Dynamic Kanban, List, and Form views with status pipeline headers and smart action buttons (*Smart Buttons* for direct traceability to related records).
- **MRP & Shop Floor Philosophy:** Work Center scheduling, 2-step location-based inventory movements, multi-level BOMs, and capture screens designed for zero latency and minimal cognitive overhead in plant operations.

## Governance & Amendment Policy

- This Constitution governs all technical architecture, specification design (`.specify`), planning (`plan.md`), and task execution (`tasks.md`) in PolyConecta.
- Amendments require formal approval from the Steering Committee.
- Semantic versioning applies (MAJOR for principle redefinition, MINOR for scope/governance updates, PATCH for wording fixes).

**Version**: 1.5.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-28
