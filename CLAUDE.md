# mini-erp

ERP didáctico para gestionar clientes, productos y facturas. Monorepo con un backend .NET y, más adelante, un frontend React.

## Estructura

```
backend/          # API .NET (MiniErp.Api) y sus tests; ver backend/CLAUDE.md
docs/adr/         # decisiones de arquitectura (ADR)
frontend/         # React; todavía no existe, se agregará después
.editorconfig     # estilo de código; vive en la raíz y aplica también a backend/
```

## Estado actual

- Backend: scaffolding inicial. `MiniErp.Api` todavía es la plantilla de ASP.NET Core (endpoint `/weatherforecast`) y los tests son placeholders.
- Frontend: no creado.

## Arquitectura

Capas simples en un solo proyecto (`MiniErp.Api`). Decisión y motivos en [docs/adr/0001-arquitectura.md](docs/adr/0001-arquitectura.md).
