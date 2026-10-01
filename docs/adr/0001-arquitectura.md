# ADR 0001: Arquitectura de mini-erp

- Estado: Reemplazada por ADR-0002
- Fecha: 2026-10-01

## Contexto
mini-erp es un ERP didáctico para gestionar clientes, productos y facturas. La lógica de negocio es
mayormente CRUD. Lo desarrolla una sola persona y el objetivo principal es aprender; más adelante se
sumará un frontend React que consumirá la API.

## Opciones consideradas
1. Capas simples en un solo proyecto (carpetas) — pros: mínima ceremonia, fácil de navegar y de
   cambiar, encaja con un dominio CRUD / contras: los límites entre capas dependen de la disciplina,
   no del compilador; escala peor si la lógica de negocio crece.
2. Vertical Slice — pros: cada feature queda aislada y cambia por separado / contras: aporta poco
   con pocas features CRUD y tiende a duplicar código entre slices.
3. Clean Architecture — pros: dominio aislado de la infraestructura, límites forzados por proyectos
   / contras: varios proyectos y abstracciones que, para CRUD, son ceremonia sin beneficio.

## Decisión
Capas simples en un solo proyecto (`MiniErp.Api`). Es un proyecto didáctico mayormente CRUD: lo más
simple que resuelve el problema.

## Consecuencias
- Positivas: menos proyectos y menos indirección; agregar un recurso nuevo es rápido.
- Negativas / lo que aceptamos pagar: la separación entre capas no la hace cumplir el compilador;
  migrar a otra arquitectura más adelante implica reorganizar código.
- Qué nos haría revisar esta decisión: si la lógica de negocio crece más allá de CRUD.
