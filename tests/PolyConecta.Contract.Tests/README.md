# Suite de contrato `bridge-v1`

Prueba el contrato de [`docs/contratos/bridge-v1.md`](../../docs/contratos/bridge-v1.md) solo por HTTP (CT-23, D-122). No referencia al bridge ni a PolyConecta, así que corre igual contra el bridge simulado (CI y desarrollo) y contra el bridge real (laboratorio `_LAB`).

| Variable | Valor por omisión | Para qué |
| :--- | :--- | :--- |
| `BRIDGE_URL` | sin valor: la suite se omite | Bridge bajo prueba. Sin esta variable, `dotnet test Polyconecta.slnx` omite la suite con un motivo explícito |
| `BRIDGE_CALLBACK_SECRET` | `secreto-de-pruebas` | El mismo que `BridgeConfig__CallbackSecret` del bridge; la suite verifica la firma de cada callback |
| `CALLBACK_HOST` | `localhost` | Nombre con el que el bridge alcanza a la suite para mandarle los callbacks |

```bash
BridgeConfig__Mode=Simulated BridgeConfig__CallbackSecret=secreto-de-pruebas dotnet run --project PolyConecta.Contpaq
BRIDGE_URL=http://localhost:5005 BRIDGE_CALLBACK_SECRET=secreto-de-pruebas \
  dotnet test --project tests/PolyConecta.Contract.Tests
```

Las pruebas de `Simulado/` provocan errores, demoras y callbacks perdidos con `PUT /admin/simulated/faults` y se omiten contra el bridge real.
