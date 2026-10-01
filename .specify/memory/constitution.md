# mini-erp Constitution

## Core Principles

### I. Reemplazabilidad tecnológica (NO NEGOCIABLE)

Cada tecnología concreta (base de datos, ORM, frontend, proveedores) MUST poder reemplazarse
escribiendo un adaptador nuevo y cambiando su registro, sin tocar el núcleo.

- El backend sigue arquitectura hexagonal en proyectos separados (ADR-0002): `Domain` no depende
  de nada; `Application` depende solo de `Domain`; los adaptadores dependen del núcleo, nunca al
  revés; `MiniErp.Api` referencia adaptadores solo para registrarlos en DI.
- Ningún tipo de un adaptador (`DbContext`, tipos de EF Core, entidades de persistencia, SQL crudo
  o funciones propias de MySQL) MUST aparecer en `Domain` ni en `Application`.
- Los puertos se definen por caso de uso en `Application` (p. ej. `ICustomerRepository`), no como
  un repositorio genérico sobre la tecnología.
- Los tests de arquitectura (`DependencyRuleTests`) son la verificación de este principio: si
  fallan, se corrige el código, nunca el test.

Rationale: el proyecto existe en parte para medir qué tan bien un agente de IA migra tecnologías;
esa medición solo es válida si las fronteras las hace cumplir el compilador y el diff de una
migración no toca el núcleo.

### II. Contratos de API explícitos y estables

La API HTTP es el contrato con sus consumidores (el futuro frontend React) y MUST ser explícita.

- Requests y responses MUST ser `record` propios de la API; las entidades de dominio o de
  persistencia nunca se exponen.
- Los endpoints devuelven `TypedResults` y los errores se expresan con `ProblemDetails`
  (RFC 9457); nunca excepciones ni strings sueltos.
- Un cambio incompatible en un contrato público (rutas, forma de requests/responses) MUST tener
  aprobación explícita del tech lead antes de implementarse.

Rationale: un contrato estable permite reemplazar el frontend o el backend por separado, que es
la otra cara de la reemplazabilidad.

### III. Comportamiento respaldado por tests

- Todo cambio de comportamiento MUST venir acompañado de su test: unit tests para la lógica de
  dominio, integration tests para los endpoints.
- Un bug se arregla escribiendo primero el test que lo reproduce.
- Los tests siguen el nombre `Metodo_Escenario_ResultadoEsperado` y la estructura
  Arrange / Act / Assert.
- No se declara terminado un cambio con tests en rojo, salteados o desactivados.

Rationale: los tests son la red que hace verificable el trabajo del agente y seguro un
reemplazo de tecnología; sin ellos no hay forma objetiva de saber si una migración preservó el
comportamiento.

### IV. Decisiones registradas en ADR

- Toda decisión de arquitectura o de tecnología (stack, librerías, estructura, entorno) MUST
  quedar registrada en un ADR en `docs/adr/`, con contexto, opciones consideradas, decisión y
  consecuencias.
- Los ADR aceptados no se editan: si una decisión cambia, se escribe un ADR nuevo que reemplaza
  al anterior y este queda marcado como reemplazado.
- Los datos externos que justifican una decisión (versiones, licencias, compatibilidad) se citan
  con la fecha en que se verificaron; no se inventan.

Rationale: el experimento de reemplazabilidad y el aprendizaje requieren poder reconstruir por
qué se eligió cada cosa y qué alternativas se descartaron.

### V. Supervisión humana del agente

El tech lead define y revisa; el agente ejecuta. Las siguientes acciones MUST tener aprobación
explícita del tech lead antes de ejecutarse:

- Agregar un paquete NuGet (o dependencia equivalente en otro stack).
- Hacer commit o push. Nunca `push --force` ni reescribir historia publicada.
- Tocar secretos, configuración de producción o CI/CD (`.github/workflows/`).
- Romper un contrato público (ver Principio II).
- Ejecutar acciones destructivas (p. ej. borrar volúmenes de datos).
- Cambios grandes o ambiguos: primero un plan, después la implementación.

Rationale: el valor del experimento depende de que cada cambio del agente sea revisable y
atribuible a una decisión humana explícita.

## Restricciones técnicas

- Backend: .NET 10, ASP.NET Core Minimal APIs, `Nullable` habilitado y
  `TreatWarningsAsErrors=true`; un warning rompe el build.
- Versiones de paquetes centralizadas en `Directory.Packages.props`.
- Persistencia: MySQL con EF Core 10 y el proveedor `MySql.EntityFrameworkCore` (ADR-0003),
  confinados al adaptador `MiniErp.Persistence.EfCore`.
- Entorno local: Docker Compose en la raíz del repo (ADR-0004); imágenes fijadas a una versión
  explícita, no a `latest`.
- Secretos: nunca versionados. En desarrollo, `dotnet user-secrets` para la API y `.env` (no
  versionado) para Compose; `.env.example` documenta las variables.
- Código, identificadores y comentarios en inglés; documentación, ADR y mensajes de commit en
  español.
- Frontend: React, todavía no creado; cuando se agregue, queda sujeto al Principio I.

## Flujo de trabajo y quality gates

- Las features se desarrollan con Spec-Driven Development (GitHub Spec Kit): specify → plan →
  tasks → implement. El plan MUST pasar el "Constitution Check" contra estos principios; toda
  violación se justifica explícitamente en la sección de complejidad del plan o se rediseña.
- Definition of done de cualquier cambio de código:
  1. `dotnet build` sin warnings.
  2. `dotnet test` en verde.
  3. `dotnet format --verify-no-changes` sin cambios.
  4. Tests nuevos para el comportamiento nuevo.
  5. Resumen de qué cambió y por qué, listo para revisión, sin commitear.
- Commits: uno por cambio lógico (no mezclar ADR, documentación y código), mensajes en español
  con prefijos Conventional Commits (`feat:`, `fix:`, `docs:`, `refactor:`, `test:`, `chore:`), sin
  atribuciones a herramientas de IA.

## Governance

- Esta constitución prevalece sobre cualquier otra práctica del proyecto. Los ADR registran
  decisiones concretas dentro de su marco; los `CLAUDE.md` (raíz y `backend/`) son la guía
  operativa de ejecución y no pueden contradecirla.
- Enmiendas: se proponen con `/speckit-constitution`, se revisan y aprueban por el tech lead y
  se commitean por separado (`docs:`), sin el Sync Impact Report.
- Versionado semántico:
  - MAJOR: se elimina o redefine un principio de forma incompatible.
  - MINOR: se agrega un principio o sección, o se amplía materialmente una guía.
  - PATCH: aclaraciones y redacción sin cambio semántico.
- Cumplimiento: cada plan de Spec Kit verifica estos principios en su "Constitution Check"; cada
  revisión de código verifica el Principio I (diff fuera de `Domain`/`Application` en cambios de
  tecnología) y la definition of done.

**Version**: 1.0.0 | **Ratified**: 2026-10-02 | **Last Amended**: 2026-10-02
