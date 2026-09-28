# INFORME OPERATIVO TO-BE: SISTEMA POLYCONECTA
**Integración Operativa entre Planta y CONTPAQi ERP**  
**Fecha:** Septiembre 2026  
**Versión:** 1.0 (Definición de Arquitectura Operativa)

> **Nota (28-sep-2026):** propuesta preliminar, se conserva como registro. La refinó el [informe de validación](INFORME_VALIDACION_DIAGRAMA_OPERATIVO.md) y después las [decisiones de diseño](../diseno/decisiones.md). El diseño vigente está en [`docs/diseno/`](../diseno/).

---

## 1. Resumen Ejecutivo y Diagnóstico (As-Is vs. To-Be)

### Gaps Operativos Identificados
* **Desconexión ERP - Planta:** CONTPAQi no refleja el consumo real de materia prima ni la existencia de producto terminado en tiempo real; los movimientos se registran de forma diferida o manual.
* **Proliferación Innecesaria de Códigos:** Creación desmedida de códigos de producto terminado para variaciones menores de medidas y calibres, saturando la base de datos comercial.
* **Aislamiento en Formulación y Calidad:** La planeación de mezclas (BoM), la asignación de máquinas y los certificados de calidad operan en hojas de cálculo externas desconectadas del flujo administrativo.
* **Fricción en Traspasos e Intercompany:** Ausencia de trazabilidad en traslados entre plantas y duplicidad operativa al generar transacciones espejo entre empresas filiales.

### Visión de la Solución (To-Be)
**PolyConecta** se establece como una capa de ejecución y control intermedio (Manufacturing Execution & Routing Engine) que sincroniza bidireccionalmente con CONTPAQi:
* Centraliza las Órdenes de Manufactura Maestras vinculadas a pedidos de venta.
* Estandariza la carga de formulaciones vía hojas de cálculo dinámicas.
* Permite el pesaje y loteo iterativo de rollos sin cerrar prematuramente las órdenes.
* Cuadra inventarios mediante balances de masa y registro explícito de scrap/merma.
* Automatiza la logística interna mediante un motor de almacenes, ubicaciones virtuales, tipos de operación y rutas predefinidas.

---

## 2. Matriz de Actores y Roles

| Actor / Rol | Entorno Principal | Responsabilidades Clave |
| :--- | :--- | :--- |
| **Atención a Clientes** | CONTPAQi / PolyConecta | Captura de pedido de venta en ERP; carga de especificaciones técnicas (Excel) y asignación de ruta logística inicial en PolyConecta. |
| **Ventas / Cobranza** | PolyConecta | Liberación del flujo de aprobación secuencial (validación comercial y liberación crediticia) previo al pase a planta. |
| **Planner de Extrusión** | PolyConecta | Importación de BoM formulada; programación de fechas y centros de trabajo; captura iterativa de rollos y pesajes; declaración de scrap y cierre técnico de OF. |
| **Control de Calidad** | PolyConecta | Inspección y liberación/bloqueo de órdenes de calidad generadas por cada rollo proyectado. |
| **Planner / Almacén Santa Cruz** | PolyConecta | Recepción física y digital de rollos en tránsito (Paso 2 de traspaso); habilitación de insumos para bolseo/impresión. |
| **Tráfico / Embarques** | CONTPAQi / PolyConecta | Despacho de rollos a cliente directo o confirmación de salida a flete interno. |
| **Operador de Máquina** | Planta Física | Ejecución manual de extrusión, embobinado y pesaje en báscula física (*sin interacción directa con el software*). |

---

## 3. Arquitectura del Motor Logístico (Routing Engine)

### 3.1. Almacenes Físicos y Entidades
* **`WH-PIM` (Planta Parque Industrial Monterrey):** Razón Social 1. Centro de extrusión y abasto de rollos maestros.
* **`WH-STC` (Planta Santa Cruz):** Razón Social 1. Procesos secundarios de bolseo, corte, impresión y empaque final.
* **`WH-MTM` (Planta Montemorelos):** Razón Social 2 (Empresa Hermana). Fabricación externa bajo esquema intercompany.
* **`WH-LOG` (Almacén Virtual de Tránsito):** Entidad lógica para custodia de material en tránsito interplanta.

### 3.2. Ubicaciones Clave
* `PIM/Stock/MP`: Materia prima disponible (resinas, pigmentos, aditivos).
* `PIM/Produccion`: Ubicación virtual de consumo.
* `PIM/Stock/PT`: Piso de producto terminado pesado y validado (rollos maestro).
* `PIM/Stock/Scrap`: Ubicacion virtual donde almacenar el SCRAP generado durando produccion.
* `PIM/Stock/Cuarentena`: Rollos fuera de tolerancia de peso o especificación.
* `TRANS/PIM-SC`: Ubicación virtual de dos pasos para traslados en curso.
* `SC/Stock/MP`: Insumos recibidos listos para bolseo/impresión.
* `SC/Produccion`: Ubicación virtual de consumo
* `SC/Stock/PT`: Piso de producto terminado pesado y validado (bolseo).
* `SC/Stock/Scrap`: Ubicacion virtual donde almacenar el SCRAP generado durando produccion.
* `SC/Stock/Cuarentena`: Producto terminado fuera de tolerancia de peso o especificación.

### 3.3. Tipos de Operación
* **`PIM-MO` (Fabricación Extrusión):** `PIM/Stock/MP` $\rightarrow$ `PIM/Stock/Rollos`.
* **`PIM-OUT-DIR` (Embarque Directo):** `PIM/Stock/Rollos` $\rightarrow$ `Clientes`.
* **`PIM-TR-OUT` (Salida a Tránsito):** `PIM/Stock/Rollos` $\rightarrow$ `TRANSIT/PIM-STC`.
* **`STC-TR-IN` (Recepción Traspaso):** `TRANSIT/PIM-STC` $\rightarrow$ `STC/Stock/Rollos`.
* **`ICO-DOC-TRIGGER` (Cruce Intercompany):** Generación simultánea de Pedido de Venta en MTM y Orden de Compra en PIM.

---

## 4. Descripción Detallada del Flujo Operativo To-Be

### Fase 1: Pedido, Especificación y Aprobaciones
1. **Captura en ERP:** Atención a Clientes captura el Pedido de Venta en CONTPAQi tras acordar especificaciones y volúmenes.
2. **Creación de OM Maestra:** PolyConecta sincroniza el pedido y genera la *Orden de Manufactura Maestra*.
3. **Adjunto Técnico:** Atención a Clientes anexa el archivo de especificaciones técnicas (Excel) y selecciona la ruta logística propuesta.
4. **Flujo de Aprobación por Áreas:**
   * **Firma 1 (Ventas):** Valida congruencia técnica, tiempos de entrega y especificación.
   * **Firma 2 (Crédito y Cobranza):** Valida estatus crediticio y cartera del cliente.
   * Al completarse ambas firmas, la orden pasa al estatus `Aprobada para Producción`.

### Fase 2: Planeación y Desglose Operativo
1. **Disparo de OF Hijas:** PolyConecta genera las Órdenes de Fabricación necesarias según los procesos requeridos.
2. **Carga de BoM Dinámica:** El Planner de Extrusión accede a la OF en PolyConecta y carga el archivo Excel con los porcentajes de formulación y materias primas.
3. **Mapeo de Consumos:** PolyConecta reserva los insumos correspondientes en base a la formulación cargada.
4. **Programación:** El sistema despliega las operaciones pendientes; el Planner asigna fecha programada y Centro de Trabajo (Extrusora).
5. **Generación Anticipada de Calidad:** Con base en el número proyectado de rollos a fabricar, PolyConecta precarga automáticamente las fichas individuales de inspección de calidad correspondientes.

### Fase 3: Ejecución, Pesaje Iterativo y Balance de Masa
1. **Extrusión Física:** La planta PIM procesa la resina según la programación física establecida.
2. **Pesaje Iterativo:** A medida que cada rollo maestro sale de línea y se pesa en báscula física, el Planner ingresa a PolyConecta:
   * Identificador secuencial / Lote individual.
   * Peso neto real (kg).
   * *La orden permanece abierta durante todo el turno o corrida productiva.*
3. **Liberación de Calidad:** Calidad valida tolerancias y propiedades sobre la orden precargada de cada rollo.
4. **Cierre de Orden y Registro de Scrap:**
   * El Planner da por finalizada la extrusión de la orden.
   * Para cuadrar el balance de masa, el Planner ingresa los **kg totales de merma/scrap** generados en el proceso.
   * **Regla de Consumo ERP:** PolyConecta calcula el insumo total ($\sum \text{Rollos} + \text{Scrap}$) y descuenta proporcionalmente las materias primas en CONTPAQi afectando las cuentas de inventario correspondientes.

### Fase 4: Bifurcación y Enrutamiento Logístico