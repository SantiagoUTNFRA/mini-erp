# ADR 0003: Persistencia con MySQL y EF Core

- Estado: Aceptada
- Fecha: 2026-10-02

## Contexto
mini-erp necesita persistir clientes, productos y facturas. Se eligió MySQL como base de datos y
EF Core como ORM, implementados como adaptador de persistencia según ADR-0002
(`MiniErp.Persistence.EfCore`). El backend apunta a .NET 10, así que el proveedor de MySQL debe
soportar EF Core 10.

Estado de los proveedores en NuGet al 2026-10-02:
- `Pomelo.EntityFrameworkCore.MySql` (comunitario): última versión 9.0.0, sin versión para EF Core 10.
- `MySql.EntityFrameworkCore` (Oracle): 10.0.9, depende de EF Core 10.0.9. Licencia
  GPL-2.0-only con Universal FOSS Exception 1.0.

En la máquina de desarrollo no hay MySQL instalado; sí Docker Desktop.

## Opciones consideradas
1. `MySql.EntityFrameworkCore` (Oracle) con EF Core 10 — pros: soporta la versión actual de EF Core;
   proveedor oficial del fabricante / contras: históricamente menos usado y con menos documentación
   comunitaria que Pomelo; licencia GPL con excepción FOSS.
2. `Pomelo.EntityFrameworkCore.MySql` 9.0.0 con EF Core 9 — pros: el proveedor más usado por la
   comunidad / contras: obliga a quedarse en EF Core 9 sobre .NET 10 hasta que salga su versión 10.

## Decisión
`MySql.EntityFrameworkCore` (Oracle) con EF Core 10: permite usar la versión actual de EF Core sin
quedar atados al calendario de Pomelo. Si más adelante Pomelo publica soporte para EF Core 10,
cambiar de proveedor será uno de los primeros experimentos de reemplazabilidad (ADR-0002).

MySQL corre localmente en un contenedor Docker; cómo se levanta se define en un ADR posterior.

## Consecuencias
- Positivas: EF Core abstrae el proveedor; cambiar de MySQL a otra base relacional afecta solo al
  adaptador de persistencia y a su registro en `MiniErp.Api`.
- Negativas / lo que aceptamos pagar: las migraciones de EF Core son específicas del proveedor; al
  cambiar de base de datos se regeneran. Hay que evitar SQL crudo y tipos o funciones propios de MySQL
  fuera del adaptador.
- Qué nos haría revisar esta decisión: problemas de compatibilidad o bugs del proveedor de Oracle, o
  el objetivo de experimentar con otra base de datos.
