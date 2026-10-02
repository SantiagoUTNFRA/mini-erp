# Research: Gestión de clientes

**Feature**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Fecha**: 2026-10-02

Cada decisión sigue el formato Decisión / Motivo / Alternativas. Las que requieren aprobación del
tech lead (Principio V) están marcadas **[requiere OK]**; ambas (R1 y R2) fueron
aprobadas el 2026-10-02.

## R1. Paquetes y versiones [requiere OK]

**Decisión**: agregar a `Directory.Packages.props`:

| Paquete | Versión | Proyecto que lo usa | Para qué |
|---|---|---|---|
| `MySql.EntityFrameworkCore` | 10.0.9 | `MiniErp.Persistence.EfCore` | Proveedor MySQL (ADR-0003) |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | `MiniErp.Persistence.EfCore` (`PrivateAssets=all`) | Generar migraciones |
| `Testcontainers.MySql` | 4.15.0 | `MiniErp.IntegrationTests` | MySQL efímero para tests (ver R2) |

Y la herramienta local `dotnet-ef` 10.0.12 en un manifiesto `.config/dotnet-tools.json` dentro de
`backend/` (versionada con el repo, no instalada globalmente).

**Motivo**: versiones verificadas en NuGet el 2026-10-02. `MySql.EntityFrameworkCore` 10.0.9 depende
de EF Core ≥ 10.0.9; al sumar `Design` 10.0.12, EF Core se resuelve a 10.0.12 (patch dentro de la
misma minor, compatible). `Testcontainers.MySql` es MIT.

**Alternativas**: fijar `Design` en 10.0.9 para igualar exactamente al proveedor; descartado porque
dejaría EF Core sin los fixes de 10.0.10–10.0.12 sin ganancia concreta. Si aparece una
incompatibilidad, ese es el plan B.

## R2. Estrategia de tests de integración contra la base [requiere OK + ADR-0005]

**Decisión**: los tests de integración levantan un MySQL efímero con Testcontainers (imagen
`mysql:9.7`, la misma que `compose.yaml`), uno por ejecución de la suite, y la API apunta a él
mediante `WebApplicationFactory<Program>`. Se registra en un **ADR-0005** (Principio IV), porque
ADR-0004 dejó explícito que revisaría la estrategia si los tests de integración necesitaban otra.

**Motivo**: los tests verifican el comportamiento real del adaptador (índice único, orden, paginación,
traducción del error de duplicado), son reproducibles en cualquier máquina con Docker y no dependen
de que el entorno de `compose` esté levantado ni de sus datos.

**Alternativas**:
- Usar la base de `compose.yaml` con una base `minierp_test`: depende de que el entorno esté
  levantado y el usuario `minierp` no tiene permisos para crear otra base; contamina datos locales.
- Reemplazar el adaptador por uno en memoria en los tests de endpoints: rápido, pero no prueba la
  unicidad concurrente ni el SQL real; queda como complemento posible, no como reemplazo.
- Proveedor InMemory de EF Core: no respeta índices únicos; Microsoft lo desaconseja para tests.

**Costo aceptado**: `dotnet test` pasa a requerir Docker Desktop corriendo, y la suite de
integración tarda más (arranque del contenedor, una vez por ejecución).

## R3. Identificador del cliente

**Decisión**: `Guid` versión 7 (`Guid.CreateVersion7()`), generado en el dominio al crear el cliente.

**Motivo**: el identificador no depende de la base (un autoincremental ataría la identidad al motor,
contra el Principio I); la versión 7 es ordenable por tiempo, lo que evita la fragmentación de índices
de los GUID aleatorios; y no expone la cantidad de clientes en las URLs.

**Alternativas**: `int` autoincremental (identidad generada por la base, peor para reemplazabilidad);
`Guid` v4 (aleatorio, fragmenta el índice primario).

## R4. Modelo de dominio vs. modelo de persistencia

**Decisión**: entidad de dominio `Customer` en `MiniErp.Domain`, sin atributos ni constructores pensados
para EF Core; el adaptador tiene su propio `CustomerRecord` y mapea en ambas direcciones.

**Motivo**: ADR-0002 prohíbe que tipos del adaptador aparezcan en el núcleo y acepta el costo de los
mapeos. Mapear la entidad de dominio directamente con EF Core obligaría a moldear el dominio para el
ORM (constructores privados, setters), y reemplazar el ORM tocaría `Domain`, que es lo que el
experimento mide.

**Alternativas**: mapear `Customer` directo con Fluent API (menos código, pero acopla la forma del
dominio al ORM).

## R5. Validación y reglas del dominio

**Decisión**: la validación vive en el dominio, en value objects con fábricas que devuelven errores
en lugar de lanzar excepciones: `TaxId` (CUIT/CUIL), `LegalName`, `Email`, `Phone`, `Address`.
`Customer.Create`/`Customer.Update` acumulan los errores de todos los campos y devuelven el cliente o
la lista completa (FR-004, SC-004). Sin librerías de validación.

**Algoritmo del CUIT/CUIL** (FR-004): se quitan guiones y espacios; deben quedar exactamente 11
dígitos. Con pesos `5,4,3,2,7,6,5,4,3,2` sobre los primeros 10 dígitos, `r = suma mod 11`; el dígito
verificador esperado es `0` si `r = 0`, y `11 − r` en otro caso; si da `10`, el número es inválido.
Se guarda normalizado (11 dígitos, sin guiones). No se valida el prefijo (20, 23, 27, 30...) porque la
spec no lo pide.

**Normalizaciones** (edge cases de la spec): `Trim` en todos los textos; texto vacío o solo espacios
= ausente; email en minúsculas; largos medidos después del `Trim`.

**Motivo**: las reglas de negocio quedan en el núcleo, testeables con unit tests puros y sin
dependencias; acumular errores cumple SC-004 sin lógica extra en la API.

**Alternativas**: FluentValidation (paquete nuevo, y la validación quedaría en la capa de aplicación o
API, no en el dominio); Data Annotations en los DTOs (reglas en el adaptador HTTP, no en el núcleo).

## R6. Resultados de los casos de uso y manejo de errores

**Decisión**: los casos de uso devuelven un resultado explícito (éxito, o error de tipo `Validation`,
`NotFound` o `Conflict`) definido en `MiniErp.Application`, sin excepciones para el flujo esperado.
La API traduce cada tipo a `TypedResults`: `ValidationProblem` (400), `NotFound` (404) y `Conflict`
(409) con `ProblemDetails`.

**Motivo**: el contrato de errores queda tipado y exhaustivo; los errores de negocio no son
excepcionales. Cumple el Principio II.

**Alternativas**: excepciones de dominio + middleware de excepciones (flujo implícito, más difícil de
testear); librerías de Result (paquete nuevo innecesario).

## R7. Unicidad del CUIT bajo concurrencia

**Decisión**: doble barrera.
1. El caso de uso consulta por el puerto si el CUIT ya existe (excluyendo al propio cliente al
   modificar) y responde `Conflict` con un mensaje claro.
2. La base tiene un índice único sobre el CUIT normalizado. Si dos altas simultáneas pasan el paso 1,
   el adaptador captura el error de clave duplicada del proveedor (error MySQL 1062) y lo traduce a
   una excepción definida en `Application` (`DuplicateTaxIdException`), que el caso de uso convierte
   en `Conflict`.

**Motivo**: el índice garantiza la regla (edge case "dos altas simultáneas") y la traducción en el
adaptador mantiene los tipos del proveedor fuera del núcleo (Principio I).

**Alternativas**: solo el chequeo previo (falla con concurrencia); solo el índice (mensajes de error
dependientes del proveedor y lógica de negocio escondida en la base).

## R8. Regla "no se da de baja un cliente con facturas" (FR-009)

**Decisión**: puerto `ICustomerInvoiceChecker.HasInvoicesAsync(customerId)` en `Application`. El
caso de uso de baja lo consulta antes de eliminar. Mientras no exista facturación, el adaptador de
EF Core lo implementa devolviendo siempre `false`, con un comentario que apunta a la feature de
facturación.

**Motivo**: la regla queda implementada y testeada hoy (unit test con un fake que devuelve `true`),
y la feature de facturación solo cambia el adaptador, sin tocar el caso de uso.

**Alternativas**: no implementar la regla hasta que existan facturas (la spec la exige y quedaría sin
test); agregar una tabla de facturas vacía (anticipa un modelo que no está especificado).

## R9. Migraciones y esquema

**Decisión**: migraciones de EF Core en `MiniErp.Persistence.EfCore/Migrations`, generadas con
`dotnet ef`. En `Development`, la API aplica las migraciones pendientes al arrancar mediante una
extensión del adaptador; los tests de integración heredan ese comportamiento porque corren en
`Development`. Fuera de Development no se migra automáticamente (decisión diferida a cuando haya un
entorno de despliegue).

**Motivo**: un solo camino para crear el esquema en desarrollo y en tests; el `compose` levanta una
base vacía y la API la deja lista.

**Alternativas**: `EnsureCreated` (incompatible con migraciones posteriores); aplicar migraciones
solo a mano con `dotnet ef database update` (paso manual fácil de olvidar; queda disponible igual).

## R10. Configuración y registro de dependencias

**Decisión**:
- Connection string `ConnectionStrings:MiniErp` en `dotnet user-secrets` del proyecto `MiniErp.Api`
  (ADR-0004); en los tests, la sobrescribe la factory con la del contenedor.
- El adaptador expone una extensión de registro en DI (`AddEfCorePersistence`). Los casos de uso se
  registran en el composition root (`MiniErp.Api`), no en `Application`, para no sumarle a
  `Application` un paquete de DI.

**Motivo**: mantiene `Application` sin dependencias externas y deja el reemplazo de tecnología en un
único punto (`Program.cs` + el adaptador).

## R11. Contrato HTTP

**Decisión**: grupo `/api/customers` con `GET` (listado), `GET /{id}`, `POST`, `PUT /{id}` y
`DELETE /{id}`. Listado con `page` y `pageSize` por query string. Condición frente al IVA como string
en JSON. Detalle en [contracts/customers-api.md](contracts/customers-api.md).

**Motivo**: convenciones REST estándar y las de `backend/CLAUDE.md` (`MapGroup`, `TypedResults`,
`ProblemDetails`, `record`). `PUT` refleja la semántica de reemplazo completo (Clarifications, Q1).
