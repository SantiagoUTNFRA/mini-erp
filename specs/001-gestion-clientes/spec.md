# Feature Specification: Gestión de clientes

**Feature Branch**: `001-gestion-clientes`

**Created**: 2026-10-02

**Status**: Draft

**Input**: User description: "Gestión de clientes del ERP. Un usuario del sistema necesita dar de alta
clientes, consultarlos (listado y detalle), modificar sus datos y darlos de baja, porque los clientes
son la base para emitir facturas más adelante. Un cliente no puede registrarse dos veces. No se puede
dar de baja un cliente que tenga facturas asociadas. Esta feature no incluye facturación,
autenticación de usuarios ni interfaz gráfica: solo la funcionalidad expuesta por la API."

## Clarifications

### Session 2026-10-02

- Q: Al modificar un cliente, ¿el usuario envía todos los datos o solo los que cambian? → A:
  Reemplazo completo; los opcionales no enviados quedan vacíos.
- Q: En el listado, ¿cuántos clientes por página por defecto y cuál es el máximo? → A: 20 por
  defecto, máximo 100 (valores provisionales, sin datos reales de volumen).
- Q: ¿Qué largo máximo tienen los campos de texto del cliente? → A: Razón social 200, dirección 300,
  email 254, teléfono 30 caracteres.
- Q: ¿El teléfono se valida con algún formato o es texto libre? → A: Solo dígitos, espacios,
  guiones, paréntesis y un `+` inicial; al menos 6 dígitos.
- Q: Para una persona física, ¿el nombre va en un único campo o separado en nombre y apellido? → A:
  Un único campo "razón social" para todos los clientes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dar de alta un cliente (Priority: P1)

Un usuario del sistema registra un cliente nuevo con sus datos para poder facturarle más adelante.
El sistema valida los datos, rechaza duplicados y devuelve el cliente creado con su identificador.

**Why this priority**: sin clientes registrados no hay nada que consultar, modificar ni facturar; es
el mínimo que aporta valor por sí solo.

**Independent Test**: se puede probar completo registrando un cliente y verificando que el sistema
lo confirma con un identificador y los datos ingresados.

**Acceptance Scenarios**:

1. **Given** no existe un cliente con el mismo CUIT/CUIL, **When** el usuario registra un cliente
   con todos los datos obligatorios válidos, **Then** el sistema lo crea y
   devuelve el cliente con su identificador asignado.
2. **Given** ya existe un cliente con el mismo CUIT/CUIL, **When** el usuario intenta registrarlo
   de nuevo, **Then** el sistema rechaza el alta indicando que el cliente ya existe y no
   crea ningún registro.
3. **Given** el usuario envía datos incompletos o con formato inválido, **When** intenta registrar el
   cliente, **Then** el sistema rechaza el alta indicando cada campo con problemas.
4. **Given** el usuario envía un CUIT/CUIL con 11 dígitos pero dígito verificador incorrecto,
   **When** intenta registrar el cliente, **Then** el sistema rechaza el alta indicando que el
   CUIT/CUIL es inválido.

---

### User Story 2 - Consultar clientes (Priority: P2)

Un usuario del sistema obtiene el listado de clientes registrados y el detalle de un cliente puntual.

**Why this priority**: es lo primero que se necesita después del alta, para verificar qué clientes
existen y elegir a cuál facturar.

**Independent Test**: con clientes precargados, se puede probar pidiendo el listado y el detalle de
uno de ellos y comparando con los datos esperados.

**Acceptance Scenarios**:

1. **Given** existen clientes registrados, **When** el usuario pide el listado, **Then** el sistema
   devuelve los clientes paginados y ordenados alfabéticamente por razón social.
2. **Given** existe un cliente, **When** el usuario pide su detalle por identificador, **Then** el
   sistema devuelve todos sus datos.
3. **Given** no existe un cliente con ese identificador, **When** el usuario pide su detalle,
   **Then** el sistema indica que el cliente no existe.
4. **Given** no hay clientes registrados, **When** el usuario pide el listado, **Then** el sistema
   devuelve un listado vacío, no un error.

---

### User Story 3 - Modificar los datos de un cliente (Priority: P3)

Un usuario del sistema corrige o actualiza los datos de un cliente existente.

**Why this priority**: los datos de los clientes cambian, pero el sistema es útil antes de poder
editarlos.

**Independent Test**: con un cliente precargado, se modifica un dato y se verifica en su detalle.

**Acceptance Scenarios**:

1. **Given** existe un cliente, **When** el usuario envía datos válidos nuevos, **Then** el sistema
   los guarda y devuelve el cliente actualizado.
2. **Given** existe otro cliente con el CUIT/CUIL que el usuario intenta asignar,
   **When** el usuario modifica el cliente, **Then** el sistema rechaza el cambio por duplicado y el
   cliente queda sin cambios.
3. **Given** no existe un cliente con ese identificador, **When** el usuario intenta modificarlo,
   **Then** el sistema indica que el cliente no existe.
4. **Given** el usuario envía datos inválidos, **When** intenta modificar el cliente, **Then** el
   sistema rechaza el cambio indicando cada campo con problemas y el cliente queda sin cambios.
5. **Given** un cliente tiene teléfono registrado, **When** el usuario lo modifica sin enviar el
   teléfono, **Then** el cliente queda sin teléfono.

---

### User Story 4 - Dar de baja un cliente (Priority: P4)

Un usuario del sistema da de baja un cliente con el que ya no se opera.

**Why this priority**: es la operación menos frecuente; el sistema funciona aunque nunca se dé de
baja a nadie.

**Independent Test**: con un cliente precargado sin facturas, se da de baja y se verifica que ya no
aparece en el listado ni en el detalle.

**Acceptance Scenarios**:

1. **Given** existe un cliente sin facturas asociadas, **When** el usuario lo da de baja, **Then** el
   sistema lo elimina definitivamente y confirma la baja; a partir de ese momento no aparece en el
   listado y su detalle indica que no existe.
2. **Given** existe un cliente con al menos una factura asociada, **When** el usuario intenta darlo de
   baja, **Then** el sistema rechaza la baja indicando que el cliente tiene facturas.
3. **Given** no existe un cliente con ese identificador, **When** el usuario intenta darlo de baja,
   **Then** el sistema indica que el cliente no existe.
4. **Given** un cliente fue dado de baja, **When** el usuario registra un cliente nuevo con el mismo
   CUIT/CUIL, **Then** el sistema lo acepta como un cliente nuevo, con otro identificador.

---

### Edge Cases

- Dos altas simultáneas del mismo cliente: solo una debe prosperar; la otra se rechaza por
  duplicado.
- El mismo CUIT/CUIL escrito con o sin guiones o espacios (`20-12345678-6`, `20123456786`): se
  considera el mismo cliente.
- Email con mayúsculas o espacios al inicio o al final: se normaliza antes de validar y guardar.
- Datos opcionales enviados vacíos: se guardan como ausentes, no como texto vacío.
- Campos de texto con solo espacios: se tratan como vacíos.
- Página solicitada fuera de rango en el listado: se devuelve una página vacía, no un error.
- Modificar un cliente dejándole el mismo CUIT/CUIL que ya tenía: no es duplicado.
- Dos modificaciones simultáneas del mismo cliente: prevalece la última.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST permitir registrar un cliente con estos datos:
  - Obligatorios: razón social, CUIT/CUIL y condición frente al IVA. La razón social es un único
    campo para todos los clientes; para una persona física contiene su nombre completo (por
    convención "Apellido, Nombre", que el sistema no valida).
  - Opcionales: email, teléfono y dirección.
- **FR-002**: El sistema MUST asignar a cada cliente un identificador único e inmutable al crearlo.
- **FR-003**: El sistema MUST impedir que existan dos clientes con el mismo CUIT/CUIL, tanto al
  registrar como al modificar. La comparación se hace sobre los 11 dígitos, ignorando guiones y
  espacios.
- **FR-004**: El sistema MUST validar los datos obligatorios y su formato, y ante un error MUST
  indicar cada campo inválido y el motivo. En particular:
  - CUIT/CUIL: 11 dígitos con dígito verificador válido.
  - Condición frente al IVA: uno de los valores admitidos (ver Assumptions).
  - Email, si se informa: formato de email válido.
  - Teléfono, si se informa: solo dígitos, espacios, guiones, paréntesis y un `+` opcional al
    inicio, con al menos 6 dígitos.
  - Largos máximos (en caracteres, después de quitar espacios al inicio y al final): razón social
    200, dirección 300, email 254, teléfono 30. Un valor más largo se rechaza; nunca se trunca.
- **FR-005**: El sistema MUST devolver el listado de clientes paginado y ordenado alfabéticamente por
  razón social, junto con el total de clientes. El tamaño de página es 20 si el usuario no lo indica
  y no puede superar 100; un tamaño o número de página inválido (cero, negativo o mayor al máximo) se
  rechaza como error de validación.
- **FR-006**: El sistema MUST devolver el detalle completo de un cliente a partir de su
  identificador.
- **FR-007**: El sistema MUST permitir modificar los datos de un cliente existente, aplicando las
  mismas validaciones que en el alta. La modificación es un reemplazo completo: el usuario envía
  todos los datos del cliente y los opcionales que no envía quedan vacíos.
- **FR-008**: El sistema MUST permitir dar de baja un cliente sin facturas asociadas. La baja es
  física: el cliente se elimina definitivamente y su CUIT/CUIL queda disponible para un alta nueva.
- **FR-009**: El sistema MUST rechazar la baja de un cliente con facturas asociadas, indicando el
  motivo.
- **FR-010**: Ante una operación sobre un cliente inexistente, el sistema MUST indicar que el
  cliente no existe, de forma distinguible de un error de validación.
- **FR-011**: Ante un rechazo (validación, duplicado, cliente inexistente o con facturas), el sistema
  MUST NOT modificar ningún dato.

### Key Entities

- **Cliente**: persona o empresa a la que se le emitirán facturas. Tiene un identificador único
  asignado por el sistema, razón social, CUIT/CUIL (único entre clientes), condición frente al IVA y,
  opcionalmente, email, teléfono y dirección.
- **Factura** (fuera de alcance, solo como referencia): se asociará a un cliente en una feature
  futura; su existencia condiciona la baja del cliente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100 % de los intentos de registrar un cliente con un CUIT/CUIL ya existente es
  rechazado, incluidos los que difieren solo en guiones o espacios.
- **SC-002**: El 100 % de los CUIT/CUIL con dígito verificador incorrecto es rechazado.
- **SC-003**: El 100 % de las operaciones rechazadas deja los datos exactamente como estaban.
- **SC-004**: Todo rechazo por datos inválidos informa todos los campos con problemas en una sola
  respuesta, sin necesidad de reintentar para descubrir el siguiente error.
- **SC-005**: Con 10.000 clientes registrados, el listado y el detalle responden de forma percibida
  como inmediata (menos de 1 segundo).
- **SC-006**: Cada una de las cuatro operaciones (alta, consulta, modificación, baja) puede ejecutarse
  y verificarse de forma independiente.

## Assumptions

- El único consumidor es un usuario del sistema a través de la API; no hay roles ni permisos
  distintos, porque la autenticación está fuera de alcance.
- La facturación no existe todavía: hasta que exista, ningún cliente tiene facturas y la baja nunca
  se rechaza por ese motivo. La regla (FR-009) se deja definida para que la feature de facturación
  la active.
- El listado no incluye búsqueda ni filtros en esta feature; solo paginación (ver FR-005).
- Los límites de paginación (20/100) y el volumen de SC-005 (10.000 clientes) son estimaciones sin
  datos reales de uso; se revisan si el uso real los contradice.
- No se registra historial de cambios (auditoría) de los datos del cliente.
- Ante modificaciones concurrentes del mismo cliente prevalece la última (sin control de
  concurrencia optimista).
- Un único país (Argentina): no se contemplan clientes del exterior con identificaciones fiscales
  extranjeras.
- Valores admitidos de condición frente al IVA: Responsable Inscripto, Monotributista, Exento y
  Consumidor Final.
- Un consumidor final sin CUIT/CUIL no se puede registrar como cliente en esta feature (el CUIT/CUIL
  es obligatorio).
- El CUIT/CUIL de un cliente se puede modificar (para corregir errores de carga), respetando la
  unicidad. Cuando exista facturación habrá que revisar si sigue siendo válido modificarlo.
- La dirección es un único texto libre; no se estructura en calle, localidad, provincia, etc.
- La baja no deja rastro: no hay papelera ni posibilidad de deshacerla.
