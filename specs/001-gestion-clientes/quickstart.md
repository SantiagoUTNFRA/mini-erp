# Quickstart: validar la gestión de clientes

**Feature**: [spec.md](spec.md) | **Contrato**: [contracts/customers-api.md](contracts/customers-api.md)

Guía para comprobar de punta a punta que la feature cumple la spec. No contiene implementación.

## Prerrequisitos

- .NET 10 SDK y Docker Desktop corriendo.
- `.env` creado a partir de `.env.example` (ver `CLAUDE.md` de la raíz).
- Connection string de la API en user-secrets (una sola vez, desde `backend/`; los valores salen
  de tu `.env`):
  ```bash
  dotnet user-secrets set "ConnectionStrings:MiniErp" \
    "Server=127.0.0.1;Port=3306;Database=minierp;User=minierp;Password=<MYSQL_PASSWORD de .env>" \
    --project src/MiniErp.Api
  ```

## 1. Validación automática

Desde `backend/`:

```bash
dotnet build                          # sin warnings
dotnet test                           # requiere Docker: los tests de integración levantan su propio MySQL
dotnet format --verify-no-changes
```

Resultado esperado: todo en verde. En particular deben pasar:
- Los tests de arquitectura (`DependencyRuleTests`): ningún tipo de EF Core ni de MySQL en `Domain`
  o `Application`.
- Los unit tests del CUIT/CUIL (válidos, dígito verificador incorrecto, con y sin guiones).
- Los tests de integración de cada endpoint del contrato.

## 2. Validación manual contra el entorno local

```bash
# Desde la raíz del repo
docker compose up -d --wait

# Desde backend/: la API aplica las migraciones al arrancar en Development
dotnet run --project src/MiniErp.Api      # http://localhost:5014
```

Ejecutar los requests de `src/MiniErp.Api/MiniErp.Api.http` desde el editor, en este orden:

| # | Request | Resultado esperado | Spec |
|---|---|---|---|
| 1 | `POST /api/customers` con datos válidos (CUIT `20-12345678-6`) | `201`, `Location` con el id, `taxId` = `20123456786` | US1-1 |
| 2 | El mismo `POST` con `taxId` = `20123456786` | `409` "Customer already exists" | US1-2, FR-003 |
| 3 | `POST` con CUIT `20-12345678-5`, email `foo` y sin `legalName` | `400` con errores en `taxId`, `email` y `legalName` juntos | US1-3, US1-4, SC-004 |
| 4 | `GET /api/customers` | `200`, el cliente del paso 1, `totalCount` = 1 | US2-1 |
| 5 | `GET /api/customers?pageSize=101` | `400` con error en `pageSize` | FR-005 |
| 6 | `GET /api/customers/{id del paso 1}` | `200` con todos los datos | US2-2 |
| 7 | `GET /api/customers/{guid inexistente}` | `404` | US2-3 |
| 8 | `PUT /api/customers/{id}` con todos los datos menos `phone` | `200`, `phone` = `null` | US3-1, US3-5 |
| 9 | `DELETE /api/customers/{id}` | `204` | US4-1 |
| 10 | `GET /api/customers/{id}` | `404` | US4-1 |
| 11 | Repetir el paso 1 | `201` con un id nuevo: el CUIT quedó libre | US4-4 |

El rechazo de la baja por facturas (US4-2) no se puede probar a mano todavía, porque la facturación
no existe; lo cubre un unit test del caso de uso (ver [research.md](research.md), R8).

## 3. Limpieza

```bash
docker compose down        # conserva los datos
docker compose down -v     # borra los datos (pedir OK)
```
