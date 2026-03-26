# Cuentas Bancarias – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Representan dinero en bancos o billeteras digitales. Clasificacion interna: Activo. Cada cuenta tiene una moneda unica. **No tienen sistema de permisos por usuario** — todos los miembros activos pueden ver y operar.

---

## 2. Entidades

### BankAccount (TenantEntity)
- BankAccountId (Guid), FamilyId (Guid)
- Name (max 100 chars, unico)
- CurrencyCode (moneda unica, ISO 4217)
- InitialBalance (decimal)
- Balance (decimal — calculado)
- AccountNumber (opcional, max 30 chars)
- CBU (opcional, max 30 chars) — codigo bancario argentino
- Alias (opcional, max 50 chars)
- IsActive: bool

---

## 3. Endpoints

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/bank-accounts | Listar cuentas | Todos los miembros |
| POST | /api/bank-accounts | Crear | FamilyAdmin |
| PUT | /api/bank-accounts/{id} | Actualizar | FamilyAdmin |
| DELETE | /api/bank-accounts/{id} | Eliminar | FamilyAdmin |

---

## 4. Reglas de negocio

1. **Nombre unico** por familia
2. **Reactivacion**: si se recrea una eliminada, se restaura
3. **Moneda no se puede cambiar** si tiene movimientos o transferencias
4. **Saldo inicial** no se puede cambiar si tiene una consolidacion (futuro)
5. **Balance** = InitialBalance + suma de movimientos
6. **IsActive**: inactivas no aparecen en combos

### Control de acceso
- **No tienen permisos por usuario.** Se descarto permanentemente la replicacion del modelo CashBoxPermission para bancos (decision marzo 2026).
- **FamilyAdmin**: CRUD completo sobre la entidad.
- **Member**: solo lectura sobre la entidad. Todos los miembros pueden operar (crear movimientos, transferencias) con cualquier banco.
- Ver documento transversal: `documentation/permissions-and-roles.md`

### Reconciliacion bancaria (v2)
- Importar extracto bancario y conciliar con movimientos registrados
