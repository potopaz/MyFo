# Tarjetas de Credito – Requisitos de Analisis
*Version 1.0 – Marzo 2026*

---

## Proposito

Documentar los datos que el sistema debe persistir durante la liquidacion de tarjetas de credito para habilitar graficos y reportes de analisis financiero en el futuro.

---

## 1. Prorrateo bimonetario

Cada pago de tarjeta se distribuye proporcionalmente (pro-rata) entre las lineas del resumen. Por cada asociacion pago-linea se persiste:

| Campo | Descripcion |
|---|---|
| payment_id | Referencia al pago |
| statement_line_id | Referencia a la linea del resumen (cuota, cargo o bonificacion) |
| amount_primary_currency | Importe prorrateado en moneda primaria |
| amount_secondary_currency | Importe prorrateado en moneda secundaria |
| amount_card_currency | Importe prorrateado en moneda de la tarjeta |
| primary_exchange_rate | TC usado (moneda tarjeta → primaria) al momento del pago |
| secondary_exchange_rate | TC usado (secundaria → primaria) al momento del pago |

---

## 2. Reportes habilitados

### 2.1 Ganancia/perdida cambiaria por compra

**Pregunta que responde:** "Cuanto gane o perdi en dolares por la devaluacion entre la compra y el pago"

**Calculo:**
```
Importe devengado USD = importe compra / TC al dia de la compra
Importe cancelado USD = suma(prorrateos del pago en USD)
Diferencia = devengado - cancelado
  > 0 → ganancia cambiaria (el dolar bajo entre compra y pago)
  < 0 → perdida cambiaria (el dolar subio entre compra y pago)
```

### 2.2 Costo real de financiacion

**Pregunta que responde:** "Cuanto me costo financiarme con la tarjeta"

**Calculo:**
```
Costo financiacion = suma(cargos por interes) por periodo
Puede verse en moneda primaria o secundaria
```

### 2.3 Evolucion de deuda por tarjeta

**Pregunta que responde:** "Como evoluciona mi deuda con esta tarjeta mes a mes"

**Datos:**
- Saldo total por periodo (en ambas monedas)
- Cuotas comprometidas futuras (suma de cuotas proyectadas pendientes)
- Tendencia: creciendo / estable / decreciendo

### 2.4 Eficiencia de bonificaciones

**Pregunta que responde:** "Cuanto me devolvio/desconto el banco vs cuanto gaste"

**Calculo:**
```
% bonificacion = suma(bonificaciones) / suma(cuotas) * 100
Por periodo, por tarjeta, acumulado
```

### 2.5 Cash flow real vs devengado

**Pregunta que responde:** "Cuando se devengo el gasto vs cuando salio la plata"

**Vista temporal:**
| Mes | Devengado (compras) | Financiero (pagos) | Diferencia |
|---|---|---|---|
| Enero | $50.000 | $0 | +$50.000 (deuda) |
| Febrero | $30.000 | $80.000 | $0 (se puso al dia) |

### 2.6 Proyeccion de cuotas futuras

**Pregunta que responde:** "Cuanto tengo comprometido en cuotas futuras"

**Datos:**
- Total cuotas pendientes por tarjeta
- Distribucion por mes futuro
- En ambas monedas (usando TC actual para proyeccion)

---

## 3. Consideraciones tecnicas para persistencia

1. **No recalcular, persistir:** Los importes en ambas monedas se guardan al momento del prorrateo. No se recalculan despues (el TC cambia diariamente).

2. **TC del pago, no del dia:** Se usa el primary_exchange_rate registrado en el movimiento de pago, que es el que el usuario ingreso/confirmo.

3. **Auditoria de re-prorrateo:** Cuando se re-prorratea (por reapertura o eliminacion de pago), los prorrateos anteriores se eliminan (soft delete) y se crean nuevos. Esto permite auditar si es necesario.

4. **Precision:** Misma precision que el resto del sistema (6 decimales para TC, 2 decimales para importes).

5. **Pago pendiente de aplicar:** Los pagos anticipados sin resumen se marcan con un concepto especial. Son visibles en cashflow como "compromiso pagado anticipadamente" y en presupuesto como disponibilidad ya comprometida.
