# MiniErp.Api

## Resumen
API del ERP didáctico mini-erp (clientes, productos, facturas). No consume otros sistemas; más adelante
la consumirá el frontend React del monorepo.

## Stack
- .NET 10, ASP.NET Core Minimal APIs, OpenAPI nativo (`Microsoft.AspNetCore.OpenApi`).
- Persistencia (decidida en ADR-0003, aún no instalada): MySQL con EF Core 10 y el proveedor `MySql.EntityFrameworkCore` (Oracle).
- Tests: xUnit v3 sobre Microsoft.Testing.Platform (MTP).
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
```
- OpenAPI se expone solo en Development, en `/openapi/v1.json`.
- `src/MiniErp.Api/MiniErp.Api.http` sirve para probar endpoints desde el editor.

## Arquitectura
Hexagonal (puertos y adaptadores) en proyectos separados.
Decisión y motivos en `docs/adr/0002-reemplazabilidad-arquitectura-hexagonal.md` (raíz del repo).

Proyectos (hoy solo existe `MiniErp.Api`; el resto se crea con el primer módulo):
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

Carpetas dentro de cada proyecto: a definir con el primer módulo.

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
- `MiniErp.IntegrationTests` todavía no referencia la API ni usa `WebApplicationFactory`.
- `.editorconfig` está en la raíz del repo, no en `backend/`.

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
