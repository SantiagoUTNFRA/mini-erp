# MiniErp.Api

## Resumen
API del ERP didáctico mini-erp (clientes, productos, facturas). No consume otros sistemas; más adelante
la consumirá el frontend React del monorepo.

## Stack
- .NET 10, ASP.NET Core Minimal APIs, OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`).
- Persistencia (ADR-0003): MySQL con EF Core 10.0.9 y el proveedor `MySql.EntityFrameworkCore` 10.0.9
  (Oracle), solo en `MiniErp.Persistence.EfCore`. MySQL local corre con Docker Compose desde la raíz
  del repo (ver `CLAUDE.md` raíz); el connection string (`ConnectionStrings:MiniErp`) va en
  `dotnet user-secrets` del proyecto `MiniErp.Api`, nunca en `appsettings*.json`. En Development la
  API aplica las migraciones pendientes al arrancar.
- Tests: xUnit v3 sobre Microsoft.Testing.Platform (MTP). Integración contra MySQL real con
  Testcontainers (ADR-0005): **`dotnet test` requiere Docker corriendo**.
- Versiones de paquetes centralizadas en `Directory.Packages.props`.

## Comandos
Desde `backend/` (ahí están `MiniErp.slnx` y `global.json`).
```bash
dotnet restore
dotnet build                                              # debe quedar sin warnings
dotnet test                                               # toda la suite
dotnet test --project tests/MiniErp.UnitTests             # un proyecto de tests
dotnet test --project tests/MiniErp.UnitTests --filter-method "*<NombreTest>"   # un test (preferido)
dotnet test --project tests/MiniErp.UnitTests --filter "FullyQualifiedName~<NombreTest>"  # alternativa
dotnet run --project src/MiniErp.Api                      # http://localhost:5014
dotnet run --project src/MiniErp.Api --launch-profile https   # https://localhost:7065
dotnet format --verify-no-changes                         # chequeo de formato (sin flag: corrige)
dotnet test --project tests/MiniErp.IntegrationTests --explicit only   # solo tests explícitos (rendimiento)

# Migraciones (dotnet-ef es herramienta local: dotnet-tools.json; `dotnet tool restore` la instala)
dotnet ef migrations add <Nombre> --project src/MiniErp.Persistence.EfCore --startup-project src/MiniErp.Persistence.EfCore
```
- OpenAPI se expone solo en Development, en `/openapi/v1.json`.
- `src/MiniErp.Api/MiniErp.Api.http` sirve para probar endpoints desde el editor (extensión REST
  Client). Al agregar un endpoint, sumar sus requests ahí.

## Arquitectura
Hexagonal (puertos y adaptadores) en proyectos separados.
Decisión y motivos en `docs/adr/0002-reemplazabilidad-arquitectura-hexagonal.md` (raíz del repo).

Proyectos:
```
src/MiniErp.Domain              # entidades y reglas de negocio; no depende de nada
src/MiniErp.Application         # casos de uso y puertos (interfaces); → Domain
src/MiniErp.Persistence.EfCore  # adaptador de persistencia; → Application, Domain
src/MiniErp.Api                 # adaptador HTTP y composition root; → Application, Domain
                                #   y a los adaptadores solo para registrarlos en DI
```
- Ningún tipo de un adaptador (`DbContext`, EF Core, entidades de persistencia) aparece en `Domain` ni en `Application`.
- Puertos definidos por caso de uso (p. ej. `ICustomerRepository`), no un repositorio genérico sobre `DbContext`.
- Reemplazar una tecnología = escribir otro adaptador y cambiar su registro en `MiniErp.Api`, sin tocar `Domain` ni `Application`.
- `tests/MiniErp.UnitTests/Architecture/DependencyRuleTests.cs` hace cumplir esta regla sobre las referencias compiladas. Si falla, se corrige el código, no el test. Límite conocido: las constantes se copian en tiempo de compilación y no dejan referencia.

Carpetas por módulo de negocio dentro de cada proyecto (`Customers/`), más `Common/` para lo
compartido. Patrón de referencia: el módulo de clientes (`specs/001-gestion-clientes/`).
- `Domain`: entidad + value objects con fábricas que devuelven `Validated<T>` (errores por campo, sin
  excepciones). Las reglas de negocio viven acá.
- `Application`: un caso de uso por clase (`CreateCustomer`, `ListCustomers`...) que devuelve
  `Result`/`Result<T>` con errores `Validation`/`NotFound`/`Conflict`; puertos por módulo.
- `Persistence.EfCore`: modelo de persistencia propio (`CustomerRecord`) mapeado al dominio; los
  errores del proveedor se traducen a excepciones de `Application` (p. ej. clave duplicada).
- `Api`: endpoints que traducen `Result` a `TypedResults` (`ResultMapping`).

Fijo en cualquier arquitectura:
```
MiniErp.slnx
global.json                    # runner de tests: Microsoft.Testing.Platform
Directory.Build.props          # net10.0, Nullable, TreatWarningsAsErrors, ImplicitUsings
Directory.Packages.props       # versiones NuGet centralizadas
src/                           # código de producción
tests/                         # un proyecto de tests por tipo (unit, integration)
```

## Trampas de configuración
- `TreatWarningsAsErrors=true` (en `Directory.Build.props`): cualquier warning rompe el build. Las
  propiedades comunes no se repiten en los `.csproj`.
- Central Package Management: la versión va solo en `Directory.Packages.props`; en los `.csproj` el
  `<PackageReference>` va sin `Version`.
- La solución usa formato `.slnx`: `dotnet sln MiniErp.slnx add <ruta.csproj>`.
- Tests sobre MTP, no VSTest: los proyectos de test son `OutputType=Exe` y `using Xunit` es global
  (`<Using Include="Xunit" />`).
- Integration tests con `WebApplicationFactory<Program>` (corre en Development). No hace falta
  `public partial class Program`: en .NET 10 `Program` ya es público.
- xUnit v3: pasar `TestContext.Current.CancellationToken` a las llamadas async en tests; si no, el
  analizador emite un warning (`xUnit1051`) y `TreatWarningsAsErrors` rompe el build.
- `.editorconfig` está en la raíz del repo, no en `backend/`. `dotnet build` no aplica el estilo:
  solo `dotnet format` lo detecta (p. ej. grupos de `using` separados por línea en blanco). Las
  migraciones (`**/Migrations/*.cs`) están marcadas como código generado y no se formatean.
- Versiones de EF Core: `Microsoft.EntityFrameworkCore.Design` es `PrivateAssets="all"`, así que no
  fija la versión fuera de `Persistence`. Tiene que coincidir con la que trae el proveedor MySQL; si
  no, el build da `MSB3277` (conflicto de versiones).
- Tests de integración: la connection string del contenedor se inyecta con `UseSetting` en la
  factory. `ConfigureAppConfiguration` no sirve: con minimal hosting se aplica después de que
  `Program.cs` la lee. Los datos se cargan con `SeedAsync` (por el puerto, sin pasar por HTTP).
- Tests explícitos (`[Fact(Explicit = true)]`) no corren en un `dotnet test` normal: aparecen como
  `skipped`. Se corren con `--explicit only`.

## Convenciones del proyecto
- Program.cs solo compone (DI, middleware, mapeo de endpoints); la lógica va fuera.
- Endpoints agrupados con `MapGroup("/api/<recurso>")`.
- Devolver `TypedResults` (`Ok`, `NotFound`, `ValidationProblem`...), no `Results`.
- Errores con `ProblemDetails` (RFC 9457); no devolver excepciones ni strings sueltos.
- Requests y responses como `record`; nunca exponer entidades de persistencia en la API.

## Testing
- Unit tests para lógica de dominio; integration tests para endpoints.
- Nombre: `Metodo_Escenario_ResultadoEsperado`.
- Estructura Arrange / Act / Assert.
- Todo cambio de comportamiento viene con su test. Un bug se arregla escribiendo primero el test que lo reproduce.

## No tocar sin preguntar
- `appsettings.Production.json` y cualquier secreto.
- `.github/workflows/` (CI/CD).
- Contratos públicos de la API (rutas, forma de requests/responses): un cambio incompatible requiere mi OK.
- `Directory.Packages.props`: no agregar paquetes sin OK.

## Definition of done
1. `dotnet build` sin warnings.
2. `dotnet test` en verde.
3. `dotnet format --verify-no-changes` sin cambios.
4. Tests nuevos para el comportamiento nuevo.
5. Resumen de qué cambió y por qué, listo para revisión (sin commitear).
