# Family Settings – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Configuracion de la familia (tenant), gestion de miembros e invitaciones. Incluye la pagina de Settings (solo FamilyAdmin) y la pagina de Miembros (solo FamilyAdmin).

---

## 2. Entidades

### Family (BaseEntity — NO es TenantEntity, ES el tenant)
- FamilyId (Guid) — PK
- Name
- PrimaryCurrencyCode (ISO 4217)
- SecondaryCurrencyCode (ISO 4217, nullable)
- Language (ISO 639-1, default "es")

### FamilyMember (TenantEntity)
- MemberId (Guid)
- UserId (Guid) → ApplicationUser
- FamilyId (Guid) → Family
- Role: Member | FamilyAdmin
- DisplayName
- IsActive: bool

### FamilyAdminConfig (BaseEntity — gestionada por SuperAdmin)
- FamilyId (Guid) → Family
- IsEnabled: bool
- MaxMembers: int
- Notes: string (nullable)
- DisabledAt: DateTime (nullable)
- DisabledReason: string (nullable)

### FamilyInvitation (BaseEntity)
- InvitationId (Guid), FamilyId (Guid)
- Token (unico, un solo uso)
- InvitedByDisplayName: string
- ExpiresAt: DateTime (7 dias desde creacion)
- AcceptedAt: DateTime (nullable)
- AcceptedByUserId: Guid (nullable)

---

## 3. Endpoints

### Configuracion (Settings)

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/family-settings | Obtener configuracion de familia | FamilyAdmin |
| PUT | /api/family-settings | Actualizar configuracion | FamilyAdmin |

### Miembros

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/family-members | Listar miembros | FamilyAdmin: todos. Member: solo activos |
| PUT | /api/family-members/{id}/toggle-active | Activar/desactivar miembro | FamilyAdmin |
| PUT | /api/family-members/{id}/role | Cambiar rol del miembro | FamilyAdmin |
| GET | /api/family-members/invitations | Listar invitaciones pendientes | FamilyAdmin |

### Invitaciones

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| POST | /api/invitations | Crear invitacion (genera token) | FamilyAdmin |
| GET | /api/invitations/{token} | Info de invitacion (para pantalla de aceptacion) | Publico |

---

## 4. Reglas de negocio

### Configuracion
1. Cambiar moneda primaria/secundaria auto-asocia la nueva moneda en FamilyCurrency
2. **No se puede cambiar moneda** si existen movimientos o transferencias
3. Solo FamilyAdmin puede modificar configuracion
4. Cambiar idioma actualiza el frontend en tiempo real (i18n)

### Miembros
1. DisplayName obligatorio
2. Un usuario (email) puede pertenecer a multiples familias
3. Miembros inactivos no pueden operar pero sus datos historicos se mantienen
4. **No se permite eliminar miembros** — solo desactivar
5. **Desactivar**: solo FamilyAdmin, no puede desactivarse a si mismo, requiere confirmacion
6. **Reactivar**: solo FamilyAdmin, valida que no se supere MaxMembers de FamilyAdminConfig
7. **Cambiar rol**: solo FamilyAdmin, no puede cambiar su propio rol, debe quedar al menos un FamilyAdmin activo
8. FamilyAdmin ve todos los miembros (activos e inactivos). Member ve solo activos.

### Invitaciones
1. Solo FamilyAdmin puede crear invitaciones
2. Token unico, un solo uso
3. Expiran en **7 dias**
4. Estados: Pending → Accepted / Expired (por vencimiento)
5. Al aceptar: se crea FamilyMember con rol Member
6. Valida MaxMembers antes de aceptar
7. El link se genera como `{origin}/join?token={token}` y se copia al clipboard del admin

### FamilyAdminConfig
1. Tabla separada de Family — gestionada exclusivamente por SuperAdmin
2. Family contiene configuracion del tenant (nombre, monedas, idioma)
3. FamilyAdminConfig contiene configuracion de plataforma (habilitacion, limites)
4. MaxMembers: limite de miembros activos (se valida al reactivar y al aceptar invitacion)

---

## 5. Control de acceso

Ver documento transversal: `documentation/permissions-and-roles.md`

- Pagina Settings: solo FamilyAdmin
- Pagina Miembros: solo FamilyAdmin
- Si un Member intenta acceder, se redirige a la pagina principal
- Frontend: rutas protegidas con `FamilyAdminRoute` que valida `isFamilyAdmin` del JWT
