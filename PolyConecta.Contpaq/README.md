# PolyConecta.Contpaq (bridge de CONTPAQi)

Servicio .NET 8 **x86** que aísla toda la comunicación con CONTPAQi Comercial Premium. Es el único componente que habla con el ERP (Principio II de la constitución). Se despliega en Windows, junto a la instalación de CONTPAQi.

## Qué hace

- **Escribe** documentos y movimientos con el SDK nativo (`ContpaqiSdkGateway`, `ContpaqiSdkNative`), con circuit breaker y en un solo hilo.
- **Lee** catálogos y existencias por SQL directo con `NOLOCK` (`SqlReadRepository`). Nunca hace `INSERT` ni `UPDATE` en tablas `adm*`.
- **Outbox propio** en SQLite (`bridge_outbox.db`), con reintentos y dead letter queue.
- **API y dashboard** en el puerto `5005`: transacciones, catálogos, DLQ, logs y métricas (`Api/Controllers`), con un `DashboardHub` en vivo y `CorrelationMiddleware` para trazar solicitudes de extremo a extremo.
- **Webhooks** de notificación de resultado (`WebhookDispatcher`).

## Advertencias

- **La cadena de conexión a CONTPAQi no se versiona.** El bridge la lee de la variable de entorno `BridgeConfig__SqlConnectionString` y no arranca si falta. En desarrollo, usa `dotnet user-secrets set "BridgeConfig:SqlConnectionString" "..." --project PolyConecta.Contpaq`. La contraseña de `sa` que quedó en el historial de git **debe rotarse**, y el bridge debe usar un login de solo lectura (D-51).
- El contrato de movimientos (WIP, varios lotes, fraccionamiento, backorder, enlace Remisión ↔ Pedido) **no está verificado** contra un CONTPAQi real. Se valida con la [matriz de pruebas](../docs/contpaq/MATRIZ_PRUEBAS_SDK_WIP_LOTES.md) usando `tools/sdk-lab`.
- El caso G-01 (documento huérfano cuando falla el movimiento) está pendiente.

## Construir y desplegar

```bash
./scripts/build.sh    # paquete win-x86
./scripts/deploy.sh   # despliegue al VPS Windows
```

Referencias obligatorias: [Referencia_SDK_CONTPAQi.md](../docs/contpaq/Referencia_SDK_CONTPAQi.md) y [Referencia_BD_CONTPAQi.md](../docs/contpaq/Referencia_BD_CONTPAQi.md).
