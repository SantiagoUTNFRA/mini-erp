# mini-erp

ERP didáctico para gestionar clientes, productos y facturas. Monorepo con un backend .NET y, más adelante, un frontend React.

## Objetivo de diseño: reemplazabilidad

Cada tecnología (base de datos, ORM, frontend) tiene que poder reemplazarse con el menor costo posible; esas migraciones son un experimento para medir qué tan bien las ejecuta un agente de IA. Al diseñar o implementar, ninguna tecnología concreta se filtra fuera de su adaptador (ver ADR-0002).

## Estructura

```
backend/          # API .NET (MiniErp.Api) y sus tests; ver backend/CLAUDE.md
docs/adr/         # decisiones de arquitectura (ADR)
specs/            # specs de features (Spec Kit): spec, plan, tareas; una carpeta por feature
frontend/         # React; todavía no existe, se agregará después
compose.yaml      # entorno local (MySQL); ver "Entorno local"
.env.example      # variables del entorno local; copiar a .env (no versionado)
.editorconfig     # estilo de código; vive en la raíz y aplica también a backend/
```

## Entorno local

Docker Compose (ADR-0004). Requiere Docker Desktop corriendo. Desde la raíz del repo:

```bash
cp .env.example .env              # solo la primera vez; reemplazar las contraseñas
docker compose up -d --wait       # levanta MySQL y espera a que esté healthy
docker compose ps                 # estado
docker compose logs -f mysql      # logs
docker compose down               # baja el entorno; los datos quedan en el volumen
docker compose down -v            # baja y BORRA los datos (pedir OK antes)
```

- MySQL 9.7 (LTS) en `127.0.0.1:3306`, base `minierp`, usuario de aplicación `minierp` (no root).
- `docker compose config` sin `--quiet` imprime las contraseñas de `.env` ya interpoladas.

## Estado actual

- Backend: gestión de clientes completa (alta, consulta, modificación y baja en `/api/customers`;
  `specs/001-gestion-clientes/`). EF Core 10 + MySQL con migraciones; tests unitarios, de arquitectura
  y de integración contra MySQL real (Testcontainers) en verde.
- Próximas features: productos y facturación.
- Frontend: no creado.

## Arquitectura

Hexagonal (puertos y adaptadores) en proyectos separados: [ADR-0002](docs/adr/0002-reemplazabilidad-arquitectura-hexagonal.md), que reemplaza al ADR-0001. Persistencia con MySQL y EF Core: [ADR-0003](docs/adr/0003-persistencia-mysql-ef-core.md). Entorno local con Docker Compose: [ADR-0004](docs/adr/0004-entorno-local-docker-compose.md). Tests de integración con Testcontainers: [ADR-0005](docs/adr/0005-tests-integracion-testcontainers.md). Los ADR aceptados no se editan; si una decisión cambia, se escribe uno nuevo que lo reemplaza.
