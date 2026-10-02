# Contrato HTTP: clientes

**Feature**: [../spec.md](../spec.md) | **Modelo**: [../data-model.md](../data-model.md)

Grupo de rutas: `/api/customers`. JSON en `camelCase`. Los errores siguen RFC 9457
(`application/problem+json`).

## Tipos

### `CustomerRequest` (cuerpo de `POST` y `PUT`)

```json
{
  "legalName": "Pérez, Juan",
  "taxId": "20-12345678-6",
  "vatCondition": "finalConsumer",
  "email": "juan@example.com",
  "phone": "+54 11 4321-5678",
  "address": "Av. Siempre Viva 742"
}
```

| Campo | Tipo JSON | Obligatorio | Notas |
|---|---|---|---|
| `legalName` | string | Sí | |
| `taxId` | string | Sí | Se aceptan guiones y espacios |
| `vatCondition` | string | Sí | `registeredTaxpayer` \| `simplifiedRegime` \| `exempt` \| `finalConsumer` |
| `email` | string \| null | No | |
| `phone` | string \| null | No | |
| `address` | string \| null | No | |

Todos los campos llegan como string a la API y se validan en el dominio, incluido `vatCondition`. Así,
un valor inválido aparece como un error más en la respuesta 400, junto con los demás (SC-004), en
lugar de cortar la lectura del cuerpo.

### `CustomerResponse`

```json
{
  "id": "0199a4b2-7c3e-7d10-9a1f-3b2c4d5e6f70",
  "legalName": "Pérez, Juan",
  "taxId": "20123456786",
  "vatCondition": "finalConsumer",
  "email": "juan@example.com",
  "phone": "+54 11 4321-5678",
  "address": "Av. Siempre Viva 742"
}
```

`taxId` se devuelve normalizado (11 dígitos). Los opcionales ausentes se devuelven como `null`.

### `CustomerPageResponse`

```json
{
  "items": [ /* CustomerResponse */ ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 57
}
```

## Endpoints

| Operación | Método y ruta | Éxito | Errores |
|---|---|---|---|
| Alta | `POST /api/customers` | `201 Created` + `Location: /api/customers/{id}` + `CustomerResponse` | 400, 409 (CUIT duplicado) |
| Listado | `GET /api/customers?page=1&pageSize=20` | `200 OK` + `CustomerPageResponse` | 400 (paginación inválida) |
| Detalle | `GET /api/customers/{id}` | `200 OK` + `CustomerResponse` | 404 |
| Modificación | `PUT /api/customers/{id}` | `200 OK` + `CustomerResponse` | 400, 404, 409 (CUIT duplicado) |
| Baja | `DELETE /api/customers/{id}` | `204 No Content` | 404, 409 (tiene facturas) |

Paginación (FR-005): `page` ≥ 1 (por defecto 1); `pageSize` entre 1 y 100 (por defecto 20). Una
página fuera de rango devuelve `items` vacío con `200`, no un error. El orden es alfabético por
`legalName`.

`{id}` usa la restricción de ruta `guid`: un id con formato inválido responde `404`, igual que uno
inexistente.

## Errores

### 400: validación (`ValidationProblem`)

Incluye **todos** los campos con problemas en una sola respuesta (SC-004):

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "taxId": ["El CUIT/CUIL no es válido: dígito verificador incorrecto."],
    "email": ["El email no tiene un formato válido."]
  }
}
```

Las claves de `errors` son los nombres de campo del `CustomerRequest` (o `page`/`pageSize` en el
listado). Los textos de los mensajes son orientativos; el contrato garantiza la clave del campo, no
el texto exacto.

### 404: cliente inexistente

`ProblemDetails` con `status: 404`. Se distingue de un error de validación por el código (FR-010).

### 409: conflicto de negocio

`ProblemDetails` con `status: 409`. El `title` distingue el motivo:

| Caso | `title` |
|---|---|
| CUIT/CUIL ya registrado (FR-003) | `Customer already exists` |
| Cliente con facturas (FR-009) | `Customer has invoices` |

En todos los casos de error, ningún dato se modifica (FR-011).
