# mini-erp

ERP didáctico para gestionar clientes, productos y facturas. Monorepo con un backend .NET y, más adelante, un frontend React.

## Objetivo de diseño: reemplazabilidad

Cada tecnología (base de datos, ORM, frontend) tiene que poder reemplazarse con el menor costo posible; esas migraciones son un experimento para medir qué tan bien las ejecuta un agente de IA. Al diseñar o implementar, ninguna tecnología concreta se filtra fuera de su adaptador (ver ADR-0002).

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

Hexagonal (puertos y adaptadores) en proyectos separados: [ADR-0002](docs/adr/0002-reemplazabilidad-arquitectura-hexagonal.md), que reemplaza al ADR-0001. Persistencia con MySQL y EF Core: [ADR-0003](docs/adr/0003-persistencia-mysql-ef-core.md). Los ADR aceptados no se editan; si una decisión cambia, se escribe uno nuevo que lo reemplaza.
