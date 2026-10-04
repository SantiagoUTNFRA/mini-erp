# ADR 0006: Integración continua con GitHub Actions

- Estado: Aceptada
- Fecha: 2026-10-04

## Contexto
La definition of done del backend (`backend/CLAUDE.md`) exige build sin warnings, tests en verde y
formato sin cambios, pero hasta ahora se verificaba a mano antes de cada merge. La feature
001-gestion-clientes mostró el riesgo: su PR se mergeó antes de completar la definition of done. Nada
impedía mergear un PR en rojo.

El repositorio está en GitHub y es público. Los tests de integración necesitan Docker
(Testcontainers, ADR-0005). El objetivo es que las reglas las haga cumplir una herramienta, igual que
los tests de arquitectura hacen cumplir ADR-0002.

Versiones al 2026-10-04: `actions/checkout` v7.0.1, `actions/setup-dotnet` v6.0.0.

## Opciones consideradas
1. GitHub Actions — pros: integrado con los PR y la protección de ramas del mismo repo; gratis para
   repos públicos; los runners Linux traen Docker, así que Testcontainers funciona sin configuración
   / contras: el pipeline queda atado a GitHub.
2. Azure Pipelines — pros: buena integración con .NET / contras: otra plataforma y otra cuenta que
   administrar para un repo que ya vive en GitHub; la integración con los PR es indirecta.
3. Sin CI, verificación manual — pros: nada que mantener / contras: depende de la disciplina, que ya
   falló una vez; nada bloquea un merge en rojo.

## Decisión
GitHub Actions, con un workflow `backend-ci.yml` que en cada PR a `main` y en cada push a `main`
corre la definition of done verificable por máquina: `dotnet restore`, `dotnet build` (los warnings
rompen el build), `dotnet test` (unitarios, arquitectura e integración contra MySQL con
Testcontainers) y `dotnet format --verify-no-changes`.

- `main` queda protegida: solo entra por PR y con el check del CI en verde. No se exige aprobación de
  otra persona (hay un solo desarrollador).
- El workflow corre siempre, sin filtro por rutas: con un check obligatorio, un PR que no dispara el
  workflow quedaría bloqueado esperando un resultado que nunca llega.
- El test de rendimiento (explícito) no corre en CI: los runners son compartidos y sus tiempos
  varían, lo que daría falsos rojos.
- Las acciones se fijan a su versión mayor (`@v7`, `@v6`).

## Consecuencias
- Positivas: un PR en rojo no se puede mergear; la definition of done deja de depender de la memoria;
  el estado del CI es visible en cada PR y en el README.
- Negativas / lo que aceptamos pagar: cada PR espera unos minutos (incluido el arranque del contenedor
  MySQL), también los que solo cambian documentación; nadie puede pushear directo a `main`, ni el tech
  lead ni el agente.
- Qué nos haría revisar esta decisión: que el tiempo del CI frene el trabajo (filtrar por rutas con una
  acción que reporte el check igual), o que el repositorio deje GitHub.
