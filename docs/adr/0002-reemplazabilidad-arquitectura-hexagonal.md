# ADR 0002: Reemplazabilidad de tecnologías y arquitectura hexagonal

- Estado: Aceptada
- Fecha: 2026-10-02
- Reemplaza a: ADR-0001

## Contexto
Además de ser un ERP didáctico mayormente CRUD (ver ADR-0001), mini-erp tiene un objetivo nuevo:
poder reemplazar cada tecnología (base de datos, ORM, frontend) con el menor costo posible, y usar
esas migraciones como experimento para medir qué tan bien las ejecuta un agente de IA.

ADR-0001 eligió capas simples en un solo proyecto y aceptó que la separación entre capas dependiera
de la disciplina, no del compilador. Con la reemplazabilidad como objetivo, ese costo pasa a ser el
problema principal: si cualquier parte del código puede depender de una tecnología concreta, cambiarla
implica tocar todo.

## Opciones consideradas
1. Mantener ADR-0001 (un proyecto, EF Core usado directamente) — pros: lo más simple / contras: cambiar
   de proveedor de base de datos es barato gracias a EF Core, pero cambiar de ORM obliga a tocar todo.
2. Un proyecto con interfaces en las fronteras (carpetas) — pros: poca ceremonia / contras: nada impide
   saltarse una interfaz; las fronteras se erosionan sin que nadie lo note.
3. Arquitectura hexagonal (puertos y adaptadores) en proyectos separados — pros: el compilador impide
   que el núcleo dependa de una tecnología; reemplazar una tecnología es escribir un adaptador nuevo
   / contras: más proyectos y más indirección que la que un CRUD necesita por sí solo.

## Decisión
Arquitectura hexagonal en proyectos separados. Es la única opción en la que las fronteras las hace
cumplir el compilador, que es lo que vuelve barato y verificable reemplazar una tecnología.

Proyectos y regla de dependencias (las flechas son referencias entre proyectos):
```
MiniErp.Domain                  # entidades y reglas de negocio; no depende de nada
MiniErp.Application             # casos de uso y puertos (interfaces); → Domain
MiniErp.Persistence.EfCore      # adaptador de persistencia; → Application, Domain
MiniErp.Api                     # adaptador HTTP y composition root; → Application, Domain
                                #   y a los adaptadores solo para registrarlos en DI
```
- Los puertos se definen por caso de uso en `Application` (p. ej. `ICustomerRepository`), no como un
  repositorio genérico sobre `DbContext`.
- Ningún tipo de un adaptador (`DbContext`, entidades de persistencia, tipos de EF Core) aparece en
  `Domain` ni en `Application`.

## Consecuencias
- Positivas: reemplazar una tecnología se reduce a escribir otro adaptador y cambiar su registro en
  `MiniErp.Api`; el resultado del experimento se mide en el diff (¿se tocó `Domain` o `Application`?).
- Negativas / lo que aceptamos pagar: más proyectos, mapeos entre capas y más código por cada recurso
  CRUD que con ADR-0001.
- Qué nos haría revisar esta decisión: si se abandona el objetivo de reemplazabilidad, o si la
  ceremonia frena el aprendizaje más de lo que aporta.
