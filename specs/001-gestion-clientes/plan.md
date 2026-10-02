# Implementation Plan: Gestión de clientes

**Branch**: `001-gestion-clientes` | **Date**: 2026-10-02 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-gestion-clientes/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Alta, consulta (listado paginado y detalle), modificación y baja física de clientes, expuestas como
API HTTP. Es la primera feature del backend: además de los clientes, recorre por primera vez todas
las capas de la arquitectura hexagonal (dominio con value objects y validación del CUIT/CUIL, casos
de uso con puertos, adaptador EF Core + MySQL con migraciones, endpoints Minimal API) y establece la
estrategia de tests de integración contra una base real (Testcontainers, ADR-0005).

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, `Nullable`, `TreatWarningsAsErrors`)

**Primary Dependencies**: ASP.NET Core Minimal APIs + OpenAPI (existentes); EF Core 10 con
`MySql.EntityFrameworkCore` 10.0.9 y `Microsoft.EntityFrameworkCore.Design` 10.0.12 (nuevos,
[R1](research.md))

**Storage**: MySQL 9.7 (LTS) vía Docker Compose en desarrollo; tabla `customers`
([data-model.md](data-model.md))

**Testing**: xUnit v3 sobre Microsoft.Testing.Platform; unit tests para dominio y casos de uso;
integration tests de endpoints con `WebApplicationFactory` + `Testcontainers.MySql` 4.15.0 (nuevo,
[R2](research.md))

**Target Platform**: servicio web (Kestrel); desarrollo en macOS con Docker Desktop

**Project Type**: web-service (backend de un monorepo; el frontend todavía no existe)

**Performance Goals**: listado y detalle en menos de 1 s con 10.000 clientes (SC-005), con índices
sobre CUIT y razón social

**Constraints**: ningún tipo de EF Core o MySQL en `Domain`/`Application` (verificado por
`DependencyRuleTests`); errores como `ProblemDetails`; build sin warnings

**Scale/Scope**: 1 entidad, 5 endpoints, 2 puertos; volumen estimado de 10.000 clientes (provisional,
ver Assumptions de la spec)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Verificación inicial (antes de Fase 0) | Re-verificación (después de Fase 1) |
|---|---|---|
| I. Reemplazabilidad tecnológica | ✅ La feature cabe en la estructura hexagonal existente; EF Core y MySQL quedan en `MiniErp.Persistence.EfCore` | ✅ Dominio sin atributos de EF (R4); modelo de persistencia propio (`CustomerRecord`); id generado en el dominio, no por la base (R3); error de clave duplicada traducido en el adaptador (R7); registro de DI solo en `MiniErp.Api` (R10) |
| II. Contratos de API explícitos y estables | ✅ Contrato nuevo, ninguno existente se rompe | ✅ `record` de request/response propios; `TypedResults`; errores 400/404/409 con `ProblemDetails`; contrato documentado en [contracts/](contracts/customers-api.md) |
| III. Comportamiento respaldado por tests | ✅ Cada historia de usuario tiene escenarios de aceptación testeables | ✅ Unit tests: value objects (CUIT incluido) y casos de uso con puertos fake (incluido FR-009); integration tests por endpoint contra MySQL real (R2); el smoke test de OpenAPI sigue en verde |
| IV. Decisiones registradas en ADR | ⚠️ Se detecta una decisión de tecnología nueva: estrategia de tests de integración | ✅ Se registra en **ADR-0005** antes de implementar (R2); el resto son decisiones de diseño dentro de ADR-0002/0003, documentadas en [research.md](research.md) |
| V. Supervisión humana del agente | ⚠️ La feature necesita paquetes NuGet nuevos | ✅ Ningún paquete se agrega sin OK: R1 y R2 **aprobados por el tech lead el 2026-10-02**; ningún contrato existente cambia; sin acciones destructivas |

**Resultado del gate**: PASA. No hay violaciones que justificar. Los dos ⚠️ iniciales se resuelven
con el procedimiento que la propia constitución prevé (ADR y aprobación), no con excepciones.

**Aprobaciones del tech lead** (otorgadas el 2026-10-02):
1. Paquetes de R1: `MySql.EntityFrameworkCore` 10.0.9, `Microsoft.EntityFrameworkCore.Design`
   10.0.12, `Testcontainers.MySql` 4.15.0, y la herramienta local `dotnet-ef` 10.0.12.
2. Estrategia de R2 (Testcontainers) y su ADR-0005.

## Project Structure

### Documentation (this feature)

```text
specs/001-gestion-clientes/
├── spec.md
├── plan.md                    # Este archivo
├── research.md                # Fase 0: decisiones técnicas
├── data-model.md              # Fase 1: entidad, value objects, puertos, tabla
├── quickstart.md              # Fase 1: guía de validación
├── contracts/
│   └── customers-api.md       # Fase 1: contrato HTTP
├── checklists/
│   └── requirements.md
└── tasks.md                   # Fase 2 (/speckit-tasks; no lo crea este comando)
```

### Source Code (repository root)

Primer módulo del backend: define la convención de carpetas que ADR-0002 dejó abierta. Se organiza
**por módulo de negocio dentro de cada proyecto** (`Customers/`), para que los módulos siguientes
(productos, facturas) repitan el patrón.

```text
backend/
├── dotnet-tools.json                      # nuevo: dotnet-ef como herramienta local
├── Directory.Packages.props               # + 3 paquetes (R1)
├── src/
│   ├── MiniErp.Domain/
│   │   ├── Common/                        # tipo para errores de validación por campo
│   │   └── Customers/                     # Customer, VatCondition, TaxId, LegalName, Email, Phone, Address
│   ├── MiniErp.Application/
│   │   ├── Common/                        # resultado de casos de uso (Validation/NotFound/Conflict)
│   │   └── Customers/                     # casos de uso, ICustomerRepository, ICustomerInvoiceChecker,
│   │                                      #   DuplicateTaxIdException
│   ├── MiniErp.Persistence.EfCore/
│   │   ├── MiniErpDbContext.cs
│   │   ├── DependencyInjection.cs         # AddEfCorePersistence + migración en Development
│   │   ├── Customers/                     # CustomerRecord, configuración Fluent, repositorio, invoice checker
│   │   └── Migrations/
│   └── MiniErp.Api/
│       ├── Program.cs                     # solo compone: DI, middleware, MapCustomers
│       ├── Customers/                     # endpoints, CustomerRequest/Response, mapeo de resultados
│       └── MiniErp.Api.http               # requests del quickstart
└── tests/
    ├── MiniErp.UnitTests/
    │   ├── Architecture/                  # existente
    │   └── Customers/                     # value objects, Customer, casos de uso (con fakes)
    └── MiniErp.IntegrationTests/
        ├── Infrastructure/                # factory + fixture del contenedor MySQL
        ├── OpenApiTests.cs                # existente; pasa a usar la fixture (la API ahora necesita base)
        └── Customers/                     # un archivo de tests por endpoint
docs/adr/
└── 0005-tests-integracion-testcontainers.md   # nuevo (R2)
```

**Structure Decision**: se mantiene la estructura hexagonal de ADR-0002 sin proyectos nuevos y se
introduce la convención de carpetas por módulo de negocio. El `OpenApiTests` existente se adapta a
la fixture de base de datos, porque la API pasa a aplicar migraciones al arrancar en Development.

## Complexity Tracking

Sin violaciones de la constitución que justificar. La complejidad adicional (modelo de persistencia
separado y mapeos) ya está aceptada en ADR-0002 como costo de la reemplazabilidad.
