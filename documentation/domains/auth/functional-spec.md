# Auth – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Autenticacion y autorizacion del sistema. Maneja registro, login, JWT, gestion de perfil de usuario, y seleccion de familia.

---

## 2. Entidades

### ApplicationUser (ASP.NET Identity)
- Id (Guid)
- UserName (= email)
- Email
- FullName
- Password (hashed por Identity)
- CreatedAt, UpdatedAt

### UserFamily (vista desde FamilyMember)
Cada usuario puede pertenecer a multiples familias. El JWT incluye el FamilyId de la familia activa.

---

## 3. Flujos

### 3.1 Registro (crear familia nueva)
1. Usuario ingresa email, password, nombre completo, nombre de familia, moneda primaria/secundaria
2. Se crea ApplicationUser + Family + FamilyAdminConfig + FamilyMember (rol FamilyAdmin) en una sola transaccion
3. Se auto-asocian monedas primaria y secundaria a la familia via FamilyCurrency
4. Se genera JWT con FamilyId claim
5. RLS: se ejecuta `SET app.current_family_id` antes del INSERT de family_members

### 3.2 Registro con invitacion
1. Usuario recibe link con token de invitacion (`/join?token=xxx`)
2. Si no tiene cuenta: se registra y se crea FamilyMember en la familia que invito (rol Member)
3. Si ya tiene cuenta: acepta la invitacion y se crea FamilyMember
4. Validacion: token debe ser valido, no expirado (7 dias), no usado
5. Validacion: no puede superar MaxMembers de FamilyAdminConfig
6. JWT incluye el FamilyId de la familia invitante

### 3.3 Login
1. Email + password
2. Usa `IgnoreQueryFilters()` para acceder sin tenant
3. Ejecuta `SET app.current_user_id` para RLS
4. Retorna lista de familias del usuario (UserFamilies)
5. Si el usuario pertenece a una sola familia: se auto-selecciona y genera JWT con FamilyId
6. Si pertenece a multiples: se redirige a pantalla de seleccion de familia

### 3.4 Seleccion de familia
1. Usuario elige familia de la lista
2. Se genera nuevo JWT con el FamilyId seleccionado
3. Solo familias donde el miembro esta activo (IsActive=true)

### 3.5 Gestion de perfil
- Actualizar nombre completo
- Cambiar password (requiere password actual)

---

## 4. Endpoints

| Metodo | Ruta | Descripcion |
|---|---|---|
| POST | /api/auth/register | Registrar usuario + crear familia |
| POST | /api/auth/login | Login email/password → retorna familias |
| POST | /api/auth/select-family | Seleccionar familia → genera JWT |
| POST | /api/auth/register-with-invitation | Registrar + unirse a familia |
| POST | /api/auth/accept-invitation | Aceptar invitacion (usuario existente) |
| PUT | /api/auth/profile | Actualizar perfil |
| PUT | /api/auth/change-password | Cambiar password |

---

## 5. Reglas de negocio

1. El registro crea usuario + familia + FamilyAdminConfig + miembro en una sola transaccion
2. El primer usuario de una familia siempre es FamilyAdmin
3. JWT contiene: UserId, FamilyId, Role (FamilyAdmin o Member)
4. Login requiere `IgnoreQueryFilters()` porque el usuario aun no tiene tenant context
5. RLS exige SET de session variables antes de cualquier operacion con datos tenant-scoped
6. Un usuario puede pertenecer a multiples familias con roles diferentes en cada una
7. Solo miembros activos pueden hacer login en una familia
8. Google OAuth: previsto en roadmap, no implementado en MVP
9. Verificacion de email: previsto en roadmap, no implementado en MVP
10. Forgot/Reset password: previsto en roadmap, no implementado en MVP

---

## 6. Seguridad

- Passwords hasheados por ASP.NET Identity (bcrypt)
- JWT con expiracion configurable
- JWT incluye claim `Role` (FamilyAdmin | Member) — usado para control de acceso en frontend y backend
- Refresh token: previsto, no implementado en MVP
- Dos connection strings: postgres (migraciones) y myfo_app (runtime con RLS)
- `ICurrentUserService` expone: UserId, FamilyId, IsSuperAdmin, IsFamilyAdmin

---

## 7. Control de acceso por rol

Ver documento transversal: `documentation/permissions-and-roles.md`

- **FamilyAdmin**: acceso completo a configuracion, miembros, permisos. Para operaciones financieras se comporta como usuario normal (necesita permisos explicitos sobre cajas).
- **Member**: acceso lectura a configuracion. Operaciones segun permisos de caja asignados.
