# Instrucciones para GitHub Copilot

Lee primero `AGENTS.md` y los documentos de `docs/` relevantes a la tarea. Respeta la arquitectura cliente-servidor y las reglas de negocio, seguridad y calidad definidas allí.

Trabaja en incrementos pequeños. Antes de crear una entidad, endpoint o pantalla, comprueba si ya existe. No dupliques lógica de negocio. No implementes lógica crítica de stock en React. Toda venta/recepción de compra debe validarse y persistirse atómicamente en el servidor.

Después de cada cambio ejecuta las pruebas/build pertinentes o explica exactamente por qué no pudieron ejecutarse. No afirmes que una prueba pasó si no ejecutaste el comando.
