# ADR 0004: Entorno local con Docker Compose

- Estado: Aceptada
- Fecha: 2026-10-02

## Contexto
ADR-0003 eligió MySQL como base de datos. En la máquina de desarrollo no hay MySQL instalado; sí
Docker Desktop. El entorno local tiene que ser reproducible (cualquiera que clone el repo levanta lo
mismo) y respetar el objetivo de reemplazabilidad de ADR-0002: cambiar de base de datos no debería
obligar a reinstalar nada en el sistema. Más adelante se sumará un frontend React.

Imagen oficial de MySQL al 2026-10-02: la LTS es la 9.7 (tag `lts` → 9.7.2); `latest` apunta a la
rama innovation (26.7), con releases frecuentes y soporte corto.

## Opciones consideradas
1. Docker Compose (`compose.yaml` en la raíz) — pros: estándar, independiente del stack (sirve igual
   para el frontend), versionado en el repo / contras: el connection string de la API se configura a
   mano.
2. .NET Aspire (proyecto AppHost) — pros: conecta connection strings automáticamente y trae dashboard
   de observabilidad / contras: más proyectos y paquetes; ata la orquestación del entorno a .NET.
3. MySQL instalado con Homebrew — pros: sin Docker / contras: ensucia el sistema; cambiar de versión o
   de base de datos es costoso.

## Decisión
Docker Compose con un `compose.yaml` en la raíz del monorepo. MySQL usa la imagen `mysql:9.7` (LTS),
fijada a la versión minor para que no cambie sin una decisión explícita.

- Credenciales en `.env` (no versionado); `.env.example` versionado documenta las variables.
- El puerto se publica solo en `127.0.0.1`, no en la red local.
- Los datos viven en un volumen con nombre; un healthcheck indica cuándo MySQL acepta conexiones.
- El connection string de la API va en `dotnet user-secrets`, no en `appsettings*.json`.

## Consecuencias
- Positivas: el entorno se levanta y se destruye con un comando; cambiar de base de datos es cambiar
  un servicio del `compose.yaml`; el frontend se suma como otro servicio.
- Negativas / lo que aceptamos pagar: requiere Docker Desktop corriendo; el connection string de la
  API y las credenciales de `.env` se mantienen sincronizados a mano.
- Qué nos haría revisar esta decisión: si sincronizar configuración entre servicios se vuelve tedioso
  (Aspire lo resuelve), o si los tests de integración necesitan otra estrategia de entorno.
