# Reglas: UI/UX

## Campos Numéricos (Importe, Cotizaciones)

### Input Mode
- `inputMode="decimal"` para permitir teclado numérico en mobile

### Validación Frontend (onChange)
- **Importe** (2 decimales): Regex `/^\d*\.?\d{0,2}$/`
- **Cotización** (6 decimales): Regex `/^\d*\.?\d{0,6}$/`
- Aceptar tanto `.` como `,` como separador decimal (normalizar internamente a `.`)
- Rechazar caracteres inválidos silenciosamente (no insertar en el input)

### Conversión
- Frontend → Backend: `parseFloat(normalizeDecimal(value))`
- Backend → Frontend: `String(number)`
- `normalizeDecimal()`: reemplaza `,` con `.` antes de cualquier cálculo

### Formato de Display
- En tablas: `toLocaleString('es-AR', { minimumFractionDigits: 2 })` → "1.234,50"
- En inputs: sin formato especial, usuario ve lo que escribió

---

## Select con Placeholder Claro

### Comportamiento
- Si no hay valor seleccionado: mostrar en color `text-muted-foreground` (gris claro)
- Si hay valor: color normal (foreground)
- Placeholder: "Sin [cosa]", "Sin especificar", etc.

### Implementación
```tsx
<SelectValue className={!value ? 'text-muted-foreground' : ''}>
  {value ? labels[value] : 'Sin tipo contable'}
</SelectValue>
```

### Ubicaciones Aplicadas
- Tipo contable ("Sin tipo contable")
- Carácter ("Sin definir")
- Centro de costo ("Sin centro de costo")
- Miembro de tarjeta ("Sin especificar")

---

## Entrada/Salida de Movimiento

### Campo "Tipo"
- **Ubicación**: Después de Fecha en el formulario
- **Opciones**: "Entrada" (Income) | "Salida" (Expense)
- **Default**: "Salida" (Expense)
- **Auto-fill**: Si subcategoría sugiere Income/Expense, actualizar automáticamente (pero usuario puede sobrescribir)

### Valores Internos
- "Entrada" → `movementType = "Income"`
- "Salida" → `movementType = "Expense"`

---

## Selects de Entidades (Caja, Banco, Tarjeta)

### Mostrar Nombre, No ID

```tsx
<SelectValue>
  {payment.cashBoxId
    ? filteredCashBoxes.find((c) => c.value === payment.cashBoxId)?.label
    : 'Seleccionar caja'}
</SelectValue>
```

- **Nunca** mostrar UUID o valor interno
- Siempre mostrar `.label` que contiene el nombre de la entidad
- Si no seleccionado: placeholder

### Filtrado por Moneda
- Al cambiar moneda del movimiento, refiltrar listas de entidades
- Si cambia moneda y entidad seleccionada no existe en nueva moneda, limpiar selección
- Mostrar mensaje: "No hay cajas en [MONEDA]" si lista vacía

---

## Importe Total del Movimiento

### Frontend
- **Campo**: De solo lectura (`disabled`, `bg-muted`)
- **Cálculo**: `sum(payments[].amount)` actualizado en tiempo real
- **Help text**: "Se calcula automáticamente desde las formas de pago"
- Cuando usuario cambia un payment, importe total se recalcula instantly

### Backend
- Recibe importe ya calculado desde frontend
- Valida: `sum(payments) == movement.amount` (exacto, tolerancia ±0.009)

---

## Formas de Pago (Payments)

### Layout
- Cada pago en su propia Card
- Grid 3 columnas: Método | Entidad | Importe
- Fila extra para CC: Miembro | Cuotas

### Agregar/Quitar
- Botón "Agregar" en header de sección (reutilizable)
- Botón "X" en cada pago si hay >1 pago (no se puede tener 0 pagos)

### Validaciones Frontend
- Cada pago debe tener `amount > 0`
- Cada pago debe tener entidad seleccionada (según tipo)
- CC: cuotas entre 1-48
- No permitir envío si suma de pagos ≠ importe total

---

## Subcategoría con Auto-fill

### Comportamiento
- Al seleccionar subcategoría, auto-llenar:
  - `movementType` (si subcategoría sugiere Income/Expense)
  - `accountingType` (tipo contable sugerido)
  - `costCenterId` (centro de costo sugerido)
  - `isOrdinary` (carácter sugerido)
- Usuario puede sobrescribir cualquier campo después

### Valores Nulos
- Si subcategoría no tiene sugerencia para un campo → no cambiar formulario (dejar como estaba)

---

## Ancho del Drawer/Modal

- Desktop: `w-[90vw] sm:max-w-5xl` (50% más ancho que versión anterior)
- Mobile: 90% del viewport
- Permite layout de 3-4 columnas sin overflow

---

## Validación de Formulario (Submit)

### Campos Requeridos
- Fecha
- Subcategoría
- Moneda
- Tipo (Entrada/Salida) — **solo si subcategoría es "Both"**
- Mínimo 1 forma de pago

### Validaciones Que Lanzan Toast Error
```
❌ "La fecha es requerida"
❌ "El importe debe ser mayor a cero"
❌ "La moneda es requerida"
❌ "La subcategoría es requerida"
❌ "El tipo de movimiento es requerido"
❌ "Agregue al menos una forma de pago"
❌ "Cada forma de pago debe tener importe > 0"
❌ "Seleccione una caja"
❌ "Seleccione un banco"
❌ "Seleccione una tarjeta"
❌ "La suma de pagos (X) no coincide con el importe (Y)"
```

---

## Colores y Badges

### Movimiento (en tabla)
- Income: Badge "Ingreso" (variante `default` = azul)
- Expense: Badge "Egreso" (variante `secondary` = gris)

### Estado de Entidades
- Activa: Badge "Activa" (variante `default`)
- Inactiva: Badge "Inactiva" (variante `secondary`)
