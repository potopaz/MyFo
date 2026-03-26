# Movimientos – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Nucleo del sistema. Registra ingresos y egresos con multiples formas de pago, soporte bimonetario, y clasificacion por categoria/subcategoria/centro de costo.

---

## 2. Entidades

### Movement (TenantEntity)
- MovementId (Guid), FamilyId (Guid)
- Date (DateOnly)
- MovementType: Income | Expense (enum)
- Amount (decimal, > 0)
- CurrencyCode (ISO 4217)
- PrimaryExchangeRate, SecondaryExchangeRate (decimal, > 0, default 1)
- AmountInPrimary, AmountInSecondary (calculados = Amount x rate)
- Description (nullable)
- SubcategoryId (Guid) → Subcategory
- AccountingType (nullable) — sobreescribe sugerencia de subcategoria
- IsOrdinary (nullable bool) — sobreescribe sugerencia
- CostCenterId (nullable) → CostCenter
- Source (default "Web")
- RowVersion (concurrencia optimista)

### MovementPayment (TenantEntity)
- MovementPaymentId (Guid), FamilyId (Guid)
- MovementId (Guid) → Movement
- PaymentMethodType: CashBox | BankAccount | CreditCard (enum)
- Amount (decimal, > 0)
- CashBoxId (nullable) — si pago con caja
- BankAccountId (nullable) — si pago con banco
- CreditCardId (nullable) — si pago con tarjeta
- CreditCardMemberId (nullable) — obligatorio si tarjeta
- Installments (nullable int, 1-48) — solo tarjeta

---

## 3. Endpoints

| Metodo | Ruta | Descripcion |
|---|---|---|
| GET | /api/movements | Listar con filtros (fecha, tipo, subcategoria) |
| GET | /api/movements/{id} | Detalle |
| POST | /api/movements | Crear |
| PUT | /api/movements/{id} | Actualizar |
| DELETE | /api/movements/{id} | Eliminar |

---

## 4. Cadena de validaciones (orden critico)

1. **FamilyId**: usuario debe tener familia seleccionada
2. **Amount**: > 0
3. **CurrencyCode**: debe ser FamilyCurrency activa
4. **Subcategory**: debe existir, estar activa, pertenecer a la familia
5. **MovementType**:
   - Si SubcategoryType = Both → usuario elige Income o Expense
   - Si SubcategoryType = Income → auto Income
   - Si SubcategoryType = Expense → auto Expense
6. **CostCenter**: si se provee, debe existir y estar activo
7. **Exchange rates**: ambos > 0
8. **Payments**: al menos uno requerido
9. **Validacion por payment**:
   - Amount > 0
   - **CashBox**: debe existir, activa, moneda = moneda movimiento
   - **BankAccount**: debe existir, activa, moneda = moneda movimiento
   - **CreditCard**: debe existir, activa, moneda = moneda movimiento, miembro debe pertenecer a la tarjeta, installments 1-48
10. **Suma de payments** = Amount del movimiento
11. **Calculo**: AmountInPrimary = Amount x PrimaryExchangeRate, idem secundaria

---

## 5. Efectos secundarios

| Forma de pago | Efecto al crear | Efecto al eliminar |
|---|---|---|
| CashBox | Balance += Amount (ingreso) o -= Amount (egreso) | Reversa |
| BankAccount | Balance += Amount (ingreso) o -= Amount (egreso) | Reversa |
| CreditCard | Sin efecto en balance (MVP) | Sin efecto |

### Compras con tarjeta
Cuando un payment es con CreditCard, un Domain Event crea:
- CreditCardPurchase (registro de la compra)
- N x CreditCardInstallment (una por cuota, con fecha estimada y monto proyectado)

---

## 6. Reglas de exchange rate

- Si NO hay modo bimonetario: ambos rates = 1 (ocultos en UI)
- Si bimonetario + moneda = primaria: TC primario = 1 (bloqueado), usuario ingresa TC secundario
- Si bimonetario + moneda = secundaria: TC secundario = 1 (bloqueado), usuario ingresa TC primario
- Si bimonetario + moneda = otra: usuario ingresa ambos TC

---

## 7. Permisos para operaciones

Ver documento transversal: `documentation/permissions-and-roles.md` para el modelo completo.

### 7.1 Crear movimiento
- **Cajas**: solo se muestran las que el usuario tiene permiso Operate (CashBoxPermission)
- **Bancos**: se muestran todos (bancos no tienen permisos)
- **Tarjetas**: se muestran todas (tarjetas no tienen permisos por usuario)
- El usuario necesita Operate en al menos una caja para usarla como forma de pago

### 7.2 Ver movimiento
- El usuario ve un movimiento si tiene al menos **View** en alguna de las cajas de sus formas de pago
- Si el movimiento solo usa bancos/tarjetas (sin cajas): todos los miembros lo ven

### 7.3 Editar movimiento

**Datos editables siempre** (si puede ver el movimiento):
- Fecha, descripcion, subcategoria, centro de costo, caracter (ordinario/extraordinario), tipo contable

**Datos editables solo con Operate en TODAS las cajas de las formas de pago:**
- Moneda del movimiento
- Tipo (Income/Expense)
- Monto total
- Formas de pago existentes (modificar monto, cambiar caja/banco)

**Agregar nueva forma de pago:**
- Requiere Operate en la caja de la nueva forma de pago (si es caja)

**Eliminar forma de pago existente:**
- Requiere Operate en la caja de esa forma de pago (si es caja)

**Regla de bloqueo de moneda/tipo:**
- Si hay AL MENOS UNA forma de pago con caja donde el usuario NO tiene Operate → moneda y tipo de movimiento (Income/Expense) quedan **bloqueados** (no editables)

### 7.4 Eliminar movimiento
- Requiere Operate en **TODAS** las cajas de todas las formas de pago del movimiento
- Si alguna caja no tiene Operate → no se puede eliminar
- Si todas las formas de pago son bancos/tarjetas (sin cajas) → cualquier miembro puede eliminar

---

## 8. Reglas adicionales de edicion y borrado

1. Soft delete (nunca borrado fisico)
2. Concurrencia optimista via RowVersion
3. Cualquier miembro puede editar un movimiento (no solo el creador), respetando las reglas de permisos de la seccion 7
4. v1.1: no se pueden modificar importes si la caja esta cerrada (arqueo)
5. v1.1: nada modificable si la fecha cae dentro de un cierre de ejercicio
6. Si un movimiento tiene pago con tarjeta cuyas cuotas ya estan en un resumen cerrado/pagado: **no se puede editar ni eliminar** (ver credit-cards/functional-spec.md seccion 8.6)
