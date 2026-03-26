# Reglas: Cotizaciones (Exchange Rates)

## Modo Bimonetario

### Condición
- `family.IsBimonetary = true`
- Family tiene `primaryCurrencyCode` y `secondaryCurrencyCode`

### Sin Bimonetario
- Ocultar campos de cotización
- `exchangeRate = 1` (forzado)
- `secondaryExchangeRate = 1` (forzado)
- `amountInSecondary = null`

---

## Lógica de Cotizaciones por Moneda

### Caso 1: Moneda = Primaria
**Estado:**
- Mostrar campo de cotización primaria: **BLOQUEADO** (deshabilitado)
- Mostrar campo de cotización secundaria: **EDITABLE**

**Valores:**
- `exchangeRate = 1` (siempre)
- `secondaryExchangeRate = [usuario ingresa]`

**Cálculos:**
- `amountInPrimary = amount × 1 = amount`
- `amountInSecondary = amount × secondaryExchangeRate`

---

### Caso 2: Moneda = Secundaria
**Estado:**
- Mostrar campo de cotización primaria: **EDITABLE**
- Mostrar campo de cotización secundaria: **BLOQUEADO** (deshabilitado)

**Valores:**
- `exchangeRate = [usuario ingresa]`
- `secondaryExchangeRate = 1` (siempre)

**Cálculos:**
- `amountInPrimary = amount × exchangeRate`
- `amountInSecondary = amount × 1 = amount`

---

### Caso 3: Moneda = Otra (ni primaria ni secundaria)
**Estado:**
- Mostrar campo de cotización primaria: **EDITABLE**
- Mostrar campo de cotización secundaria: **EDITABLE**

**Valores:**
- `exchangeRate = [usuario ingresa]`
- `secondaryExchangeRate = [usuario ingresa]`

**Cálculos:**
- `amountInPrimary = amount × exchangeRate`
- `amountInSecondary = amount × secondaryExchangeRate`

---

## Cambio de Moneda

Al usuario seleccionar nueva moneda:
1. Detectar nuevo estado (caso 1, 2 o 3)
2. Si `lockPrimary = true`: `exchangeRate = 1`
3. Si `lockSecondary = true`: `secondaryExchangeRate = 1`
4. Si campo ahora está bloqueado pero tenía valor: reemplazar con bloqueado (1)

**Código:**
```tsx
const erState = getExchangeRateState(v)
setForm((p) => ({
  ...p,
  currencyCode: v,
  exchangeRate: erState.lockPrimary ? '1' : p.exchangeRate,
  secondaryExchangeRate: erState.lockSecondary ? '1' : p.secondaryExchangeRate,
}))
```

---

## Campos en DB

### Almacenamiento
- `exchange_rate NUMERIC(18,6)` — DEFAULT 1
- `secondary_exchange_rate NUMERIC(18,6)` — DEFAULT 1
- `amount_in_primary NUMERIC(18,2)` — Calculado, almacenado
- `amount_in_secondary NUMERIC(18,2) NULL` — Calculado, almacenado (null si no bimonetario)

### Cálculos Backend
```csharp
movement.AmountInPrimary = movement.Amount * movement.ExchangeRate;
movement.AmountInSecondary = family.IsBimonetary
    ? movement.Amount * movement.SecondaryExchangeRate
    : (decimal?)null;
```

---

## Validaciones

### Frontend
- `exchangeRate > 0` — Rechazar ≤0
- `secondaryExchangeRate > 0` — Rechazar ≤0 (si bimonetario)
- Regex: `/^\d*\.?\d{0,6}$/` (hasta 6 decimales)
- Aceptar `.` y `,` como separador, normalizar a `.`

### Backend
- Validar `exchangeRate > 0`
- Validar `secondaryExchangeRate > 0` si `family.IsBimonetary = true`
- Si mismatch entre estado esperado y valores: rechazar

---

## Display

### En Formulario
```
Cotización [MONEDA_PRIMARIA]: [value] [bloqueado si caso 1]
Cotización [MONEDA_SECUNDARIA]: [value] [bloqueado si caso 2]
```

### En Tabla (movimientos)
No mostrar cotizaciones (solo importe en moneda original)

---

## Ejemplo: Familia ARS/USD Bimonetaria

**Escenario A: Movimiento en ARS**
```
Moneda: ARS (primaria)
Importe: 1000 ARS
Cotización ARS: 1 [BLOQUEADO]
Cotización USD: 0.01 [EDITABLE] → usuario ingresa 0.01
→ amountInPrimary = 1000 × 1 = 1000 ARS
→ amountInSecondary = 1000 × 0.01 = 10 USD
```

**Escenario B: Movimiento en USD**
```
Moneda: USD (secundaria)
Importe: 100 USD
Cotización ARS: 100 [EDITABLE] → usuario ingresa 100 (1 USD = 100 ARS)
Cotización USD: 1 [BLOQUEADO]
→ amountInPrimary = 100 × 100 = 10000 ARS
→ amountInSecondary = 100 × 1 = 100 USD
```

**Escenario C: Movimiento en EUR (tercera moneda)**
```
Moneda: EUR (ni primaria ni secundaria)
Importe: 50 EUR
Cotización ARS: 110 [EDITABLE] → usuario ingresa 110 (1 EUR = 110 ARS)
Cotización USD: 1.1 [EDITABLE] → usuario ingresa 1.1 (1 EUR = 1.1 USD)
→ amountInPrimary = 50 × 110 = 5500 ARS
→ amountInSecondary = 50 × 1.1 = 55 USD
```
