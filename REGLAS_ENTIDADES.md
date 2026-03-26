# Reglas: Entidades (Cajas, Bancos, Tarjetas)

## Cajas de Dinero (CashBox)

### Crear

- Nombre: requerido, máx 100 caracteres, único por familia (case-insensitive)
- Moneda: requerida, debe ser FamilyCurrency activa
- Saldo inicial: opcional, default 0, decimales 2
- Estado: default `isActive = true`

### Editar

**Campos que PUEDEN cambiar:**
- Nombre
- Saldo (sin restricción en MVP)
- Estado (isActive)

**Campos que NO pueden cambiar:**
- Moneda — Bloqueado si caja tiene movimientos, transferencias u operaciones asociadas

### Eliminar

- Soft delete: `DeletedAt = NOW()`
- Validaciones previas: Ninguna en MVP

### En Movimientos

- Filtradas por moneda del movimiento
- Solo mostrarse si `isActive = true`
- Validar que la moneda de la caja coincida con `movement.currencyCode`

---

## Cuentas Bancarias (BankAccount)

### Crear

- Nombre: requerido, máx 100 caracteres, único por familia (case-insensitive)
- Moneda: requerida, debe ser FamilyCurrency activa
- Saldo inicial: opcional, default 0, decimales 2
- Número de cuenta: opcional, máx 30 caracteres
- CBU: opcional, máx 30 caracteres
- Alias: opcional, máx 50 caracteres
- Estado: default `isActive = true`

### Editar

**Campos que PUEDEN cambiar:**
- Nombre
- Saldo (sin restricción en MVP)
- Número de cuenta
- CBU
- Alias
- Estado (isActive)

**Campos que NO pueden cambiar:**
- Moneda — Bloqueado si banco tiene movimientos, consolidaciones u operaciones asociadas

### Eliminar

- Soft delete: `DeletedAt = NOW()`
- Validaciones previas: Ninguna en MVP

### En Movimientos

- Filtradas por moneda del movimiento
- Solo mostrarse si `isActive = true`
- Validar que la moneda del banco coincida con `movement.currencyCode`

---

## Tarjetas de Crédito (CreditCard)

### Crear

- Nombre: requerido, máx 100 caracteres, único por familia (case-insensitive)
- Moneda: requerida, debe ser FamilyCurrency activa
- Titular principal: requerido (CreditCardMember con `isPrimary = true`)
- Estado: default `isActive = true`

### Editar

**Campos que PUEDEN cambiar:**
- Nombre
- Estado (isActive)
- Miembros (agregar/eliminar)

**Campos que NO pueden cambiar:**
- Moneda (MVP: bloqueado si tiene movimientos)

### Miembros (CreditCardMember)

**Crear:**
- Nombre: requerido, máx 100 caracteres
- Últimos 4 dígitos: opcional, máx 4 caracteres, solo dígitos
- Primario: Boolean, máximo 1 por tarjeta
- Estado: default `isActive = true`

**Editar:**
- Nombre
- Últimos 4 dígitos
- Estado (isActive)

**Eliminar:**
- Soft delete

### En Movimientos

- Filtradas por moneda del movimiento
- Solo mostrarse si `isActive = true`
- Mostrar: nombre tarjeta
- Si hay miembros, permitir seleccionar miembro (sin especificar es opcional en MVP)
- Requerido si movimiento tiene cuotas (CC payment)

---

## Centros de Costo (CostCenter)

### Crear

- Nombre: requerido, máx 100 caracteres, único por familia (case-insensitive)
- Estado: default `isActive = true`

### Editar

- Nombre
- Estado (isActive)

### Eliminar

- Soft delete

### En Movimientos

- Solo mostrar si `isActive = true`
- Opcional en movimiento (puede ser null)
- Si no seleccionado: mostrar placeholder "Sin centro de costo" en color `text-muted-foreground`

---

## Regla General: Inactivos no Seleccionables

**Al Crear/Editar movimiento:**
- En dropdowns/comboboxes: NO mostrar entidades con `isActive = false`
- Excepción: Si el movimiento actual usa una entidad inactiva, mostrarla pero deshabilitada

**UI:**
- Si no hay opciones activas: mensaje "No hay [cajas/bancos/tarjetas] en [moneda]"

---

## Regla General: Inactivos en Listados

**Tablas de configuración (CashBoxes, BankAccounts, CreditCards):**
- Mostrar todos los registros (activos e inactivos)
- Usar badge de estado: "Activa" (verde) o "Inactiva" (gris)
