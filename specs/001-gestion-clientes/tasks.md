---

description: "Lista de tareas de la feature 001-gestion-clientes"
---

# Tasks: Gestión de clientes

**Input**: Design documents from `/specs/001-gestion-clientes/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts/customers-api.md](contracts/customers-api.md),
[quickstart.md](quickstart.md)

**Tests**: OBLIGATORIOS. La constitución (Principio III) exige un test para todo cambio de
comportamiento. En cada fase, los tests se escriben primero y deben FALLAR antes de implementar.

**Organization**: tareas agrupadas por historia de usuario, para implementar y validar cada una por
separado.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario a la que pertenece (US1, US2, US3, US4)
- Rutas relativas a la raíz del repo. Comandos `dotnet` desde `backend/`.

## Convenciones para todas las tareas

- Código, identificadores y comentarios en inglés; glosario en [data-model.md](data-model.md).
- File-scoped namespaces, `Nullable` sin `!` injustificados, async con `CancellationToken` de punta a
  punta, `record` para DTOs.
- Tests: nombre `Method_Scenario_ExpectedResult`, estructura Arrange / Act / Assert, y
  `TestContext.Current.CancellationToken` en las llamadas async (si no, `xUnit1051` rompe el build).
- Ningún tipo de EF Core ni de MySQL en `MiniErp.Domain` ni en `MiniErp.Application` (Principio I).
- No agregar paquetes fuera de los aprobados en [research.md](research.md) (R1).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: decisión registrada, paquetes aprobados y herramientas.

- [X] T001 Escribir el ADR-0005 en docs/adr/0005-tests-integracion-testcontainers.md con el formato de los ADR existentes (Estado: Aceptada, Fecha: 2026-10-02, Contexto, Opciones consideradas, Decisión, Consecuencias), a partir de research.md R2: MySQL efímero con `Testcontainers.MySql` 4.15.0 e imagen `mysql:9.7`; alternativas descartadas (base de compose, adaptador en memoria, proveedor InMemory de EF Core); costo: `dotnet test` requiere Docker. Va en su propio commit (`docs:`)
- [X] T002 Agregar a backend/Directory.Packages.props, y solo ahí, las versiones aprobadas: `MySql.EntityFrameworkCore` 10.0.9, `Microsoft.EntityFrameworkCore.Design` 10.0.12, `Testcontainers.MySql` 4.15.0
- [X] T003 [P] Referenciar `MySql.EntityFrameworkCore` y `Microsoft.EntityFrameworkCore.Design` (este último con `PrivateAssets="all"`) sin `Version` en backend/src/MiniErp.Persistence.EfCore/MiniErp.Persistence.EfCore.csproj
- [X] T004 [P] Referenciar `Testcontainers.MySql` sin `Version` en backend/tests/MiniErp.IntegrationTests/MiniErp.IntegrationTests.csproj
- [X] T005 [P] Crear el manifiesto de herramientas locales backend/dotnet-tools.json con `dotnet new tool-manifest` (en .NET 10 el SDK lo crea en la raíz, no en `.config/`) e instalar `dotnet tool install dotnet-ef --version 10.0.12` (local, nunca global)
- [X] T006 [P] Inicializar user-secrets en backend/src/MiniErp.Api/MiniErp.Api.csproj con `dotnet user-secrets init --project src/MiniErp.Api` (agrega `UserSecretsId`; no guardar ningún secreto en archivos versionados)
- [X] T007 Verificar sobre backend/MiniErp.slnx que `dotnet build` termina sin warnings y `dotnet test` sigue en verde con los paquetes nuevos

**Checkpoint**: paquetes y herramientas listos; nada de comportamiento nuevo todavía.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: modelo de dominio, puertos, adaptador de persistencia, composición de la API e
infraestructura de tests de integración que usan las 4 historias.

**⚠️ CRITICAL**: ninguna historia puede empezar hasta completar esta fase.

### Tests del dominio (escribir primero; deben fallar)

- [ ] T008 [P] Unit tests de `TaxId` en backend/tests/MiniErp.UnitTests/Customers/TaxIdTests.cs: `"20-12345678-6"` y `"20 12345678 6"` son válidos y se normalizan a `"20123456786"`; `"20123456785"` es inválido (dígito verificador incorrecto); menos o más de 11 dígitos, letras, vacío y solo espacios son inválidos; un número cuyo verificador calculado es 10 es inválido; un número con resto 0 exige verificador 0. Algoritmo: pesos `5,4,3,2,7,6,5,4,3,2` sobre los primeros 10 dígitos, `r = suma mod 11`, verificador = `0` si `r = 0`, `11 − r` en otro caso, inválido si da `10`
- [ ] T009 [P] Unit tests de los value objects en backend/tests/MiniErp.UnitTests/Customers/CustomerValueObjectTests.cs: `LegalName` "Trim; no vacío; ≤ 200 caracteres"; `Email` "Trim + minúsculas; un único `@`, parte local y dominio no vacíos, dominio con al menos un `.` que no esté al inicio ni al final, y sin espacios; ≤ 254 caracteres" (casos inválidos: sin `@`, dos `@`, local o dominio vacíos, dominio sin `.`, `.` al inicio o al final del dominio, espacios internos); `Phone` "Trim; solo dígitos, espacios, `-`, `(`, `)` y un `+` inicial; ≥ 6 dígitos; ≤ 30 caracteres" (incluye `+` en el medio como inválido); `Address` "Trim; texto libre; ≤ 300 caracteres". El largo se mide después del Trim, y un valor más largo se rechaza, nunca se trunca
- [ ] T010 [P] Unit tests de `Customer` en backend/tests/MiniErp.UnitTests/Customers/CustomerTests.cs: `Create` con datos válidos asigna un `Id` Guid versión 7; `Create` con varios campos inválidos devuelve **todos** los errores juntos, cada uno con su campo (SC-004); `vatCondition` acepta `registeredTaxpayer`, `simplifiedRegime`, `exempt` y `finalConsumer` sin distinguir mayúsculas, y rechaza cualquier otro valor, incluidos los números; opcionales vacíos o con solo espacios quedan ausentes; `Update` reemplaza todos los datos (un opcional que no se envía queda ausente) y no cambia el `Id`

### Dominio

- [ ] T011 [P] Crear `FieldError` (campo + mensaje) y `Validated<T>` (valor válido o lista de `FieldError`) en backend/src/MiniErp.Domain/Common/FieldError.cs y backend/src/MiniErp.Domain/Common/Validated.cs
- [ ] T012 [P] Crear el enum `VatCondition` (`RegisteredTaxpayer`, `SimplifiedRegime`, `Exempt`, `FinalConsumer`) en backend/src/MiniErp.Domain/Customers/VatCondition.cs
- [ ] T013 [P] Implementar `TaxId` (fábrica que devuelve `Validated<TaxId>`, normaliza quitando `-` y espacios, valida 11 dígitos y dígito verificador según T008) en backend/src/MiniErp.Domain/Customers/TaxId.cs
- [ ] T014 [P] Implementar `LegalName`, `Email`, `Phone` y `Address` con fábricas que devuelven `Validated<T>` y las reglas de T009, en backend/src/MiniErp.Domain/Customers/LegalName.cs, Email.cs, Phone.cs y Address.cs
- [ ] T015 Implementar `Customer` en backend/src/MiniErp.Domain/Customers/Customer.cs: `Create(...)` y `Update(...)` reciben los datos como strings, validan todos los campos (incluido el parseo de `vatCondition`) y acumulan los errores; `Restore(Guid id, LegalName, TaxId, VatCondition, Email?, Phone?, Address?)` reconstruye desde valores ya válidos (lo usa el adaptador). Sin atributos ni constructores pensados para EF Core (R4). Depende de T011–T014

### Aplicación (puertos y resultados)

- [ ] T016 [P] Crear el resultado de los casos de uso (éxito, o error `Validation` con `FieldError`s, `NotFound`, o `Conflict` con título) en backend/src/MiniErp.Application/Common/Result.cs (R6)
- [ ] T017 [P] Crear los puertos en backend/src/MiniErp.Application/Customers/: `ICustomerRepository.cs` (`GetByIdAsync`, `ListAsync(page, pageSize)` → `CustomerPage`, `ExistsByTaxIdAsync(taxId, excludingId?)`, `AddAsync`, `UpdateAsync`, `DeleteAsync`), `CustomerPage.cs` (ítems + total), `ICustomerInvoiceChecker.cs` (`HasInvoicesAsync(customerId)`) y `DuplicateTaxIdException.cs`. Todo async y con `CancellationToken`
- [ ] T018 [P] Crear los fakes de los puertos para unit tests en backend/tests/MiniErp.UnitTests/Customers/Fakes/InMemoryCustomerRepository.cs (configurable para lanzar `DuplicateTaxIdException` en `AddAsync`/`UpdateAsync` y simular la carrera de R7) y backend/tests/MiniErp.UnitTests/Customers/Fakes/FakeCustomerInvoiceChecker.cs (resultado configurable)

### Adaptador de persistencia

- [ ] T019 Crear `CustomerRecord` y su configuración Fluent en backend/src/MiniErp.Persistence.EfCore/Customers/CustomerRecord.cs y CustomerConfiguration.cs: tabla `customers`; `id` PK; `legal_name` NOT NULL largo 200; `tax_id` NOT NULL largo 11 con índice único `ux_customers_tax_id`; `vat_condition` NOT NULL largo 30, guardado como texto; `email` NULL largo 254; `phone` NULL largo 30; `address` NULL largo 300; índice `ix_customers_legal_name`. No fijar el tipo de columna del Guid
- [ ] T020 Crear `MiniErpDbContext` (con `ApplyConfigurationsFromAssembly`) en backend/src/MiniErp.Persistence.EfCore/MiniErpDbContext.cs y una fábrica de diseño `IDesignTimeDbContextFactory` en backend/src/MiniErp.Persistence.EfCore/MiniErpDbContextFactory.cs, para generar migraciones sin depender de `MiniErp.Api` (la connection string de diseño sale de una variable de entorno, con un valor no secreto por defecto, porque `migrations add` no se conecta)
- [ ] T021 Implementar el mapeo `CustomerRecord` ↔ `Customer` (usa `Customer.Restore`; si un registro no se puede reconstruir, lanza `InvalidOperationException`, porque es corrupción de datos) en backend/src/MiniErp.Persistence.EfCore/Customers/CustomerMapping.cs
- [ ] T022 Implementar `CustomerRepository : ICustomerRepository` en backend/src/MiniErp.Persistence.EfCore/Customers/CustomerRepository.cs: listado ordenado por `legal_name` con `Skip/Take` y total; `ExistsByTaxIdAsync` excluyendo el id indicado; en `AddAsync`/`UpdateAsync`, capturar el `DbUpdateException` cuyo error interno de MySQL sea el número 1062 (clave duplicada) y relanzarlo como `DuplicateTaxIdException` (R7)
- [ ] T023 [P] Implementar `EfCoreCustomerInvoiceChecker : ICustomerInvoiceChecker` que devuelve siempre `false`, con un comentario que explica que lo reemplaza la feature de facturación (R8), en backend/src/MiniErp.Persistence.EfCore/Customers/EfCoreCustomerInvoiceChecker.cs
- [ ] T024 Crear backend/src/MiniErp.Persistence.EfCore/DependencyInjection.cs con `AddEfCorePersistence(this IServiceCollection, string connectionString)` (registra el `DbContext` con `UseMySQL`, el repositorio y el invoice checker) y `MigrateDatabaseAsync(this IServiceProvider, CancellationToken)` (R9, R10)
- [ ] T025 Generar la migración inicial con `dotnet ef migrations add CreateCustomers --project src/MiniErp.Persistence.EfCore --startup-project src/MiniErp.Persistence.EfCore` en backend/src/MiniErp.Persistence.EfCore/Migrations/ y revisar que crea exactamente la tabla e índices de T019

### Composición de la API

- [ ] T026 [P] Crear los records del contrato en backend/src/MiniErp.Api/Customers/CustomerContracts.cs: `CustomerRequest` (todos los campos `string?`, incluido `vatCondition`), `CustomerResponse` (`taxId` normalizado, `vatCondition` en camelCase, opcionales ausentes como `null`) y `CustomerPageResponse` (`items`, `page`, `pageSize`, `totalCount`), según contracts/customers-api.md
- [ ] T027 [P] Crear la traducción de resultados a `TypedResults` en backend/src/MiniErp.Api/Customers/ResultMapping.cs: `Validation` → `ValidationProblem` (400) con las claves de campo del request en camelCase; `NotFound` → 404 `ProblemDetails`; `Conflict` → 409 `ProblemDetails` con el título del error
- [ ] T028 Crear backend/src/MiniErp.Api/Customers/CustomerEndpoints.cs con `MapCustomers(this IEndpointRouteBuilder)` y `MapGroup("/api/customers")`, todavía sin endpoints
- [ ] T029 Componer en backend/src/MiniErp.Api/Program.cs: leer `ConnectionStrings:MiniErp` y fallar al arrancar con un mensaje claro si falta; `AddEfCorePersistence`; `AddProblemDetails`; en Development, `MigrateDatabaseAsync` antes de `Run`; `MapCustomers()`. `Program.cs` solo compone, sin lógica

### Infraestructura de tests de integración

- [ ] T030 Crear backend/tests/MiniErp.IntegrationTests/Infrastructure/MiniErpApiFactory.cs: `WebApplicationFactory<Program>` + `IAsyncLifetime` que arranca un `MySqlContainer` con imagen `mysql:9.7`, sobrescribe `ConnectionStrings:MiniErp` con la del contenedor **con `builder.UseSetting("ConnectionStrings:MiniErp", ...)` dentro de `ConfigureWebHost`** (no con `ConfigureAppConfiguration`: con Minimal APIs se aplica después de que `Program.cs` lee la configuración y la API no vería el valor). Expone `ResetDatabaseAsync()` (vacía la tabla `customers` con el `MiniErpDbContext` resuelto en un `Services.CreateScope()`) y `SeedAsync(params Customer[])` (agrega clientes con `ICustomerRepository` resuelto en un `CreateScope()`, porque ambos son scoped). Registrarla como assembly fixture de xUnit v3 (un contenedor por ejecución)
- [ ] T031 Desactivar la paralelización entre colecciones (`"parallelizeTestCollections": false`) en backend/tests/MiniErp.IntegrationTests/xunit.runner.json, porque los tests comparten la base
- [ ] T032 Adaptar backend/tests/MiniErp.IntegrationTests/OpenApiTests.cs para usar `MiniErpApiFactory`, porque la API ahora necesita base al arrancar
- [ ] T033 Verificar el checkpoint sobre backend/MiniErp.slnx: `dotnet build` sin warnings; `dotnet test` en verde (T008–T010, `DependencyRuleTests` y `OpenApiTests` contra el contenedor)

**Checkpoint**: la base se crea por migración, la API arranca y el dominio está testeado. Pueden empezar
las historias.

---

## Phase 3: User Story 1 - Dar de alta un cliente (Priority: P1) 🎯 MVP

**Goal**: registrar un cliente válido y rechazar duplicados y datos inválidos.

**Independent Test**: `POST /api/customers` con datos válidos devuelve `201` con `Location` e id; un
segundo `POST` con el mismo CUIT devuelve `409` (quickstart, pasos 1–3).

### Tests for User Story 1 (escribir primero; deben fallar)

- [ ] T034 [P] [US1] Unit tests del caso de uso en backend/tests/MiniErp.UnitTests/Customers/CreateCustomerTests.cs (con los fakes de T018): éxito agrega el cliente; CUIT ya existente → `Conflict` "Customer already exists" sin agregar nada; el repositorio lanza `DuplicateTaxIdException` → `Conflict`; datos inválidos → `Validation` con todos los campos y sin agregar nada
- [ ] T035 [P] [US1] Integration tests en backend/tests/MiniErp.IntegrationTests/Customers/CreateCustomerEndpointTests.cs: `201` con `Location: /api/customers/{id}` y `taxId` = `"20123456786"` para `"20-12345678-6"`; mismo CUIT sin guiones → `409` con title `Customer already exists`; CUIT `"20-12345678-5"`, email `"foo"` y sin `legalName` → un solo `400` con errores en `taxId`, `email` y `legalName`; `vatCondition` inválido aparece como error de `vatCondition`; `legalName` de 201 caracteres → `400`; dos `POST` simultáneos con el mismo CUIT → exactamente un `201` y un `409`. Llamar a `ResetDatabaseAsync` antes de cada test

### Implementation for User Story 1

- [ ] T036 [US1] Implementar el caso de uso `CreateCustomer` (comando con los datos como strings → `Customer.Create` → `ExistsByTaxIdAsync` → `AddAsync`, traduciendo `DuplicateTaxIdException` a `Conflict`) en backend/src/MiniErp.Application/Customers/CreateCustomer.cs
- [ ] T037 [US1] Agregar `POST /` (`201 Created` con `Location` y `CustomerResponse`) en backend/src/MiniErp.Api/Customers/CustomerEndpoints.cs y registrar `CreateCustomer` en backend/src/MiniErp.Api/Program.cs
- [ ] T038 [US1] Agregar los requests 1–3 del quickstart a backend/src/MiniErp.Api/MiniErp.Api.http

**Checkpoint**: US1 completa y testeada. Es el MVP.

---

## Phase 4: User Story 2 - Consultar clientes (Priority: P2)

**Goal**: listado paginado y ordenado, y detalle por id.

**Independent Test**: con clientes cargados por el repositorio, `GET /api/customers` los devuelve
ordenados por razón social con `totalCount`, y `GET /api/customers/{id}` devuelve el detalle
(quickstart, pasos 4–7).

### Tests for User Story 2 (escribir primero; deben fallar)

- [ ] T039 [P] [US2] Unit tests en backend/tests/MiniErp.UnitTests/Customers/GetCustomerTests.cs y ListCustomersTests.cs: detalle de un id inexistente → `NotFound`; `page` por defecto 1 y `pageSize` por defecto 20; `page` < 1, `pageSize` 0 o negativo, y `pageSize` 101 → `Validation` con clave `page` o `pageSize`
- [ ] T040 [P] [US2] Integration tests en backend/tests/MiniErp.IntegrationTests/Customers/ListCustomersEndpointTests.cs y GetCustomerEndpointTests.cs, cargando los datos con `SeedAsync` de la factory (sin depender del `POST`): orden alfabético por `legalName` (datos de prueba sin variantes de mayúsculas ni acentos, para no depender del collation de la base); `totalCount` correcto; `pageSize` por defecto 20; página fuera de rango → `200` con `items` vacío; tabla vacía → `200` con `items` vacío; `pageSize=101` → `400` con error en `pageSize`; detalle → `200` con todos los campos; id inexistente → `404`; id con formato inválido → `404`

### Implementation for User Story 2

- [ ] T041 [P] [US2] Implementar `GetCustomer` en backend/src/MiniErp.Application/Customers/GetCustomer.cs
- [ ] T042 [P] [US2] Implementar `ListCustomers` (valida `page` ≥ 1 y `pageSize` entre 1 y 100, con valores por defecto 1 y 20) en backend/src/MiniErp.Application/Customers/ListCustomers.cs
- [ ] T043 [US2] Agregar `GET /` y `GET /{id:guid}` en backend/src/MiniErp.Api/Customers/CustomerEndpoints.cs y registrar los casos de uso en backend/src/MiniErp.Api/Program.cs
- [ ] T044 [US2] Agregar los requests 4–7 del quickstart a backend/src/MiniErp.Api/MiniErp.Api.http

**Checkpoint**: US1 y US2 funcionan por separado.

---

## Phase 5: User Story 3 - Modificar los datos de un cliente (Priority: P3)

**Goal**: reemplazo completo de los datos de un cliente existente.

**Independent Test**: con un cliente cargado por el repositorio, `PUT /api/customers/{id}` sin `phone`
devuelve `200` con `phone: null` (quickstart, paso 8).

### Tests for User Story 3 (escribir primero; deben fallar)

- [ ] T045 [P] [US3] Unit tests en backend/tests/MiniErp.UnitTests/Customers/UpdateCustomerTests.cs: éxito reemplaza todos los datos (un opcional que no se envía queda ausente); id inexistente → `NotFound`; CUIT de **otro** cliente → `Conflict` y sin cambios; dejar el **propio** CUIT no es duplicado; cambiar el CUIT por uno que no usa nadie → éxito (spec, Assumptions); datos inválidos → `Validation` y sin cambios; `DuplicateTaxIdException` → `Conflict`
- [ ] T046 [P] [US3] Integration tests en backend/tests/MiniErp.IntegrationTests/Customers/UpdateCustomerEndpointTests.cs, con datos cargados por el repositorio: `200` con los datos nuevos y `phone: null` si no se envía; `404` para un id inexistente; `200` al cambiar el CUIT por uno libre; `409` al asignar el CUIT de otro cliente; `400` con todos los campos inválidos; después de cada rechazo, un `GET` muestra el cliente sin cambios (FR-011)

### Implementation for User Story 3

- [ ] T047 [US3] Implementar `UpdateCustomer` (carga → `Customer.Update` → `ExistsByTaxIdAsync` excluyendo el propio id → `UpdateAsync`) en backend/src/MiniErp.Application/Customers/UpdateCustomer.cs
- [ ] T048 [US3] Agregar `PUT /{id:guid}` en backend/src/MiniErp.Api/Customers/CustomerEndpoints.cs y registrar `UpdateCustomer` en backend/src/MiniErp.Api/Program.cs
- [ ] T049 [US3] Agregar el request 8 del quickstart a backend/src/MiniErp.Api/MiniErp.Api.http

**Checkpoint**: US1–US3 funcionan por separado.

---

## Phase 6: User Story 4 - Dar de baja un cliente (Priority: P4)

**Goal**: baja física, rechazada si el cliente tiene facturas.

**Independent Test**: con un cliente cargado por el repositorio, `DELETE` devuelve `204` y un `GET`
posterior devuelve `404`; el mismo CUIT se puede volver a registrar (quickstart, pasos 9–11).

### Tests for User Story 4 (escribir primero; deben fallar)

- [ ] T050 [P] [US4] Unit tests en backend/tests/MiniErp.UnitTests/Customers/DeleteCustomerTests.cs: éxito elimina; con `FakeCustomerInvoiceChecker` en `true` → `Conflict` "Customer has invoices" y el cliente no se elimina (FR-009); id inexistente → `NotFound`
- [ ] T051 [P] [US4] Integration tests en backend/tests/MiniErp.IntegrationTests/Customers/DeleteCustomerEndpointTests.cs, con datos cargados por el repositorio: `204` y después `GET` → `404`; id inexistente → `404`; después de la baja, `ExistsByTaxIdAsync` con ese CUIT devuelve `false` y un `SeedAsync` con el mismo CUIT y otro id funciona (prueba que el índice único lo liberó; el alta por API de US4-4 la cubre el paso 11 del quickstart, para no depender de US1)

### Implementation for User Story 4

- [ ] T052 [US4] Implementar `DeleteCustomer` (existe → `HasInvoicesAsync` → `DeleteAsync`) en backend/src/MiniErp.Application/Customers/DeleteCustomer.cs
- [ ] T053 [US4] Agregar `DELETE /{id:guid}` (`204 No Content`) en backend/src/MiniErp.Api/Customers/CustomerEndpoints.cs y registrar `DeleteCustomer` en backend/src/MiniErp.Api/Program.cs
- [ ] T054 [US4] Agregar los requests 9–11 del quickstart a backend/src/MiniErp.Api/MiniErp.Api.http

**Checkpoint**: las 4 historias funcionan por separado.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: rendimiento, documentación y definition of done.

- [ ] T055 Test de rendimiento para SC-005 en backend/tests/MiniErp.IntegrationTests/Customers/CustomerPerformanceTests.cs: cargar 10.000 clientes con un insert masivo (`AddRange` + un único `SaveChangesAsync` sobre el `MiniErpDbContext` de un `CreateScope()`) y verificar que una página del listado y un detalle responden en menos de 1 segundo. Marcarlo como explícito de xUnit v3 (`[Fact(Explicit = true)]`): no corre en el `dotnet test` normal, para no hacer más lenta la suite
- [ ] T056 [P] Actualizar backend/CLAUDE.md: estado del backend, convención de carpetas por módulo (`Customers/`), comandos de `dotnet ef` (herramienta local), que `dotnet test` requiere Docker, y el ADR-0005
- [ ] T057 [P] Actualizar "Estado actual" en CLAUDE.md (raíz) y marcar "Gestión de clientes" como hecha en el roadmap de README.md
- [ ] T058 Correr la definition of done desde backend/: `dotnet build` sin warnings, `dotnet test` en verde, el test explícito de rendimiento de T055 en verde (correrlo habilitando los tests explícitos de xUnit v3; verificar el flag exacto del runner al implementar), `dotnet format --verify-no-changes` sin cambios
- [ ] T059 Validación manual con specs/001-gestion-clientes/quickstart.md, sección 2 (requiere que el tech lead configure la connection string en user-secrets)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias. T001 (ADR) va antes que todo lo demás.
- **Foundational (Phase 2)**: depende de Setup y bloquea todas las historias.
- **User Stories (Phases 3–6)**: dependen solo de Foundational; no dependen entre sí, porque los
  tests cargan sus datos con el repositorio.
- **Polish (Phase 7)**: depende de las historias que se quieran entregar.

### Dentro de Foundational

- T008–T010 (tests) antes que T011–T015 (dominio); T015 después de T011–T014.
- T019 → T020 → T021 → T022 → T024 → T025 (migración, con el modelo completo).
- T026–T028 antes de T029; T029 antes de T030–T032.

### Dentro de cada historia

- Tests primero, y deben fallar → caso de uso → endpoint y registro → requests `.http`.
- Las tareas que tocan `CustomerEndpoints.cs`, `Program.cs` o `MiniErp.Api.http` no son paralelas
  entre historias (mismo archivo).

## Parallel Example: User Story 1

```bash
# Tests de US1 en paralelo (archivos distintos):
Task: "Unit tests de CreateCustomer en backend/tests/MiniErp.UnitTests/Customers/CreateCustomerTests.cs"
Task: "Integration tests de POST en backend/tests/MiniErp.IntegrationTests/Customers/CreateCustomerEndpointTests.cs"
```

## Parallel Example: Foundational

```bash
# Value objects en paralelo, una vez escritos sus tests:
Task: "TaxId en backend/src/MiniErp.Domain/Customers/TaxId.cs"
Task: "LegalName, Email, Phone, Address en backend/src/MiniErp.Domain/Customers/"
Task: "VatCondition en backend/src/MiniErp.Domain/Customers/VatCondition.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (ADR-0005 en su propio commit).
2. Phase 2: Foundational.
3. Phase 3: US1 → **parar y validar** (quickstart, pasos 1–3).

### Incremental Delivery

Setup + Foundational → US1 (MVP) → US2 → US3 → US4 → Polish. Cada historia suma valor sin romper las
anteriores y se valida en su checkpoint.

### Commits sugeridos (un cambio lógico por commit, constitución)

1. `docs: agrega ADR 0005 de tests de integración con Testcontainers` (T001)
2. `chore: agrega EF Core, proveedor MySQL y Testcontainers` (T002–T007)
3. `feat: agrega modelo de clientes, persistencia y composición de la API` (T008–T033)
4. `feat: agrega alta de clientes` (US1)
5. `feat: agrega consulta de clientes` (US2)
6. `feat: agrega modificación de clientes` (US3)
7. `feat: agrega baja de clientes` (US4)
8. `test: agrega test de rendimiento del listado de clientes` (T055)
9. `docs: actualiza documentación con la gestión de clientes` (T056–T057)

Los commits los hace el tech lead o se piden explícitamente (Principio V).

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Verificar que cada test falla antes de implementar.
- Si un test de arquitectura falla, se corrige el código, nunca el test.
- Parar en cada checkpoint para validar la historia por separado.
