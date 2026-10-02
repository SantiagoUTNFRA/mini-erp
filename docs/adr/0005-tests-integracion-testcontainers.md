# ADR 0005: Tests de integración contra MySQL con Testcontainers

- Estado: Aceptada
- Fecha: 2026-10-02

## Contexto
La primera feature (gestión de clientes, `specs/001-gestion-clientes/`) agrega persistencia real: un
índice único sobre el CUIT, orden y paginación hechos por la base, y la traducción del error de clave
duplicada del proveedor de MySQL. Los tests de integración de los endpoints tienen que verificar ese
comportamiento contra un motor real, no contra una simulación.

ADR-0004 dejó explícito que revisaría la estrategia de entorno si los tests de integración
necesitaban otra. El entorno de `compose.yaml` está pensado para desarrollo: su usuario `minierp` no
tiene permisos para crear otra base, y sus datos son los del desarrollador.

`Testcontainers.MySql` al 2026-10-02: versión 4.15.0, licencia MIT.

## Opciones consideradas
1. Testcontainers (MySQL efímero por ejecución de la suite) — pros: prueba el motor real; reproducible
   en cualquier máquina con Docker; no depende del entorno de `compose` ni de sus datos / contras:
   `dotnet test` requiere Docker corriendo; la suite tarda más por el arranque del contenedor.
2. La base de `compose.yaml` con una base `minierp_test` — pros: sin paquetes nuevos / contras: exige
   tener el entorno levantado; el usuario de aplicación no puede crear la base; mezcla datos de tests
   con los de desarrollo.
3. Adaptador de persistencia en memoria en los tests de endpoints — pros: rápido, sin Docker /
   contras: no prueba el índice único, la concurrencia ni el SQL real; útil como complemento, no como
   reemplazo.
4. Proveedor InMemory de EF Core — pros: trivial de configurar / contras: no respeta índices únicos ni
   transacciones; Microsoft lo desaconseja para tests.

## Decisión
Testcontainers con el paquete `Testcontainers.MySql` 4.15.0 e imagen `mysql:9.7`, la misma que
`compose.yaml`, para que tests y desarrollo usen el mismo motor y versión.

- Un contenedor por ejecución de la suite de integración (assembly fixture de xUnit v3), no uno por
  test.
- La API bajo test apunta al contenedor sobrescribiendo `ConnectionStrings:MiniErp` desde
  `WebApplicationFactory<Program>`.
- Los tests que comparten la base no corren en paralelo entre sí y la limpian antes de cada uno.

## Consecuencias
- Positivas: los tests de integración verifican el adaptador real; cambiar de motor de base de datos
  es cambiar la imagen y el módulo de Testcontainers, y la misma suite valida la migración
  (experimento de ADR-0002).
- Negativas / lo que aceptamos pagar: `dotnet test` requiere Docker Desktop corriendo; la primera
  ejecución descarga la imagen; la suite de integración es más lenta que una en memoria.
- Qué nos haría revisar esta decisión: que el tiempo de la suite frene el ciclo de desarrollo, o que
  un entorno de CI no permita contenedores.
