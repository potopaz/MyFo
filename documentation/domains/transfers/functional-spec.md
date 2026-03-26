# Transferencias – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Movimiento de dinero entre activos propios (cajas y bancos). No es ingreso ni egreso — es redistribucion de disponibilidad. Incluye auto-confirmacion condicional basada en permisos.

---

## 2. Entidades

### Transfer (TenantEntity)
- TransferId (Guid), FamilyId (Guid)
- Date (DateOnly)
- FromCashBoxId (nullable) → CashBox
- FromBankAccountId (nullable) → BankAccount
- ToCashBoxId (nullable) → CashBox
- ToBankAccountId (nullable) → BankAccount
- Amount (decimal) — monto origen
- AmountTo (decimal) — monto destino (puede diferir si cross-currency)
- ExchangeRate (decimal, default 1) — TC basico
- FromPrimaryExchangeRate, FromSecondaryExchangeRate
- ToPrimaryExchangeRate, ToSecondaryExchangeRate
- AmountInPrimary, AmountInSecondary (calculados desde origen)
- AmountToInPrimary, AmountToInSecondary (calculados desde destino)
- Description (nullable)
- Source (default "Web")
- RowVersion (concurrencia optimista)
- Status: PendingConfirmation | Confirmed | Rejected (enum)
- IsAutoConfirmed: bool
- RejectionComment (nullable)

---

## 3. Endpoints

| Metodo | Ruta | Descripcion |
|---|---|---|
| GET | /api/transfers | Listar con filtros (fecha, estado) |
| GET | /api/transfers/{id} | Detalle |
| POST | /api/transfers | Crear |
| PUT | /api/transfers/{id} | Actualizar |
| DELETE | /api/transfers/{id} | Eliminar |
| POST | /api/transfers/{id}/confirm | Confirmar |
| POST | /api/transfers/{id}/reject | Rechazar |

---

## 4. Reglas de negocio

### Origen y destino
1. **Exactamente un origen**: FromCashBoxId O FromBankAccountId (no ambos, no ninguno)
2. **Exactamente un destino**: ToCashBoxId O ToBankAccountId (no ambos, no ninguno)
3. **Distintos**: origen ≠ destino (no transferir a si mismo)
4. **Nunca tarjeta**: no se puede usar tarjeta de credito como origen ni destino

### Cross-currency
- Si origen y destino tienen distinta moneda: exchange rate requerido
- Se registran 4 exchange rates para tracking bimonetario
- AmountTo = Amount x ExchangeRate (o ingresado por usuario)

### Permisos y confirmacion

Ver documento transversal: `documentation/permissions-and-roles.md` para el modelo completo.

**Crear transferencia:**
- **Origen caja**: requiere permiso Operate en la caja (CashBoxPermission)
- **Origen banco**: sin restriccion (bancos no tienen permisos)
- **Destino**: se muestran todas las cajas y bancos, independientemente de permisos
- FamilyAdmin NO tiene acceso implicito — necesita permiso Operate explicito en cajas

**Auto-confirmacion:**
- Si el destino es **banco** → auto-confirma siempre (bancos no tienen permisos)
- Si el destino es **caja** y el usuario tiene Operate en esa caja → auto-confirma (Status=Confirmed, IsAutoConfirmed=true)
- Si el destino es **caja** y el usuario NO tiene Operate → queda PendingConfirmation

**Confirmar transferencia pendiente:**
- Requiere Operate en la caja destino
- Solo transferencias en estado PendingConfirmation

**Rechazar transferencia pendiente:**
- Requiere Operate en la caja destino (o ser el creador)

### Efecto en balances

**Transferencia confirmada:**
- Origen: Balance -= Amount
- Destino: Balance += AmountTo

**Transferencia pendiente:**
- Origen: Balance -= Amount (se descuenta al crear)
- Destino: sin efecto hasta confirmacion

**Al confirmar:**
- Destino: Balance += AmountTo

**Al rechazar:**
- Origen: Balance += Amount (se devuelve)

### Eliminacion
- Reversa los balances segun estado
- Si confirmada: reversa ambos lados
- Si pendiente: reversa solo origen
- Soft delete

### Edicion de transferencias auto-confirmadas
- Pendiente definir reglas exactas de recalculo de saldos al editar
- Documentado en: `project_pending_transfer_edit_autoconfirmed.md`
