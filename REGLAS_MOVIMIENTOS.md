# Reglas: Movimientos

## Crear Movimiento

### Validaciones Backend (CreateMovementCommandHandler)

1. **FamilyId no nulo** — Usuario debe tener familia seleccionada
2. **Importe > 0** — Validar antes de procesar
3. **Moneda debe ser FamilyCurrency activa** — Cargar desde DB, verificar `isActive = true`
4. **Subcategoría debe existir y no estar borrada** — Validar existencia
5. **MovementType compatible con SubcategoryType**:
   - Si SubcategoryType = Income → MovementType = Income (forzado)
   - Si SubcategoryType = Expense → MovementType = Expense (forzado)
   - Si SubcategoryType = Both → Usuario elige (Income o Expense)
6. **CostCenterId (opcional)**:
   - Si informado: debe existir y estar activo (`isActive = true`)
7. **ExchangeRate > 0** — Siempre requerido (default 1)
8. **SecondaryExchangeRate > 0** — Solo si `family.IsBimonetary = true`
9. **Payments no vacío** — Mínimo 1 forma de pago
10. **Cada payment**:
    - Amount > 0
    - Entidad (caja/banco/tarjeta) existe y está activa
    - Moneda de la entidad coincide con `movement.currencyCode`
    - Si CreditCard: `installments >= 1 && installments <= 48`
    - Si CreditCard: `creditCardMemberId` (si informado) pertenece a la tarjeta
11. **Suma de payments == importe del movimiento** — Validación exacta (tolerancia ±0.009)
12. **Calcular AmountInPrimary** = Amount × ExchangeRate
13. **Calcular AmountInSecondary** = Amount × SecondaryExchangeRate (o null si no bimonetario)

### Side Effects (Actualizar Balances)

- **CashBox**: `balance += sign * payment.amount` (sign: +1 si Income, -1 si Expense)
- **BankAccount**: `balance += sign * payment.amount`
- **CreditCard**: Sin cambio en MVP (no hay tabla de saldos de CC)

### Valores por Defecto

- `exchangeRate = 1`
- `secondaryExchangeRate = 1`
- `description = null` (opcional)
- `accountingType = null` (opcional)
- `isOrdinary = null` (opcional)
- `costCenterId = null` (opcional)

---

## Editar Movimiento

### Proceso

1. Cargar movimiento actual con sus pagos
2. Reversar balances antiguos: `balance -= oldSign * oldPayment.amount`
3. Validar nuevos datos (mismo proceso que Create)
4. Eliminar pagos antiguos
5. Agregar pagos nuevos
6. Actualizar movimiento
7. Aplicar nuevos balances: `balance += newSign * newPayment.amount`

### Cambios Permitidos

- Fecha
- Tipo (Entrada/Salida)
- Subcategoría
- Moneda
- Cotizaciones
- Descripción
- Clasificación (tipo contable, carácter, centro de costo)
- Formas de pago (método, entidad, importe, cuotas si CC)

### Restricciones

Ninguna por ahora. Todos los campos pueden cambiar.

---

## Eliminar Movimiento

### Proceso

1. Cargar movimiento con pagos
2. Reversar balances de todos los pagos
3. Soft delete: `movement.DeletedAt = NOW()`, `movement.DeletedBy = UserId`
4. Soft delete todos los pagos asociados

### Filtros en Listado

- Automático: global query filter excluye donde `deletedAt IS NOT NULL`
- Backend + Frontend: solo devuelve movimientos activos

---

## Tipos de Movimiento

| Tipo | Valor DB | Etiqueta UI |
|------|----------|-------------|
| Income | "Income" | "Entrada" |
| Expense | "Expense" | "Salida" |

---

## Métodos de Pago

| Tipo | Valor DB | Etiqueta UI |
|------|----------|-------------|
| CashBox | "CashBox" | "Caja" |
| BankAccount | "BankAccount" | "Banco" |
| CreditCard | "CreditCard" | "Tarjeta de crédito" |

---

## Cálculo del Importe Total

**Frontend:**
- El campo "Importe total" es **de solo lectura**
- Se calcula automáticamente como: `sum(payments[].amount)`
- Al cambiar cualquier pago, se recalcula instantáneamente
- Si suma de pagos = 0, campo vacío

**Backend:**
- Recibe el importe del frontend (ya calculado)
- Valida que `sum(payments) == movement.amount` (exacto, tolerancia ±0.009)
