# Tarjetas de Credito – Modelo de Datos
*Version 2.0 – Marzo 2026*

---

## 1. Tablas existentes a modificar

### 1.1 movement_payments (agregar campos)

| Campo nuevo | Tipo | Descripcion |
|---|---|---|
| bonification_type | varchar(20) nullable | "percentage" o "fixed_amount" |
| bonification_value | decimal(18,2) nullable | Valor ingresado (% o importe) |
| bonification_amount | decimal(18,2) nullable | Monto calculado de la bonificacion |
| net_amount | decimal(18,2) nullable | amount - bonification_amount (deuda real) |

**Notas:**
- `amount` (ya existente) = monto bruto
- `net_amount` = monto neto = deuda real con la tarjeta
- Si no hay bonificacion: net_amount = amount
- Validaciones: si percentage → value > 0 y <= 100. Si fixed_amount → value > 0.

---

## 2. Tablas

### 2.1 credit_card_installments

Cuota individual de una compra con tarjeta.

| Campo | Tipo | Nullable | Descripcion |
|---|---|---|---|
| family_id | uuid | NO | FK → families |
| credit_card_installment_id | uuid | NO | PK (con family_id) |
| movement_payment_id | uuid | NO | FK → movement_payments |
| installment_number | int | NO | Numero de cuota (1, 2, 3...) |
| projected_amount | decimal(18,2) | NO | Monto bruto / total cuotas |
| bonification_applied | decimal(18,2) | NO | Cuanto de la bonif absorbe esta cuota (0 si no hay) |
| effective_amount | decimal(18,2) | NO | projected - bonification_applied |
| actual_amount | decimal(18,2) | SI | Monto real confirmado por el usuario |
| estimated_date | date | NO | Fecha estimada en que cae la cuota |
| statement_period_id | uuid | SI | FK → statement_periods (null = no asignada) |
| audit fields | | | created_at/by, modified_at/by, deleted_at/by |

**PK:** (family_id, credit_card_installment_id)

**Indices:**
- ix_cc_installments_payment: (family_id, movement_payment_id)
- ix_cc_installments_period: (family_id, statement_period_id) WHERE statement_period_id IS NOT NULL

**Logica de monto para el resumen:**
```
monto_para_resumen = actual_amount ?? effective_amount
```

**Distribucion de bonificacion al crear la compra:**
```
bonificacion_restante = bonification_amount (del movement_payment)
Para cada cuota en orden (1, 2, 3...):
  bonification_applied = MIN(projected_amount, bonificacion_restante)
  effective_amount = projected_amount - bonification_applied
  bonificacion_restante -= bonification_applied
  Si bonificacion_restante == 0: las siguientes cuotas tienen bonification_applied = 0
```

---

### 2.2 statement_periods

Periodo/resumen de tarjeta de credito.

| Campo | Tipo | Nullable | Descripcion |
|---|---|---|---|
| family_id | uuid | NO | FK → families |
| statement_period_id | uuid | NO | PK (con family_id) |
| credit_card_id | uuid | NO | FK → credit_cards |
| period_start | date | NO | Inicio del periodo |
| period_end | date | NO | Cierre del periodo |
| due_date | date | NO | Vencimiento del pago |
| payment_status | varchar(20) | NO | Unpaid, PartiallyPaid, FullyPaid |
| previous_balance | decimal(18,2) | NO | Saldo anterior (0 si es el primero) |
| installments_total | decimal(18,2) | NO | Suma de monto_para_resumen de cuotas incluidas |
| charges_total | decimal(18,2) | NO | Suma de cargos |
| bonifications_total | decimal(18,2) | NO | Suma de bonificaciones del banco |
| statement_total | decimal(18,2) | NO | previous_balance + installments - bonifications + charges |
| payments_total | decimal(18,2) | NO | Suma de pagos realizados |
| pending_balance | decimal(18,2) | NO | statement_total - payments_total |
| closed_at | timestamp | SI | null = Abierto, con valor = Cerrado |
| closed_by | uuid | SI | Quien cerro |
| audit fields | | | created_at/by, modified_at/by, deleted_at/by |

**PK:** (family_id, statement_period_id)

**Estados del periodo (dos dimensiones independientes):**

| closed_at | payment_status | Significado | Acciones |
|---|---|---|---|
| null | Unpaid | Abierto, sin pagos | Editar todo |
| null | PartiallyPaid | Abierto, con pagos anticipados | Editar todo |
| timestamp | Unpaid | Cerrado, sin pagos | Registrar pagos |
| timestamp | PartiallyPaid | Cerrado, pagos parciales | Registrar mas pagos |
| timestamp | FullyPaid | Cerrado, pagado total | Solo lectura |

**Indices:**
- ix_statement_periods_card: (family_id, credit_card_id)
- ix_statement_periods_one_open: UNIQUE (family_id, credit_card_id) WHERE closed_at IS NULL AND deleted_at IS NULL

**Calculo de totales (al cerrar):**
```
installments_total = SUM(actual_amount ?? effective_amount) de cuotas con statement_period_id = este periodo
charges_total = SUM(amount) de statement_line_items tipo 'charge'
bonifications_total = SUM(amount) de statement_line_items tipo 'bonification'
statement_total = previous_balance + installments_total + charges_total - bonifications_total
payments_total = SUM(amount) de credit_card_payments asociados
pending_balance = statement_total - payments_total
```

---

### 2.3 statement_line_items

Cargos y bonificaciones del resumen (unificados en una tabla).

| Campo | Tipo | Nullable | Descripcion |
|---|---|---|---|
| family_id | uuid | NO | FK → families |
| statement_line_item_id | uuid | NO | PK (con family_id) |
| statement_period_id | uuid | NO | FK → statement_periods |
| line_type | varchar(20) | NO | "charge" o "bonification" |
| description | varchar(200) | NO | Descripcion libre |
| amount | decimal(18,2) | NO | Siempre positivo. UI muestra negativo si bonificacion. |
| audit fields | | | created_at/by, modified_at/by, deleted_at/by |

**PK:** (family_id, statement_line_item_id)

**Indice:**
- ix_statement_line_items_period: (family_id, statement_period_id)

**Interpretacion para analisis:**
- line_type = 'charge' → se interpreta como **egreso**
- line_type = 'bonification' → se interpreta como **ingreso**

---

### 2.4 credit_card_payments (tabla unificada)

Pagos de tarjeta de credito. Puede estar asociado a un periodo (statement_period_id) o ser un pago huerfano (sin periodo).

| Campo | Tipo | Nullable | Descripcion |
|---|---|---|---|
| family_id | uuid | NO | FK → families |
| credit_card_payment_id | uuid | NO | PK (con family_id) |
| credit_card_id | uuid | NO | FK → credit_cards |
| payment_date | date | NO | Fecha del pago |
| amount | decimal(18,2) | NO | Importe del pago |
| description | varchar(200) | SI | Descripcion libre |
| cash_box_id | uuid | SI | FK → cash_boxes (uno de los dos) |
| bank_account_id | uuid | SI | FK → bank_accounts (uno de los dos) |
| is_total_payment | bool | NO | Marca explicita: pago total o parcial |
| statement_period_id | uuid | SI | FK → statement_periods (null = no asociado) |
| primary_exchange_rate | decimal(18,6) | NO | TC moneda tarjeta → primaria (default 1) |
| secondary_exchange_rate | decimal(18,6) | NO | TC moneda tarjeta → secundaria (default 1) |
| amount_in_primary | decimal(18,2) | NO | amount × primary_exchange_rate |
| amount_in_secondary | decimal(18,2) | NO | amount × secondary_exchange_rate |
| audit fields | | | created_at/by, modified_at/by, deleted_at/by |

**PK:** (family_id, credit_card_payment_id)

**Indices:**
- ix_credit_card_payments_card: (family_id, credit_card_id)
- ix_credit_card_payments_period: (family_id, statement_period_id) WHERE statement_period_id IS NOT NULL

**Constraint:** Exactamente un origen (cash_box_id XOR bank_account_id)

**Reglas de negocio:**
- La moneda de la caja/banco origen debe coincidir con la moneda de la tarjeta
- `statement_period_id = null` → pago no asociado a ningun periodo (huerfano)
- `is_total_payment = true` requiere `statement_period_id` no null + periodo cerrado
- Al crear un periodo, se muestran los pagos huerfanos para que el usuario asocie
- Al cerrar un periodo, se ejecuta el prorrateo sobre los pagos asociados
- Al registrar: balance de caja/banco -= amount
- Al eliminar: balance de caja/banco += amount

---

### 2.5 statement_payment_allocations

Prorrateo: distribucion de cada pago entre las lineas del resumen.

| Campo | Tipo | Nullable | Descripcion |
|---|---|---|---|
| family_id | uuid | NO | FK → families |
| allocation_id | uuid | NO | PK (con family_id) |
| credit_card_payment_id | uuid | NO | FK → credit_card_payments |
| credit_card_installment_id | uuid | SI | FK → cuota (uno de los dos) |
| statement_line_item_id | uuid | SI | FK → cargo/bonificacion (uno de los dos) |
| amount_card_currency | decimal(18,2) | NO | Importe prorrateado en moneda tarjeta |
| amount_in_primary | decimal(18,2) | NO | Importe en moneda primaria |
| amount_in_secondary | decimal(18,2) | NO | Importe en moneda secundaria |
| primary_exchange_rate | decimal(18,6) | NO | TC usado (del pago) |
| secondary_exchange_rate | decimal(18,6) | NO | TC usado (del pago) |
| audit fields | | | created_at/by, modified_at/by, deleted_at/by |

**PK:** (family_id, allocation_id)

**Indices:**
- ix_allocations_payment: (family_id, credit_card_payment_id)
- ix_allocations_installment: (family_id, credit_card_installment_id) WHERE NOT NULL
- ix_allocations_line_item: (family_id, statement_line_item_id) WHERE NOT NULL

**Constraint:** Exactamente una referencia (installment XOR line_item):
```sql
CHECK (
  (credit_card_installment_id IS NOT NULL AND statement_line_item_id IS NULL) OR
  (credit_card_installment_id IS NULL AND statement_line_item_id IS NOT NULL)
)
```

**Validacion:** La suma de amount_card_currency de todas las allocations para una cuota/linea no puede superar el monto de esa cuota/linea.

---

## 3. Diagrama de relaciones

```
credit_cards
  │
  ├──< credit_card_members
  │
  ├──< credit_card_payments (pagos, con statement_period_id opcional)
  │     │
  │     └──< statement_payment_allocations (prorrateo)
  │            ├──>? credit_card_installments
  │            └──>? statement_line_items
  │
  └──< statement_periods
        │
        ├──< credit_card_installments (via statement_period_id)
        │
        ├──< statement_line_items (cargos + bonificaciones banco)
        │
        └──< credit_card_payments (via statement_period_id, nullable)

movement_payments ──< credit_card_installments (via movement_payment_id)
```

---

## 4. Enums

```csharp
public enum PaymentStatus
{
    Unpaid = 0,
    PartiallyPaid = 1,
    FullyPaid = 2
}

public enum BonificationType  // para MovementPayment
{
    Percentage = 0,
    FixedAmount = 1
}

public enum StatementLineType
{
    Charge = 0,      // cargo (egreso)
    Bonification = 1  // bonificacion banco (ingreso)
}
```

---

## 5. Reglas de integridad

### 5.1 Periodos
- Solo un periodo abierto (closed_at IS NULL) por tarjeta (unique partial index)
- `previous_balance` del periodo N = `pending_balance` del periodo N-1 (o 0 si es el primero)
- Totales se recalculan al cerrar y al registrar/eliminar pagos

### 5.2 Cuotas
- `statement_period_id = null` → cuota futura, no asignada a ningun periodo
- `statement_period_id != null` → cuota incluida en ese periodo
- `bonification_applied` se calcula una vez al crear la compra, no cambia
- `actual_amount` solo editable si el periodo esta abierto (closed_at IS NULL)
- Monto para resumen: `actual_amount ?? effective_amount`

### 5.3 Pagos (credit_card_payments)
- Exactamente un origen: cash_box_id XOR bank_account_id
- `statement_period_id` es nullable: null = pago no asociado a ningun periodo
- `is_total_payment = true` requiere statement_period_id no null + periodo cerrado
- Al registrar pago con periodo cerrado → se genera prorrateo automaticamente
- Al cerrar periodo con pagos existentes → se genera prorrateo automaticamente

### 5.4 Allocations (prorrateo)
- Se crean automaticamente, nunca manualmente
- Metodo: pro-rata (proporcional al monto de cada linea)
- Ultima linea absorbe diferencia de redondeo
- Se eliminan completamente al reabrir periodo (y se recalculan al volver a cerrar)
- Se eliminan y recalculan al eliminar un pago

### 5.5 Proteccion de movimientos
- Si una cuota tiene `statement_period_id` apuntando a un periodo cerrado (closed_at != null):
  el movimiento padre y su movement_payment NO se pueden editar ni eliminar
- Para corregir: reabrir periodo → desasignar cuota (statement_period_id = null) → recien ahi editar/eliminar

### 5.6 Soft delete
- Todas las tablas tienen deleted_at/deleted_by
- RLS aplica solo por family_id (no por deleted_at)
- Global query filters de EF Core filtran por deleted_at IS NULL

---

## 6. Resumen de tablas

| Tabla | Tipo | Descripcion |
|---|---|---|
| credit_cards | Existente | Sin cambios |
| credit_card_members | Existente | Sin cambios |
| movement_payments | Modificada | +4 campos bonificacion |
| credit_card_payments | **Unificada** | Pagos TC (con statement_period_id opcional) |
| credit_card_installments | **Nueva** | Cuotas individuales |
| statement_periods | **Modificada** | Periodos/resumenes (payment_status + closed_at) |
| statement_line_items | **Nueva** | Cargos + bonificaciones banco |
| statement_payment_allocations | **Modificada** | Prorrateo bimonetario (FK → credit_card_payments) |
