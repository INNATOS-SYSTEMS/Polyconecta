<!--
Sync Impact Report:
- Version change: 1.7.0 → 1.8.0
- Modified sections: Governance & Amendment Policy adds the branch rule: each spec is built on its own branch, nothing is committed directly to `main`, and only finished work is merged into `main`.
- Modified principles: None.
- Added principles: None.
- Removed sections: None.
- Follow-up TODOs: None. Source decision: docs/diseno/decisiones.md D-125 (2026-10-05), approved by the user.

Previous amendment (1.6.1 → 1.7.0, 2026-10-05):
- Version change: 1.6.1 → 1.7.0
- Modified sections: Governance & Amendment Policy adds the one-spec-per-phase rule: each phase of the work plan is a single spec with an exploration stage, and every change discovered while building it stays inside that spec instead of opening a new one.
- Modified principles: None.
- Added principles: None.
- Removed sections: None.
- Follow-up TODOs: None. Source decisions: docs/diseno/decisiones.md D-116 and D-119 (2026-10-05), approved by the user.

Previous amendment (1.6.0 → 1.6.1, 2026-09-30):
- Version change: 1.6.0 → 1.6.1
- Modified principles:
  - Principle II: the SDK library is `MGWServicios.dll` (CONTPAQi Comercial), not `SDK_CONTPAQ.dll`; the bridge is a dedicated x86 process that keeps one long-lived SDK session with programmatic sign-in, instead of a generic Windows "Worker" (the SDK does not run over SSH, and running it as a Windows service is still unverified).
- Added principles: None.
- Removed sections: None.
- Follow-up TODOs: pregunta H-03 (how the bridge runs unattended). Source decisions: docs/diseno/decisiones.md D-88, D-91 and D-105 (2026-09-30), from the SDK test matrix.

Previous amendment (1.5.0 → 1.6.0, 2026-09-28): Principle X, Free Documents (D-52 to D-57).
Previous amendment (1.4.0 → 1.5.0, 2026-09-28): Principles I, II, IV, V, VI and IX aligned with no handheld devices, `SC` plant code and Montemorelos deferred to Phase 2 (D-32 to D-51).
-->

# PolyConecta Project Constitution

## Core Principles

### I. System Architecture & ERP Repository of Record
PolyConecta is a specialized, custom Operational Routing & Inventory Engine. **CONTPAQi Comercial Premium v10+ remains the single master repository of record** for billing, accounting, financial reporting, and official stock balances. No third-party ERP framework (such as Odoo) will be deployed. PolyConecta operates as the intelligence layer for shop-floor routing, stock movements, and production tracking, continuously reflecting transactions into CONTPAQi. Although PolyConecta is a custom application and does not deploy Odoo as an underlying ERP, its User Experience (UI/UX), shop-floor interaction, and operational philosophy across MRP, Inventory Locations, Work Centers, and Quality MUST explicitly take **Odoo 19 Enterprise** as their primary design and interaction benchmark.

### II. Asynchronous ERP Synchronization & Outbox Resilience
Integration with CONTPAQi Premium MUST strictly follow an asynchronous Outbox Pattern with a dedicated single-threaded Integration Bridge (.NET x86 process) interacting via the CONTPAQi Comercial SDK (`MGWServicios.dll`). The bridge signs in programmatically, with no human interaction, and keeps one long-lived SDK session; how it runs unattended on Windows is defined in the technical design. Direct SQL `INSERT` or `UPDATE` queries to CONTPAQi database tables (`adm*`) are strictly prohibited to preserve database integrity and CFDI compliance. Plant operations and shop-floor capture MUST proceed asynchronously without waiting for synchronous ERP locks.

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

### X. Free Documents
Following Odoo's document philosophy, every operational document (sales order, manufacturing order, stock operation, quality control, incident) MUST be creatable on its own through a "New" action, without a source document. The origin (the document that generated it) is an optional reference, navigable through smart buttons, and never a precondition for existence. A document's rules MUST apply identically with or without origin: states and transitions, role permissions, the quality hard-stop, mass balance, CONTPAQi postings and the documents it derives. Any restriction on free creation MUST be stated explicitly per document type and justified by an invariant (for example, a free reception can only receive lots already in transit).

## Governance & Amendment Policy

- This Constitution governs all technical architecture, specification design (`.specify`), planning (`plan.md`), and task execution (`tasks.md`) in PolyConecta.
- **One spec per phase.** Each phase of the work plan is exactly one spec. What the spec defines when it is ratified is its **primary objective**; building it is also an **exploration stage**, and every change discovered along the way (new requirements, adjustments, corrections, decisions) MUST be recorded and executed inside that same spec, in its exploration log. A minor or light change MUST NOT create a new spec. A new spec is only justified by a new phase of the work plan. Changes that alter a validated decision or the bridge contract are also recorded in `docs/diseno/decisiones.md` when they are made (contract changes still require both leads); the rest are integrated into `docs/diseno/` when the spec closes.
- **One branch per spec; nothing goes directly to `main`.** Each spec is built on its own branch, named after its folder (`NNN-<fase>`), and both leads work on that branch. Any other change (documentation, fixes, amendments) also goes on its own branch. No commit is made directly on `main`: a branch is merged into `main` only when its work is finished and verified (for a spec, when it closes per CT-43; for any other change, when it is complete). After the merge, the branch is deleted.
- Amendments require formal approval from the Steering Committee.
- Semantic versioning applies (MAJOR for principle redefinition, MINOR for scope/governance updates, PATCH for wording fixes).

**Version**: 1.8.0 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-10-05
