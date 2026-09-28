# Especificación Técnica y Funcional: Réplica de la Capa de Presentación en Angular SPA (`PolyConecta.Web.Angular`)

> **Estado**: Aprobado / Listo para Ejecución  
> **Fecha**: 28 de Septiembre de 2026  
> **Proyecto Origen**: `PolyConecta.Presentation` (Blazor Server .NET 8)  
> **Proyecto Destino**: `PolyConecta.Web.Angular` (Angular 18+ SPA Standalone)  
> **Ubicación de Referencia**: `/Users/emilio/Development/Sandbox/Polyconecta/docs/sdd/ESPECIFICACION_MIGRACION_ANGULAR_PRESENTATION.md`  

---

## 🎯 1. Objetivo y Alcance

El objetivo de esta especificación es definir los requisitos técnicos, la arquitectura objetivo, la estructura de componentes y el plan de implementación asistido por agentes de IA para **recrear fielmente (paridad 1:1 de funcionalidad y UI/UX)** la capa de presentación `PolyConecta.Presentation` utilizando **Angular (SPA)**.

### 1.1 Premisas Clave
1. **Diseño e Interfaz Intactos (Odoo Design System)**: Mantenimiento exacto del estilo visual profesional Odoo (pantalla de inicio con App Launcher, Smart Buttons con contadores, barra de estado de pipeline, tabla de captura de líneas, cajón de Chatter en tiempo real, modales de lotes y barra de búsqueda con filtros/agrupadores).
2. **Paridad Funcional Completa**: Migración sin pérdida de funciones de las 18 páginas operativas (Ventas, Fabricación Extrusión/Impresión/Bolseo, Logística de Traslados/Entregas/Recepciones/Recolecciones, Control de Calidad por Lote, Inventario Actual, Dashboard y Terminales de Planta Handheld).
3. **Cero Rompimiento Backend**: Mantener compatibilidad con los contratos REST expuestos por `PolyConecta.Api` (puerto `9020`).

---

## 🏛️ 2. Impacto en la Arquitectura de la Solución

El cambio de **Blazor Server** a **Angular SPA** transforma la arquitectura de despliegue y flujo de datos de la solución:

```
[ ANTES: Blazor Server Architecture ]
Browser Client <---(WebSocket / SignalR UI Diffs)---> PolyConecta.Presentation (Port 9000, .NET 8)
                                                             |
                                                     (REST API Clients)
                                                             v
                                                   PolyConecta.Api (Port 9020)

[ DESPUÉS: Angular SPA Architecture ]
Browser Client / Handheld Terminal (SPA Angular estática)
       │
       ├───(REST HTTP JSON / RxJS)─────────> PolyConecta.Api (Port 9020)
       │                                            │
       └───(SignalR WebSockets: @microsoft/signalr)─┴──> ChatterHub (Relocalizado en API 9020)
```

### 2.1 Cambios Arquitectónicos Estructurales

| Dimensión | Estado Actual (`Blazor Server`) | Estado Objetivo (`Angular SPA`) | Impacto / Solución |
| :--- | :--- | :--- | :--- |
| **Modelo de Renderizado** | Server-Side Prerendering con diffs de DOM por WebSocket. | Client-Side Rendering (CSR) puro con Angular Standalone Components. | Rendimiento inmediato en terminales de planta sin latencia por clic. |
| **Puerto y Hosting** | Servidor Web C# dedicado en el puerto `9000`. | Artefactos estáticos compilados (`dist/`). | Pueden servirse vía Nginx, CDN o carpeta `wwwroot` de `PolyConecta.Api`. |
| **SignalR Hub (`ChatterHub`)** | Reside en `PolyConecta.Presentation/Hubs/ChatterHub.cs`. | **Relocalizado a `PolyConecta.Api/Hubs/ChatterHub.cs`**. | Angular se conecta vía `@microsoft/signalr` al puerto 9020. |
| **Gestión de Estado** | Servicios Scoped C# (`OperationalFlowState`, `InventoryState`). | **Angular Signals & RxJS Services** (`OperationalFlowStore`, etc.). | El estado operativo reside en memoria cliente (navegador). |
| **Tipado y Modelos** | Clases C# de dominio y ViewModel compartidas en proyecto. | **Interfaces y Types TypeScript** creadas en `src/app/core/models/`. | Garantía de tipado estricto en cliente sin compilación C#. |

---

## 🧩 3. Matriz de Componentes del Design System "Estilo Odoo"

Se garantiza la recreación exacta de los 13 componentes de UI reutilizables:

```
src/app/shared/components/odoo/
├── odoo-topbar/             <-- Barra superior con marca PolyConecta y selector de compañía
├── main-layout/             <-- Layout contenedor principal con sidebar/chatter integrados
├── odoo-breadcrumb/         <-- Migas de pan dinámicas por módulo
├── odoo-search-panel/       <-- Filtros rápidos, búsqueda y agrupadores Odoo
├── odoo-view-switcher/      <-- Conmutador de vistas (Kanban, Lista, Formulario)
├── odoo-pager/              <-- Paginador de registros Odoo
├── odoo-smart-buttons/      <-- Caja de Smart Buttons con contadores numéricos e íconos
├── odoo-status-pipeline/    <-- Barra de estado del documento (Borrador -> Planeado -> En progreso -> Hecho)
├── odoo-line-capture/       <-- Captura de líneas interactivas (Componentes BOM, Subproductos, Líneas de Pedido)
├── odoo-chatter-drawer/     <-- Cajón lateral de Chatter con historial de mensajes y notas internas
├── lot-picker-modal/        <-- Selector modal de lotes con inventario disponible
├── lot-quantity-picker-modal/ <-- Modal para desglose y asignación rápida de cantidades por lote
└── poc-sales-order-form/    <-- Formulario especializado de prueba de concepto de pedido
```

---

## 📑 4. Matriz de Páginas y Vistas Operativas (Paridad 1:1)

| Módulo / Sección | Vista C# Original (Blazor) | Componente Angular Target | Descripción / Funcionalidad |
| :--- | :--- | :--- | :--- |
| **App Launcher** | `Dashboard.razor` | `DashboardComponent` | Hub de inicio con accesos directos a Ventas, Fabricación, Calidad, Logística y Planta. |
| **Ventas** | `PedidosList.razor`<br>`PedidoFormView.razor` | `PedidosListComponent`<br>`PedidoFormViewComponent` | Gestión de Pedidos de Venta, líneas de pedido, firmas Ventas/Crédito y sync CONTPAQi. |
| **Fabricación (MES)** | `FabricacionList.razor`<br>`FabricacionFormView.razor` | `FabricacionListComponent`<br>`FabricacionFormViewComponent` | Órdenes de Fabricación encadenadas (Extrusión, Impresión, Bolseo), componentes BOM, lotes y subproductos. |
| **Control de Calidad** | `CalidadList.razor`<br>`CalidadFormView.razor` | `CalidadListComponent`<br>`CalidadFormViewComponent` | Revisiones de calidad por lote de fabricación, estatus Aprobado/Rechazado y almacén de falla. |
| **Logística - Traslados** | `TrasladosList.razor`<br>`TrasladoFormView.razor` | `TrasladosListComponent`<br>`TrasladoFormViewComponent` | Movimientos interplanta de materia prima y producto terminado con lotes. |
| **Logística - Entregas** | `EntregasList.razor`<br>`EntregaFormView.razor` | `EntregasListComponent`<br>`EntregaFormViewComponent` | Despacho a clientes, vinculación de pedido y control de peso/báscula. |
| **Logística - Recepción/Recolección** | `RecepcionList.razor` / `RecepcionFormView.razor`<br>`RecoleccionesList.razor` / `RecoleccionFormView.razor` | `RecepcionComponents`<br>`RecoleccionComponents` | Recepciones de materia prima de proveedor y recolecciones logísticas. |
| **Planta / Handheld** | `IncidenciasPage.razor`<br>`CapturaMasivaPage.razor` | `IncidenciasComponent`<br>`CapturaMasivaComponent` | Terminales para operadores de planta: registro rápido de paros/fallas y pesaje masivo de rollos/lotes. |
| **Inventario** | `InventarioActualList.razor` | `InventarioActualListComponent` | Consulta de existencias por almacén, lote, clave y estatus de calidad. |

---

## 🛠️ 5. Estructura del Proyecto Angular Target

```
PolyConecta.Web.Angular/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── models/                <-- Interfaces TS (ManufacturingOrder, SalesOrderLine, ProductionLot, etc.)
│   │   │   ├── services/              <-- ApiClientService, SignalRChatterService
│   │   │   └── stores/                <-- OperationalFlowStore, InventoryStore, StockOperationStore (Signals)
│   │   ├── shared/
│   │   │   └── components/odoo/       <-- 13 Componentes Reutilizables Odoo UI System
│   │   ├── features/
│   │   │   ├── dashboard/
│   │   │   ├── ventas/
│   │   │   ├── fabricacion/
│   │   │   ├── calidad/
│   │   │   ├── logistica/
│   │   │   ├── inventario/
│   │   │   └── planta-handheld/
│   │   ├── app.routes.ts
│   │   └── app.config.ts
│   ├── assets/
│   ├── styles.scss                    <-- Bootstrap 5 + Estilos Odoo (Morado #714B67, cards generosas)
│   └── main.ts
├── angular.json
├── package.json
└── tsconfig.json
```

---

## 🤖 6. Plan de Ejecución Mediante Agentes de IA

Para ejecutar esta migración con la máxima eficiencia y sin margen de error, se asigna el trabajo a **5 Agentes de IA especializados**:

### 🤖 Agente 1: Infraestructura & Setup Backend/Frontend
* **Misión**: 
  1. Relocalizar `ChatterHub.cs` a `PolyConecta.Api` y habilitar CORS/SignalR en `PolyConecta.Api/Program.cs`.
  2. Inicializar el proyecto `PolyConecta.Web.Angular` con Angular Standalone Components, Bootstrap 5 y `@microsoft/signalr`.
  3. Crear los modelos/interfaces TypeScript equivalentes a los DTOs de C#.
* **Criterio de Aceptación**: `ng build` exitoso y conexión SignalR funcionando desde cliente Angular hacia `http://localhost:9020/hubs/chatter`.

### 🤖 Agente 2: Design System Odoo UI Components
* **Misión**: 
  1. Recrear los 13 componentes Odoo-like (`OdooTopbar`, `OdooSmartButtons`, `OdooStatusPipeline`, `OdooLineCapture`, `OdooChatterDrawer`, Modales de Lotes, etc.).
  2. Mantener la tipografía Inter, el color primario Odoo (`#714B67`) y las animaciones de los modales/drawer.
* **Criterio de Aceptación**: Todos los componentes renderizan aisladamente sin errores y respetan los inputs/outputs de datos.

### 🤖 Agente 3: Servicios de Estado & Capa REST
* **Misión**: 
  1. Migrar la lógica de `OperationalFlowState.cs`, `InventoryState.cs`, `StockOperationState.cs` y `UiViewState.cs` a servicios de Angular utilizando **Angular Signals**.
  2. Implementar los endpoints REST (`/api/v1/orders`, `/api/v1/raw-materials`, `/api/v1/rolls`, `/api/v1/locations`).
* **Criterio de Aceptación**: Cambio de estado reactivo comprobable al agregar componentes BOM, registrar rollos o actualizar estatus de órdenes de fabricación.

### 🤖 Agente 4: Módulos de Ventas, Fabricación y Calidad
* **Misión**: 
  1. Implementar `DashboardComponent`, `PedidosListComponent`, `PedidoFormViewComponent`.
  2. Implementar `FabricacionListComponent` y `FabricacionFormViewComponent` (incluyendo la lógica de OFs encadenadas: Extrusión -> Impresión -> Bolseo).
  3. Implementar `CalidadListComponent` y `CalidadFormViewComponent`.
* **Criterio de Aceptación**: Flujo de ventas a fabricación encadenada navegable y funcional 1:1 con la versión Blazor.

### 🤖 Agente 5: Módulos de Logística, Inventario y Planta Handheld + QA E2E
* **Misión**: 
  1. Implementar vistas de Traslados, Entregas, Recepciones y Recolecciones.
  2. Implementar vistas de Inventario y terminales de Planta (`IncidenciasComponent`, `CapturaMasivaComponent`).
  3. Ejecutar pruebas de integración visual y responsive para pantallas handheld de báscula.
* **Criterio de Aceptación**: Todas las 18 páginas operativas funcionando al 100% en Angular SPA.

---

## 🚀 7. Definición de Hecho (Definition of Done)

La migración se considerará completada cuando:
1. `PolyConecta.Web.Angular` ejecute de forma autónoma consumiendo `PolyConecta.Api` (puerto 9020).
2. Se mantenga paridad 1:1 estética (Odoo UX) y operativa con los ~4,900 LOC originales de `PolyConecta.Presentation`.
3. El cajón de Chatter en tiempo real funcione mediante SignalR relocalizado en la API.
4. No exista dependencia del servidor Web C# `PolyConecta.Presentation` (puerto 9000).
