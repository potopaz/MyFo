# Categorias y Subcategorias – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Clasificacion jerarquica de ingresos y egresos. Cada movimiento se asigna a una subcategoria, que pertenece a una categoria.

---

## 2. Entidades

### Category (TenantEntity)
- CategoryId (Guid), FamilyId (Guid)
- Name (max 100 chars)
- Icon (opcional)
- Subcategories (navegacion)

### Subcategory (TenantEntity)
- SubcategoryId (Guid), FamilyId (Guid)
- CategoryId (Guid) → Category
- Name (max 100 chars)
- SubcategoryType: Income | Expense | Both (enum)
- IsActive: bool
- SuggestedAccountingType: Asset | Liability | Income | Expense (nullable)
- SuggestedCostCenterId (nullable) → CostCenter
- IsOrdinary: bool (nullable) — ordinario vs extraordinario

---

## 3. Endpoints

| Metodo | Ruta | Descripcion | Acceso |
|---|---|---|---|
| GET | /api/categories | Listar con subcategorias | Todos los miembros |
| POST | /api/categories | Crear categoria (+ subcategorias opcionales) | FamilyAdmin |
| PUT | /api/categories/{id} | Actualizar categoria | FamilyAdmin |
| DELETE | /api/categories/{id} | Eliminar categoria | FamilyAdmin |
| POST | /api/subcategories | Crear subcategoria | FamilyAdmin |
| PUT | /api/subcategories/{id} | Actualizar subcategoria | FamilyAdmin |
| DELETE | /api/subcategories/{id} | Eliminar subcategoria | FamilyAdmin |

---

## 4. Reglas de negocio

### Categorias
1. **Nombre unico** por familia (valida incluso contra soft-deleted)
2. **Reactivacion**: si se recrea una categoria eliminada, se restaura en vez de crear nueva
3. **Eliminacion jerarquica**: eliminar categoria soft-deletes todas sus subcategorias

### Subcategorias
1. **SubcategoryType es fijo**: determina si aparece en combos de ingreso, egreso o ambos
2. **Sugerencias opcionales**: AccountingType, CostCenter, IsOrdinary son recomendaciones que el usuario puede sobreescribir al cargar un movimiento
3. **IsActive**: inactivas no aparecen en combos al crear. En edicion se muestra el valor actual.

### Control de acceso
- **FamilyAdmin**: CRUD completo sobre categorias y subcategorias
- **Member**: solo lectura (ve todas las categorias y subcategorias para usarlas en movimientos)
- Ver documento transversal: `documentation/permissions-and-roles.md`
