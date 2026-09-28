# Módulos y roles

Qué módulos tiene PolyConecta, cómo se conectan, en qué momento tocan CONTPAQi y qué puede hacer cada rol en cada uno. Las reglas de cada módulo están en [02-flujo-y-reglas.md](02-flujo-y-reglas.md).

## 1. Mapa de módulos

Las columnas siguen el avance del pedido. Cada flecha dice qué pasa de un módulo al siguiente; las punteadas están pendientes.

```mermaid
flowchart LR
    ERP[("CONTPAQi Premium")]

    subgraph COM["Comercial"]
        PED["Pedido de venta"]
    end
    subgraph PLA["Planeación y abasto"]
        OF["Orden de fabricación"]
        REC["Recolección<br/>MP → WIP"]
    end
    subgraph PISO["Piso y calidad · PIM"]
        PES["Captura de pesaje"]
        CAL["Control de calidad"]
        INC["Incidencias"]
    end
    subgraph LOG["Logística"]
        TRA["Traslado<br/>PIM-TR-OUT"]
        RCP["Recepción<br/>STC-TR-IN"]
        MTM["Intercompany MTM"]
    end
    subgraph SC["Santa Cruz y salida"]
        CON["Conversión SC<br/>bolseo · impresión"]
        ENT["Entrega a cliente"]
    end

    ERP -- "① pedido sincronizado" --> PED
    PED -- "autoriza: genera OF por el faltante" --> OF
    PED -- "hay existencia: entrega directa" --> ENT
    OF -- "solicita MP" --> REC
    REC -- "MP reservada en WIP" --> OF
    PES -- "rollos pesados" --> OF
    INC -- "paros · merma" --> OF
    OF -- "inspección por lote" --> CAL
    CAL -- "libera · .S a cuarentena" --> OF
    CAL -- "lote aprobado" --> TRA
    CAL -- "venta de rollo" --> ENT
    TRA -- "TRANSIT/PIM-STC" --> RCP
    TRA -. "TRANSIT/PIM-MTM" .-> MTM
    RCP -- "SC/Stock/MP" --> CON
    CON -- "bolsa PT" --> ENT

    REC -. "② traspaso MP→WIP" .-> ERP
    OF -. "③ consumo + entrada PT" .-> ERP
    RCP -. "④ traspaso de almacén" .-> ERP
    CON -. "⑤ consumo + entrada PT" .-> ERP
    ENT -. "⑥ remisión de venta" .-> ERP
```

### Estado de cada módulo

"En prototipo" significa que la pantalla y el flujo existen en `PolyConecta.Presentation` sobre estado en memoria; ninguno está construido todavía sobre la API ni sobre persistencia real.

| Módulo | Estado | Lo que falta |
| :--- | :--- | :--- |
| Pedido de venta | En prototipo | Motor de rutas MTSO/MTO y segregación de firmas por persona |
| Orden de fabricación | En prototipo | Planeación por centro, subproductos, cierre con balance de masa y numeración centralizada |
| Recolección | En prototipo | Restringir la validación al rol de Almacén |
| Captura de pesaje | En prototipo | Slots precargados y renombrado `.S` |
| Control de calidad | En prototipo | `QualityControl` como documento propio |
| Incidencias | En prototipo | Catálogo de tipos y roles definidos |
| Traslado / Recepción / Entrega | En prototipo | Spec formal de logística y enlace Remisión ↔ Pedido |
| Conversión Santa Cruz | Parcial | Registro dual millares/kg y factor real (la OF-BOL ya existe) |
| Intercompany MTM | Pendiente | Todo; es Fase 2 según la constitución |
| Inventario actual | En prototipo | Proyección de las cinco cifras (físico, reservado, WIP, disponible, entrante) |
| Usuarios, roles y permisos | Pendiente | Todo (sección 3) |
| Búsqueda y filtros | Parcial | Solo existe la faceta de contexto; falta la vista declarativa en todos los modelos |
| Chatter | En prototipo | Persistencia y atribución por usuario |

## 2. Puntos de contacto con CONTPAQi

PolyConecta solo escribe en CONTPAQi cuando un documento **se valida o se cierra**. Mientras el material viaja o una orden sigue abierta, el ERP no cambia. Toda escritura pasa por el outbox y el bridge (Principio II).

| # | Documento en CONTPAQi | Módulo | Cuándo | Lo dispara |
| :---: | :--- | :--- | :--- | :--- |
| ① | Entra el pedido (lectura) | Pedido de venta | Sincronización tras la captura en CONTPAQi | Sistema |
| ② | Traspaso MP → WIP (y su inverso) | Recolección | Al validar la salida o la devolución | Almacenista |
| ③ | Consumo de MP + entrada de PT | Orden de fabricación | Al cierre técnico, descontando desde WIP | Planner |
| ④ | Traspaso de almacén | Recepción | Al validar la llegada a Santa Cruz | Almacenista SC |
| ⑤ | Consumo de rollo + entrada de bolsa PT | Conversión SC | Al cierre de la OF de bolseo o impresión | Planner SC |
| ⑥ | Remisión de venta | Entrega a cliente | Al validar el despacho | Logística / Tráfico |
| — | Factura intercompany y pedido espejo | Intercompany MTM | Fase 2 | Por definir |

La salida a tránsito (`PIM-TR-OUT`) **no** escribe en CONTPAQi.

## 3. Roles

### Modelo

| Pieza | Qué es |
| :--- | :--- |
| **Usuario** | Una persona con credenciales |
| **Rol** | Un conjunto de permisos, no una persona |
| **Asignación** | Usuario × Rol × **Planta**: un usuario puede tener varios roles y el alcance es por planta |

La seguridad tiene dos capas que no se mezclan:

- **Capa 1, permisos por acción**: qué puede *hacer* un rol sobre un tipo de documento. Es una matriz declarativa, separada del código de negocio.
- **Capa 2, reglas de fila**: sobre *qué registros* puede hacerlo. Son filtros reutilizables por rol, nunca condicionales dispersos.

### Catálogo de roles

| Rol | Responsabilidad | Titulares conocidos |
| :--- | :--- | :--- |
| Atención a Clientes | Captura y confirma el pedido; elige la ruta por línea; decide traspasos | Celia Villarreal |
| Comercial | Firma la autorización comercial | por confirmar |
| Crédito y Cobranza | Firma la autorización de crédito | por confirmar |
| Planner | Configura componentes, confirma y planea la OF, vacía los diarios y hace el cierre técnico | Roosvelt (PIM), Diana (SC) |
| Almacenista | Declara y valida lo que sale o entra del almacén | por confirmar |
| Calidad | Aprueba o rechaza lotes; es el único que levanta el hard-stop | por confirmar |
| Logística / Tráfico | Valida entregas a cliente y salidas a flete | por confirmar |
| Operador de piso | Pesa y reporta paros; no opera el sistema directamente | Alejandro Varela, Alfonso Ortega |
| Administrador | Configura catálogos, ubicaciones, tipos de operación y asignaciones | por confirmar |

### Matriz rol × módulo (capa 1)

**Negrita** = acción que mueve el documento. `Lee` = solo lectura. `—` = sin acceso. ◐ = pendiente de decidir.

| Módulo | AC | Comercial | Cobranza | Planner | Almacenista | Calidad | Tráfico | Operador | Admin |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| Pedido de venta | **Confirma, cancela** | **Firma 1/2**, revoca ◐ | **Firma 2/2**, revoca ◐ | Lee | — | — | Lee | — | **Confirma, cancela, revoca** |
| Orden de fabricación | Lee | — | — | **Edita, confirma, planea, cierra** | Lee | Lee | — | Lee | **Todo** |
| Recolección | — | — | — | **Solicita, devuelve**; no valida | **Declara lotes, valida** | Lee | **Valida** | — | **Valida** |
| Captura de pesaje | — | — | — | **Captura** | — | Lee | — | **Pesa** (terminal) | **Captura** |
| Control de calidad | — | — | — | Lee | — | **Aprueba, rechaza** | — | Registra | Registra; no levanta rechazos |
| Incidencias ◐ | — | — | — | Lee | — | — | — | **Captura** | Lee |
| Traslado | **Decide** | — | — | Lee | **Valida** | — | **Valida** | — | **Valida** |
| Recepción | — | — | — | **Recibe** (SC) | **Valida** | — | **Valida** | — | **Valida** |
| Conversión SC | — | — | — | **Planea, cierra** | — | **Libera** | — | Captura | **Edita** |
| Entrega a cliente | Lee | — | — | — | Lee | — | **Valida** | — | **Valida** |
| Inventario | Consulta; sustituye ◐ | — | — | **Re-lotifica** | **Re-lotifica** | Lee | — | — | **Todo** |
| Configuración | Lee | Lee | Lee | Lee | Lee | Lee | Lee | Lee | **Configura** |

Los roles de Incidencias los propuso el equipo de diseño; ninguna spec los había definido.

### Reglas de fila y segregación (capa 2)

| # | Regla |
| :--- | :--- |
| RF-1 | Un Planner solo ve y edita órdenes de **su planta**. |
| RF-2 | Un Almacenista solo valida operaciones cuyo **almacén origen** pertenece a su planta. |
| RF-3 | Comercial solo escribe su propia firma; Cobranza, solo la suya. |
| RF-4 | **Ningún usuario aporta las dos firmas del mismo pedido**, aunque tenga ambos roles. |
| RF-5 | El Operador de piso no tiene acceso directo: captura por terminal mediada. |
| RF-6 | Los documentos en estado `Hecho` quedan cerrados para todos los roles; se corrigen con un documento inverso. |

Además:

- **Producción pide, Almacén entrega**: el Planner no valida su propia recolección.
- **Hard-stop de Calidad**: ningún rol distinto de Calidad aprueba un lote, y nadie levanta un rechazo vigente sin una nueva liberación.
- Toda transición registra usuario, rol ejercido, fecha y documento, de forma consultable.
- Las acciones no permitidas se muestran deshabilitadas con su razón, no ocultas.
- Cambiar los permisos de un rol no requiere tocar código de negocio.

Fuera de alcance: la requisición de compras de tres firmas (Solicitante / Autorizador / Elaborador, Fase 2), la federación de identidad con CONTPAQi y los permisos por campo.

Las preguntas sin resolver (autenticación, titulares, revocación, suplencias…) están en [preguntas-abiertas.md](preguntas-abiertas.md).
