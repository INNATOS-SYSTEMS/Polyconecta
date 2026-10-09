# Quickstart: verificar F1

Cómo comprobar que F1 cumple su spec. Cada sección dice qué correr y qué debe pasar. Se recorre completo antes del cierre (C-T del cierre) y el resultado de cada sección va a "Exploración y cambios" de [spec.md](spec.md).

## Requisitos

- Lo de F0 ([05 §6](../../../docs/diseno/05-arquitectura-tecnica.md#6-cómo-correrlo)): SDK de .NET 10, Docker y Node 24.16.
- Variable `Seguridad__AdministradorInicial__Contrasena` en `.env.local` (la crea `run.sh` si falta).
- Para la sección 6, acceso al VPS por Escritorio remoto (D-130).

## 1. Todo en verde

```bash
./run.sh --with-bridge
```

Debe compilar, aplicar las migraciones `F1_*`, pasar las pruebas de .NET y web, y levantar la web (`:9000`), la API (`:9020`) y el bridge simulado (`:9030`). Luego:

```bash
BRIDGE_URL=http://localhost:9030 BRIDGE_CALLBACK_SECRET=<secreto> dotnet test --project tests/PolyConecta.Contract.Tests
cd PolyConecta.Web && npm run e2e -- e2e/f1
```

Esperado: la suite de contrato al 100 %, incluidas las lecturas de F1 (FR-002), y las pruebas extremo a extremo de F1 en verde.

## 2. Usuarios y grupos (US2)

1. Entrar a `http://localhost:9000` como `admin`. La barra superior muestra el nombre y la inicial.
2. En Configuración › Grupos, abrir **Comercial**: los permisos aparecen en dos paneles, en árbol Módulo › Documento o funcionalidad › Acción. Pasar a la izquierda `Ventas › Pedido › Firmar como Comercial`, guardar, y volver a ponerlo.
3. Crear el grupo **Producción · Supervisor** copiando **Supervisor de turno**.
4. Crear los usuarios de la [tabla de R1](#tabla-de-usuarios-de-r1) (o cargar `scripts/sql/f1-datos-r1.sql`).
5. Salir y entrar como `cobranza1` con una contraseña equivocada 5 veces: queda bloqueado 15 minutos.

## 3. Sincronización (US3)

1. Como `sistemas`, en Configuración › Sincronización, pulsar **Sincronizar todo**. Esperado: productos, clientes y almacenes con resultado `Éxito` y el conteo del catálogo semilla (SC-004).
2. Pulsar **Sincronizar ahora** solo en Productos: `0` cambiados y `0` archivados.
3. En el bridge simulado, marcar inactivo un producto (`PUT /admin/simulated/catalog/products/{codigo}`, que agrega L1 en 1.4, R-05) y sincronizar Productos: aparece archivado y no se ofrece en una línea nueva.
4. Detener el bridge y sincronizar: resultado `Error` con su motivo y nada archivado. Levantarlo y sincronizar: `Éxito`.
5. Abrir un producto PT, clasificarlo y capturar su ficha técnica (bloques Rollo y PT). Sincronizar otra vez: la clasificación no cambia.

## 4. Pedido con dos firmas (US1)

1. Como `ac1`: Pedidos › Nuevo. Elegir el cliente `EMM-001`: la moneda se propone con la del cliente y el domicilio con el de envío. Agregar dos líneas: la unidad es la base del producto y no se edita; capturar el precio unitario. Guardar: queda en Borrador con folio `PV-2026-0001`.
2. Confirmar sin precio en una línea: no confirma y dice qué falta. Corregir y confirmar.
3. Como `comercial1`: Autorizar. El formulario dice "1 de 2 firmas" y que falta Cobranza.
4. Como `ac1`: cambiar una cantidad y guardar. Aparece el aviso de D-147. Confirmar: la firma se borra y el chatter muestra el cambio y "Revocada por edición".
5. Firmar otra vez como `comercial1`. Como `doble` (Comercial y Cobranza) intentar firmar: **Autorizar** deshabilitado con su razón si ya firmó, y la API responde `409 TRANSICION_INVALIDA` si se pide directo, porque es una regla del dominio (RF-4) y no un permiso (SC-002). Un usuario sin el permiso de firmar recibe `403` (SC-003).
6. Como `cobranza-suplente`: Autorizar. El pedido queda Autorizado y la firma dice "suplente".
7. Como `comercial1`: Revocar con motivo. Regresa a Confirmado.
8. En el kanban, como `ac1`, arrastrar un pedido de Borrador a Confirmado; como `comercial1`, de Confirmado a Autorizado (diálogo de firma).
9. Recargar todo: nada se pierde, y la bitácora (`plt.state_transition_log`) tiene cada paso con usuario y grupo (SC-007).

## 5. Listas, favoritos y chatter (US4)

En desarrollo solo existe el pedido representativo IV310-26; la medición con volumen (SC-005) la cubre `ConsultaListaTests`, que crea sus propios pedidos.

1. Lista de Pedidos: filtrar Confirmado y Borrador (O) y un cliente (Y). Agrupar por cliente y luego por estado. Cada cambio responde en menos de 1 s (SC-005). La prueba `ConsultaListaTests` cuenta las consultas.
2. Guardar el favorito "Por autorizar" por omisión. Entrar como otro usuario: no lo ve. Volver a entrar como el primero en otro navegador: se aplica solo.
3. Abrir un pedido en dos navegadores con usuarios distintos. Escribir un mensaje en uno: aparece en el otro en vivo y sigue al recargar.

## 6. Bridge real en el VPS (US5, L1)

Siguiendo `PolyConecta.Contpaq/AGENTS.md` y `scripts/vps/`:

1. Publicar el bridge de la rama y arrancarlo. El registro muestra un solo inicio del SDK, con los dos inicios de sesión, y ninguna ventana.
2. Durante una hora, correr la sonda de lectura cada minuto y la suite de contrato de lecturas contra el real. Sin nuevos inicios de sesión (SC-006).
3. Forzar el tiempo límite de una llamada (`BridgeConfig__Sdk__TimeoutSegundos=1` con la sonda lenta): responde `SDK_TIMEOUT`, se reinicia y la tarea lo levanta.
4. Ajustar `BridgeConfig__ReinicioDiario` a dos minutos adelante: cierra limpio y vuelve.
5. Cotejo de T-06: cambiar una existencia en la UI de CONTPAQi, medir cuándo la refleja `GET /inventory/stocks` y comparar F-01 y F-02 con la UI. Medir la duración de la lectura completa de productos y clientes (R-05). Todo a la matriz y a `evidence/F1/`.

## 7. Cierre

La fase cumple los dos cierres de [06 §9](../../../docs/diseno/06-constitucion-tecnica.md) y el guion de R1 (FR-032) se recorre con los datos de ejemplo.

### Tabla de usuarios de R1

| Usuario | Grupos (planta) | Para probar |
| :--- | :--- | :--- |
| `admin` | Administrador (PIM, SC) | Configuración |
| `sistemas` | Sistemas (PIM) | Sincronización |
| `ac1` | Atención a Clientes (PIM) | Captura y confirmación |
| `comercial1` | Comercial (PIM) | Primera firma |
| `cobranza1` | Cobranza (PIM) | Segunda firma |
| `cobranza-suplente` | Cobranza, suplente (PIM) | D-38 |
| `doble` | Comercial y Cobranza (PIM) | RF-4, D-34 |
| `planner-pim` | Planner (PIM) | Solo lectura del pedido |
