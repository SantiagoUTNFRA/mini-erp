# mini-erp

ERP didáctico para gestionar clientes, productos y facturas, construido con .NET 10 y arquitectura
hexagonal. Lo desarrollo con un agente de IA (Claude Code) usando Spec-Driven Development con
[GitHub Spec Kit](https://github.com/github/spec-kit): yo defino y reviso como tech lead, el agente
ejecuta.

> **Estado:** en construcción. La arquitectura, el entorno local y la constitución del proyecto están
> listos; la primera feature (gestión de clientes) está especificada y en planificación.

## El experimento: reemplazabilidad

El objetivo de diseño no es solo el ERP. Cada tecnología (base de datos, ORM, frontend) tiene que
poder reemplazarse con el menor costo posible, y esas migraciones son un experimento para medir qué
tan bien las ejecuta un agente de IA.

La métrica está en el diff: una migración bien hecha escribe un adaptador nuevo y cambia su registro,
sin tocar el dominio ni los casos de uso.

## Arquitectura

Hexagonal (puertos y adaptadores) en proyectos separados, para que las fronteras las haga cumplir el
compilador y no la disciplina.

```
backend/src/
  MiniErp.Domain                # entidades y reglas de negocio; no depende de nada
  MiniErp.Application           # casos de uso y puertos (interfaces); → Domain
  MiniErp.Persistence.EfCore    # adaptador de persistencia; → Application, Domain
  MiniErp.Api                   # adaptador HTTP y composition root
backend/tests/
  MiniErp.UnitTests             # incluye tests de arquitectura sobre las referencias compiladas
  MiniErp.IntegrationTests
```

Los tests de arquitectura fallan si `Domain` o `Application` dependen de un adaptador.

## Stack

- .NET 10, ASP.NET Core Minimal APIs, OpenAPI.
- MySQL 9.7 (LTS) con EF Core 10, confinados al adaptador de persistencia.
- xUnit v3 sobre Microsoft.Testing.Platform.
- Docker Compose para el entorno local.
- Frontend React: planificado.

## Cómo se decide y cómo se trabaja

- **Decisiones de arquitectura** registradas como ADR en [`docs/adr/`](docs/adr/). Los ADR aceptados
  no se editan; si una decisión cambia, se escribe uno nuevo que la reemplaza.
- **Constitución del proyecto** en [`.specify/memory/constitution.md`](.specify/memory/constitution.md):
  los principios no negociables que cada plan verifica.
- **Features con Spec-Driven Development**: specify → clarify → plan → tasks → implement. Las
  especificaciones viven en `specs/`; cada feature se desarrolla en su rama y entra a
  `main` por Pull Request.
- **Instrucciones para el agente** en [`CLAUDE.md`](CLAUDE.md) y [`backend/CLAUDE.md`](backend/CLAUDE.md).

## Correr el proyecto

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) y Docker Desktop.

```bash
# Base de datos local (desde la raíz del repo)
cp .env.example .env              # solo la primera vez; reemplazar las contraseñas
docker compose up -d --wait       # levanta MySQL y espera a que esté listo

# Backend (desde backend/)
cd backend
dotnet build
dotnet test
dotnet run --project src/MiniErp.Api      # http://localhost:5014
```

En Development, el documento OpenAPI se expone en `/openapi/v1.json`.

## Roadmap

- [x] Arquitectura hexagonal con tests de arquitectura
- [x] Entorno local con Docker Compose
- [x] Constitución del proyecto y adopción de Spec Kit
- [ ] Gestión de clientes (especificada, en planificación)
- [ ] Gestión de productos
- [ ] Facturación
- [ ] Frontend React
- [ ] Primeras migraciones de tecnología (proveedor de base de datos, ORM)
