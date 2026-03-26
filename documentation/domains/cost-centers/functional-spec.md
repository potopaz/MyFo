# Centros de Costo – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Centros de costo personalizables por familia para clasificar movimientos. MVP: un centro de costo por movimiento. v1.1: multiples con porcentajes.

---

## 2. Entidades

### CostCenter (TenantEntity)
- CostCenterId (Guid), FamilyId (Guid)
- Name (max 100 chars)
- IsActive: bool

---

## 3. Endpoints

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/cost-centers | Listar centros de costo | Todos los miembros |
| POST | /api/cost-centers | Crear | FamilyAdmin |
| PUT | /api/cost-centers/{id} | Actualizar | FamilyAdmin |
| DELETE | /api/cost-centers/{id} | Eliminar | FamilyAdmin |

---

## 4. Reglas de negocio

1. **Nombre unico** por familia
2. **IsActive**: solo activos aparecen en combos
3. **Soft delete**: movimientos que referencian un CC eliminado mantienen la referencia (FK nullable)
4. **Subcategorias pueden sugerir** un centro de costo por defecto (SuggestedCostCenterId)
5. **v1.1**: asignacion multi-CC con porcentajes (suma = 100%)

### Control de acceso
- **FamilyAdmin**: CRUD completo sobre centros de costo
- **Member**: solo lectura (ve todos los CC para usarlos en movimientos)
- Ver documento transversal: `documentation/permissions-and-roles.md`
