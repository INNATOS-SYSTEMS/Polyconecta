# Implementation Plan: Captura de producción (F4)

> **Secciones por líder (CT-34, D-120).** Lo común va primero y lo acuerdan los dos líderes. Después, la sección **L1** la edita solo Alejandro Ponce y la **L2** solo Luis Alvarado Martinez.

**Branch**: `006-captura-produccion` | **Date**: _por definir_ | **Spec**: [spec.md](spec.md)

**Status**: Esqueleto. Se llena con `/speckit-plan` después de ratificar la spec.

---

## Común

### Summary

_Por redactar: objetivo de la fase y enfoque técnico._

### Technical Context

**Language/Version**: .NET 10, C#; Angular 22 con TypeScript 6.0 (CT-04, D-67, D-68)

**Primary Dependencies**: ASP.NET Core 10, EF Core 10, SignalR; bridge `win-x86` con `MGWServicios.dll` (CT-36)

**Storage**: SQL Server 2022 (PolyConecta, CT-05); SQLite (outbox del bridge)

**Testing**: xUnit v3 + AwesomeAssertions, Playwright, suite de contrato (CT-23, CT-27)

**Target Platform**: API y web en el hosting por definir (H-01); bridge en el VPS de CONTPAQi (D-115)

**Constraints**: _Por redactar._

### Constitution Check

*Se revisa antes de investigar y otra vez después del diseño.*

| Principio o regla | Cómo se cumple |
| :--- | :--- |
| II · Toda escritura a CONTPAQi va por outbox y bridge | _Por redactar_ |
| X · Documento libre ("Nuevo") | _Por redactar_ |
| CT-43 · Exploración dentro de esta spec | Los cambios se registran en la spec |

### Contrato

_Comandos y lecturas de `bridge-v1` que usa la fase, y cambios propuestos (CT-22)._

### Project Structure

```text
.specify/features/006-captura-produccion/
├── spec.md      # común
├── plan.md      # este archivo: Común, L1, L2
├── tasks.md     # Común, L1, L2
└── research.md, data-model.md, quickstart.md   # si hacen falta
```

---

## L1 · Camino 1 · Alejandro Ponce

_Por redactar: decisiones de diseño del bridge y, cuando aplique, de lo que construye en PolyConecta (D-117)._

---

## L2 · Camino 2 · Luis Alvarado Martinez

_Por redactar: decisiones de diseño de dominio, casos de uso, persistencia, API y pantallas._

---

## Complexity Tracking

_Solo si algo viola la constitución y hay que justificarlo._
