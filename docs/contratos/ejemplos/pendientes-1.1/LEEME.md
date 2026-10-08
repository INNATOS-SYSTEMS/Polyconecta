# Ejemplos `1.1` por implementar

Cargas del contrato `1.1` que el bridge todavía no acepta: `ALTA_PEDIDO` con `agente` (D-153). Están fuera de `ejemplos/` porque `LectorComandosTests` exige que todo ejemplo de esa carpeta sea un comando válido para el bridge, y esa prueba no se debilita.

L1 los mueve a `ejemplos/` en la tarea L1-T010 de la spec 003, cuando el lector de comandos acepte `agente` y la validación responda `AGENTE_NO_EXISTE`.
