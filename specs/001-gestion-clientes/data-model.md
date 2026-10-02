# Data Model: Gestión de clientes

**Feature**: [spec.md](spec.md) | **Decisiones**: [research.md](research.md)

## Glosario (español del negocio → identificadores en inglés del código)

| Negocio | Código |
|---|---|
| Cliente | `Customer` |
| Razón social | `LegalName` |
| CUIT/CUIL | `TaxId` |
| Condición frente al IVA | `VatCondition` |
| Responsable Inscripto | `VatCondition.RegisteredTaxpayer` |
| Monotributista | `VatCondition.SimplifiedRegime` |
| Exento | `VatCondition.Exempt` |
| Consumidor Final | `VatCondition.FinalConsumer` |
| Alta / Baja / Modificación | Create / Delete / Update |

## Entidad de dominio: `Customer` (`MiniErp.Domain`)

| Campo | Tipo | Obligatorio | Reglas (spec) |
|---|---|---|---|
| `Id` | `Guid` (v7) | Sí | Asignado por el dominio al crear; inmutable (FR-002, R3) |
| `LegalName` | `LegalName` | Sí | Trim; no vacío; ≤ 200 caracteres (FR-001, FR-004) |
| `TaxId` | `TaxId` | Sí | Sin guiones ni espacios; 11 dígitos; dígito verificador válido (FR-004, R5); único (FR-003) |
| `VatCondition` | `VatCondition` (enum) | Sí | Uno de los 4 valores del glosario (FR-004) |
| `Email` | `Email?` | No | Trim + minúsculas; formato de email; ≤ 254 caracteres |
| `Phone` | `Phone?` | No | Trim; solo dígitos, espacios, `-`, `(`, `)` y un `+` inicial; ≥ 6 dígitos; ≤ 30 caracteres |
| `Address` | `Address?` | No | Trim; texto libre; ≤ 300 caracteres |

Reglas transversales:
- Un texto vacío o con solo espacios se trata como ausente: es error si el campo es obligatorio y se
  guarda como ausente si es opcional.
- Un valor más largo que el máximo se rechaza; nunca se trunca.
- `Customer.Create(...)` y `Customer.Update(...)` validan todos los campos y devuelven **todos** los
  errores juntos, cada uno asociado a su campo (SC-004).
- `Update` es un reemplazo completo: un opcional que no se envía queda ausente (Clarifications, Q1).
  `Id` no cambia.

### Value objects

Cada uno se construye solo con una fábrica que valida y normaliza. Si existe una instancia, el valor
es válido.

| Value object | Normalización | Error si... |
|---|---|---|
| `LegalName` | Trim | Vacío, o más de 200 caracteres |
| `TaxId` | Quita `-` y espacios | No quedan 11 dígitos, o el dígito verificador no coincide |
| `Email` | Trim + minúsculas | Formato inválido, o más de 254 caracteres |
| `Phone` | Trim | Caracteres no permitidos, `+` fuera del inicio, menos de 6 dígitos, o más de 30 caracteres |
| `Address` | Trim | Más de 300 caracteres |

### Ciclo de vida

```
(no existe) --Create--> Activo --Update--> Activo
                          |
                          +--Delete (sin facturas)--> (no existe; el CUIT queda libre)
                          +--Delete (con facturas)--> rechazado, sigue Activo
```

No hay estados intermedios: la baja es física (FR-008).

## Puertos (`MiniErp.Application`)

| Puerto | Operaciones | Implementación en esta feature |
|---|---|---|
| `ICustomerRepository` | `GetByIdAsync`, `ListAsync(page, pageSize)` → página + total, `ExistsByTaxIdAsync(taxId, excludingId?)`, `AddAsync`, `UpdateAsync`, `DeleteAsync` | `MiniErp.Persistence.EfCore` |
| `ICustomerInvoiceChecker` | `HasInvoicesAsync(customerId)` | `MiniErp.Persistence.EfCore`; devuelve `false` hasta que exista facturación (R8) |

Todas las operaciones son async y reciben `CancellationToken`. `AddAsync` y `UpdateAsync` lanzan
`DuplicateTaxIdException` (definida en `Application`) si la base rechaza el CUIT por duplicado (R7).

## Modelo de persistencia (`MiniErp.Persistence.EfCore`)

`CustomerRecord`, mapeado con Fluent API a la tabla `customers`:

| Columna | Desde | Restricciones |
|---|---|---|
| `id` | `Id` | PK |
| `legal_name` | `LegalName` | NOT NULL, largo 200 |
| `tax_id` | `TaxId` (11 dígitos) | NOT NULL, largo 11, **índice único** |
| `vat_condition` | `VatCondition` (como texto) | NOT NULL, largo 30 |
| `email` | `Email` | NULL, largo 254 |
| `phone` | `Phone` | NULL, largo 30 |
| `address` | `Address` | NULL, largo 300 |

Índices:
- `ux_customers_tax_id` (único): unicidad y búsqueda por CUIT (FR-003, R7).
- `ix_customers_legal_name`: orden del listado (FR-005, SC-005).

Notas:
- `vat_condition` se guarda como texto y no como número, para que reordenar el enum no corrompa
  datos y la base sea legible.
- El adaptador mapea `CustomerRecord` ↔ `Customer`. El dominio no conoce `CustomerRecord` (R4).
- El tipo concreto de columna del `Guid` lo decide el proveedor; no se fija en el modelo para no
  atarlo a MySQL.
