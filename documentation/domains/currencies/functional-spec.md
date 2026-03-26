# Currencies – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Gestion de monedas globales y asociacion de monedas por familia. Soporte para modo bimonetario.

---

## 2. Entidades

### Currency (BaseEntity — GLOBAL, sin family_id)
- CurrencyId (Guid)
- Code (ISO 4217: "ARS", "USD", "EUR")
- Name ("Peso Argentino", "Dolar Estadounidense")
- Symbol ("$", "US$", "€")
- DecimalPlaces (default 2)

### FamilyCurrency (TenantEntity)
- FamilyCurrencyId (Guid)
- FamilyId (Guid)
- CurrencyId (Guid) → Currency
- IsActive: bool

---

## 3. Endpoints

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/currencies | Listar todas las monedas globales | Todos |
| GET | /api/family-currencies | Listar monedas de la familia | Todos los miembros |
| POST | /api/family-currencies | Asociar moneda a la familia | FamilyAdmin |
| PUT | /api/family-currencies/{id} | Activar/desactivar | FamilyAdmin |
| DELETE | /api/family-currencies/{id} | Eliminar asociacion | FamilyAdmin |

---

## 4. Reglas de negocio

1. **152 monedas pre-seeded** (ISO 4217) via migracion EF Core
2. Currency es global — no tiene family_id
3. FamilyCurrency es la asociacion tenant-scoped
4. **No se puede desactivar** la moneda primaria ni la secundaria si bimonetario esta activo
5. **No se puede eliminar** si la usan cajas, bancos o movimientos activos
6. La familia siempre debe tener su moneda primaria (y secundaria si aplica) activa

### Modo bimonetario
- Configurable a nivel de familia (en family-settings)
- Cuando activo: cada movimiento registra DOS tipos de cambio
- El usuario elige en que moneda visualizar reportes
- exchange_rate: moneda del movimiento → primaria
- secondary_exchange_rate: moneda secundaria → primaria

### Control de acceso
- **FamilyAdmin**: CRUD completo sobre FamilyCurrency
- **Member**: solo lectura (ve las monedas de la familia para usarlas en movimientos)
- Ver documento transversal: `documentation/permissions-and-roles.md`
