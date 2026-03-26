# Cajas – Especificacion Funcional
*Version 1.2 – Marzo 2026*

---

## 1. Resumen

Representan dinero fisico disponible (billeteras, cajas fuertes, sobres). Clasificacion interna: Activo. Cada caja tiene una moneda unica y permisos por miembro.

---

## 2. Entidades

### CashBox (TenantEntity)
- CashBoxId (Guid), FamilyId (Guid)
- Name (max 100 chars, unico)
- CurrencyCode (moneda unica, ISO 4217)
- InitialBalance (decimal)
- Balance (decimal — calculado: InitialBalance + movimientos)
- IsActive: bool

### CashBoxPermission (TenantEntity)
- CashBoxId (Guid) → CashBox
- MemberId (Guid) → FamilyMember
- *(Sin campo Permission — la existencia del registro indica permiso para operar)*

---

## 3. Endpoints

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/cashboxes | Listar cajas (filtrado por permisos) | Todos (filtrado) |
| POST | /api/cashboxes | Crear caja | FamilyAdmin |
| PUT | /api/cashboxes/{id} | Actualizar | FamilyAdmin |
| DELETE | /api/cashboxes/{id} | Eliminar | FamilyAdmin |
| GET | /api/cashboxes/{id}/permissions | Listar permisos | FamilyAdmin |
| PUT | /api/cashboxes/{id}/permissions/{memberId} | Otorgar permiso | FamilyAdmin |
| DELETE | /api/cashboxes/{id}/permissions/{memberId} | Revocar permiso | FamilyAdmin |

---

## 4. Reglas de negocio

### Caja
1. **Nombre unico** por familia
2. **Reactivacion**: si se recrea una eliminada, se restaura
3. **Moneda no se puede cambiar** si tiene movimientos o transferencias
4. **Saldo inicial** se puede cambiar siempre que NO tenga un cierre (arqueo). No depende de movimientos.
5. **Balance** = InitialBalance + suma de movimientos (ingresos positivos, egresos negativos)
6. **IsActive**: inactivas no aparecen en combos al crear movimientos

### Permisos (binario: tiene o no tiene)
1. **Con permiso** (registro existe): puede ver y operar la caja (crear movimientos, transferencias)
2. **Sin permiso** (registro no existe o soft-deleted): la caja no aparece en ningun listado ni combo
3. Un miembro puede tener permiso en multiples cajas
4. Una caja puede tener multiples miembros con permisos
5. GET /api/cashboxes: Admin ve todas las cajas. Miembro solo ve las que tiene permiso.
6. **FamilyAdmin necesita permiso explicito** — no tiene acceso implicito a ninguna caja para operar
7. Solo FamilyAdmin puede gestionar permisos (otorgar, revocar)
8. Al crear una caja, el FamilyAdmin no recibe permiso automatico
9. **Soft delete y re-otorgamiento**: al revocar se hace soft-delete. Al re-otorgar se restaura el registro existente (evita error de PK duplicada)

### Control de acceso
- **FamilyAdmin**: CRUD completo sobre la entidad caja. Para operar (movimientos, transferencias) necesita permiso explicito.
- **Member**: solo lectura sobre la entidad caja (ve las cajas donde tiene permiso). Opera segun permiso asignado.

### UI de permisos
- Dialogo con checkbox por miembro (marcado = puede operar, desmarcado = sin acceso)

Ver documento transversal: `documentation/permissions-and-roles.md`

### Arqueo (v1.1 — no MVP)
- Cierre manual: usuario ingresa saldo fisico real
- Diferencia genera movimiento de ajuste automatico
- Movimientos anteriores al cierre: no modificables
