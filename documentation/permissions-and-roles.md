# Permisos y Roles – Documento Transversal
*Version 1.0 – Marzo 2026*

---

## 1. Resumen

Este documento describe el modelo de permisos y roles del sistema MyFO. Define quien puede acceder a cada funcionalidad, que operaciones puede realizar y bajo que condiciones.

---

## 2. Roles del sistema

### 2.1 Roles a nivel plataforma

| Rol | Descripcion |
|---|---|
| **SuperAdmin** | Administrador de la plataforma. Gestiona FamilyAdminConfig (habilitar/deshabilitar familias, max miembros). No pertenece a familias. |

### 2.2 Roles a nivel familia

| Rol | Descripcion |
|---|---|
| **FamilyAdmin** | Administrador de la familia. Puede gestionar configuracion, miembros, permisos y entidades de configuracion. Para operaciones (movimientos, transferencias) se comporta como un usuario normal — necesita permisos explicitos. |
| **Member** | Miembro de la familia. Accede a entidades de configuracion en modo lectura. Opera movimientos y transferencias segun permisos asignados. |

**Regla clave:** FamilyAdmin NO tiene privilegios implicitos sobre operaciones financieras. Si quiere operar una caja, debe asignarse el permiso explicitamente.

---

## 3. Permisos sobre cajas (CashBoxPermission)

### 3.1 Niveles de permiso

| Nivel | Descripcion |
|---|---|
| **Sin permiso** | No ve la caja ni su saldo. La caja no aparece en ningun listado ni combo. |
| **View** | Ve la caja, su saldo y los movimientos asociados. No puede crear ni editar movimientos que usen esa caja. |
| **Operate** | Todo lo de View + puede crear movimientos, transferencias y pagos TC que usen esa caja como forma de pago. |

### 3.2 Entidad: CashBoxPermission

- CashBoxId + MemberId (PK compuesta logica)
- Permission: View | Operate (enum)
- Es TenantEntity (tiene FamilyId)
- Soft delete (DeletedAt)

### 3.3 Reglas

1. Solo FamilyAdmin puede gestionar permisos (crear, modificar, revocar)
2. Un miembro puede tener permiso en multiples cajas
3. Una caja puede tener multiples miembros con permisos
4. **FamilyAdmin necesita permiso explicito** para operar una caja (no lo tiene por defecto)
5. FamilyAdmin sin permiso explicito en una caja **no la ve** en listados operacionales
6. Al crear una caja, el FamilyAdmin no recibe permiso automatico (debe asignarselo)

### 3.4 Comportamiento en GET /api/cashboxes

Cada caja retorna campos `canView` y `canOperate` segun los permisos del usuario autenticado:
- Si no tiene permiso → la caja NO aparece en la respuesta
- Si tiene View → `canView=true`, `canOperate=false`
- Si tiene Operate → `canView=true`, `canOperate=true`

---

## 4. Permisos sobre bancos y tarjetas de credito

### 4.1 Cuentas bancarias (BankAccount)

**No tienen sistema de permisos.** Todos los miembros activos de la familia pueden ver y operar con todas las cuentas bancarias.

Razon: se descarto permanentemente la replicacion del modelo CashBoxPermission para bancos (marzo 2026).

### 4.2 Tarjetas de credito (CreditCard)

**No tienen sistema de permisos por usuario.** Todos los miembros activos pueden ver y operar con todas las tarjetas. El permiso de operacion depende del CashBoxPermission de la caja/banco desde donde se paga.

---

## 5. Acceso a paginas

### 5.1 Paginas exclusivas de FamilyAdmin

| Pagina | Descripcion |
|---|---|
| **Configuracion (Settings)** | Nombre de familia, monedas, idioma |
| **Miembros (Members)** | Listar miembros, activar/desactivar, cambiar rol, invitar |
| **Permisos de Cajas** | Asignar View/Operate por miembro y caja |

Si un Member intenta acceder a estas paginas, se redirige a la pagina principal.

### 5.2 Paginas de configuracion (Config)

| Pagina | FamilyAdmin | Member |
|---|---|---|
| Cajas | CRUD completo | Solo lectura |
| Cuentas bancarias | CRUD completo | Solo lectura |
| Tarjetas de credito | CRUD completo | Solo lectura |
| Categorias/Subcategorias | CRUD completo | Solo lectura |
| Centros de costo | CRUD completo | Solo lectura |
| Monedas de familia | CRUD completo | Solo lectura |

### 5.3 Paginas operacionales

| Pagina | Acceso |
|---|---|
| **Movimientos** | Todos los miembros activos. Filtrado de cajas por permisos (View para ver, Operate para operar). |
| **Transferencias** | Todos los miembros activos. Origen filtrado por Operate. Destino: todas las cajas. |
| **Pagos TC** | Todos los miembros activos. Cajas origen filtradas por Operate. |
| **Resumenes TC** | Todos los miembros activos. Pueden ver y editar resumenes no cerrados. |
| **Movimientos frecuentes** | Cada miembro gestiona (CRUD) sus propios movimientos frecuentes. Al aplicar, si no tiene Operate en una caja de la plantilla, la deja en blanco para seleccion manual. |
| **Dashboard / Reportes** | Todos los miembros. Solo muestra datos de cajas donde tiene al menos View. |

---

## 6. Reglas de permisos en movimientos

### 6.1 Crear movimiento

- Requiere al menos una forma de pago
- **Cajas**: solo se muestran las que el usuario tiene Operate
- **Bancos**: se muestran todos (sin permisos)
- **Tarjetas**: se muestran todas (sin permisos por usuario)

### 6.2 Ver movimiento

- El usuario ve un movimiento si tiene al menos **View** en alguna de las cajas de sus formas de pago, O si el movimiento usa solo bancos/tarjetas (sin cajas)

### 6.3 Editar movimiento

**Datos editables siempre** (si puede ver el movimiento):
- Fecha, descripcion, subcategoria, centro de costo, caracter (ordinario/extraordinario), tipo contable

**Datos editables solo con Operate en TODAS las cajas de las formas de pago:**
- Moneda del movimiento
- Tipo (Income/Expense)
- Monto
- Formas de pago existentes (modificar monto, cambiar caja)

**Agregar nueva forma de pago:**
- Requiere Operate en la caja de la nueva forma de pago

**Eliminar forma de pago existente:**
- Requiere Operate en la caja de esa forma de pago

**Regla de bloqueo de moneda/tipo:**
- Si hay AL MENOS UNA forma de pago con caja donde el usuario NO tiene Operate → moneda y tipo de movimiento (Income/Expense) quedan **bloqueados** (no editables)

### 6.4 Eliminar movimiento

- Requiere Operate en **TODAS** las cajas de todas las formas de pago del movimiento
- Si alguna caja no tiene Operate → no se puede eliminar

---

## 7. Reglas de permisos en transferencias

### 7.1 Crear transferencia

- **Origen (caja)**: requiere Operate
- **Origen (banco)**: sin restriccion (bancos no tienen permisos)
- **Destino**: se muestran todas las cajas y bancos (independientemente de permisos)
- Si el usuario tiene Operate en la caja destino → auto-confirmacion (Status=Confirmed, IsAutoConfirmed=true)
- Si NO tiene Operate en la caja destino → queda PendingConfirmation
- Si el destino es banco → auto-confirmacion (bancos no tienen permisos)

### 7.2 Confirmar transferencia pendiente

- Requiere Operate en la caja destino
- Solo transferencias en estado PendingConfirmation

### 7.3 Rechazar transferencia pendiente

- Requiere Operate en la caja destino (o ser el creador)

### 7.4 Editar/Eliminar transferencia

- Pendientes de definir reglas exactas de edicion de transferencias auto-confirmadas (recalculo de saldos)
- Documentado en: `project_pending_transfer_edit_autoconfirmed.md`

---

## 8. Reglas de permisos en pagos de tarjeta de credito

### 8.1 Crear pago TC

- Requiere Operate en la caja/banco de origen
- Cajas: solo se muestran las que tienen Operate
- Bancos: se muestran todos

### 8.2 Editar pago TC

- **Datos editables siempre**: exchange rates, descripcion, fecha
- **Datos NO editables sin Operate en la caja origen**: caja origen, monto del pago
- **No se puede cambiar moneda** sin Operate en la caja origen
- **No se puede eliminar** sin Operate en la caja origen

---

## 9. Reglas de permisos en resumenes de tarjeta (Statement Periods)

- Todos los miembros activos pueden ver resumenes
- Todos los miembros activos pueden editar resumenes **no cerrados** (cuotas, cargos, bonificaciones)
- El cierre de periodo: requiere FamilyAdmin (pendiente definir si Member puede cerrar)
- Pagos dentro del resumen: siguen reglas de pagos TC (seccion 8)

---

## 10. Gestion de miembros

### 10.1 Acciones sobre miembros

| Accion | Quien puede | Restricciones |
|---|---|---|
| **Ver miembros** | FamilyAdmin ve todos (activos e inactivos). Member ve solo activos. | — |
| **Desactivar miembro** | Solo FamilyAdmin | No puede desactivarse a si mismo. Requiere confirmacion. |
| **Reactivar miembro** | Solo FamilyAdmin | No puede superar MaxMembers de FamilyAdminConfig. |
| **Cambiar rol** | Solo FamilyAdmin | No puede cambiar su propio rol. Debe quedar al menos un FamilyAdmin. |
| **Invitar** | Solo FamilyAdmin | No puede superar MaxMembers. Genera link con token. |
| **Eliminar miembro** | **No permitido** | Los miembros se desactivan, nunca se eliminan. Datos historicos se mantienen. |

### 10.2 Invitaciones

- Token unico, un solo uso
- Expiran en **7 dias**
- Estados: Pending → Accepted / Expired
- Al aceptar: se crea FamilyMember con rol Member
- El link se copia al clipboard del FamilyAdmin

---

## 11. FamilyAdminConfig

Tabla a nivel plataforma (gestionada por SuperAdmin):
- **IsEnabled**: si la familia esta habilitada para operar
- **MaxMembers**: cantidad maxima de miembros activos permitidos
- **Notes**: notas internas del SuperAdmin
- **DisabledAt/DisabledReason**: si la familia fue deshabilitada

Esta tabla es **separada** de Family (que es configuracion del tenant gestionada por FamilyAdmin).

---

## 12. Resumen de permisos por entidad

| Entidad | Ver | Operar | Gestionar (CRUD) |
|---|---|---|---|
| Caja | CashBoxPermission View+ | CashBoxPermission Operate | FamilyAdmin |
| Banco | Todos los miembros | Todos los miembros | FamilyAdmin |
| Tarjeta de credito | Todos los miembros | Todos (permiso depende de caja/banco de pago) | FamilyAdmin |
| Categoria/Subcategoria | Todos los miembros | N/A | FamilyAdmin |
| Centro de costo | Todos los miembros | N/A | FamilyAdmin |
| Moneda de familia | Todos los miembros | N/A | FamilyAdmin |
| Movimiento | View en alguna caja del pago | Operate en cajas del pago | N/A |
| Transferencia | Ver origen o destino | Operate en origen | N/A |
| Pago TC | Ver caja/banco origen | Operate en caja/banco origen | N/A |
| Miembros | FamilyAdmin (todos), Member (activos) | N/A | FamilyAdmin |
| Settings | FamilyAdmin | N/A | FamilyAdmin |
| Permisos cajas | FamilyAdmin | N/A | FamilyAdmin |
