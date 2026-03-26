# Tarjetas de Credito – Especificacion Funcional
*Version 1.1 – Marzo 2026*

---

## 1. Resumen

Las tarjetas de credito en MyFO representan un **pasivo**. El circuito cubre desde el registro de compras con cuotas hasta la liquidacion (pago del resumen), incluyendo prorrateo bimonetario para analisis financiero.

**Permisos:** Las tarjetas de credito no tienen sistema de permisos por usuario. Todos los miembros activos pueden ver y operar con todas las tarjetas. Los permisos relevantes son los de la caja/banco desde donde se paga (CashBoxPermission).

**Separacion clave:**
- **Devengado**: el gasto se reconoce al momento de la compra
- **Flujo de caja**: el dinero sale cuando se paga la liquidacion

---

## 2. Entidades ya implementadas (MVP)

### 2.1 Tarjeta de Credito (CreditCard)
- Nombre / descripcion
- Moneda
- Dia de cierre del resumen
- Dia de vencimiento del pago
- Estado (activa/inactiva)

### 2.2 Miembro de Tarjeta (CreditCardMember)
- Miembro de la familia asignado
- Ultimos 4 digitos del plastico
- Fecha de vencimiento del plastico
- Estado (activo/inactivo)

### 2.3 Compra con Tarjeta (CreditCardPurchase + CreditCardInstallment)
Al registrar un movimiento con forma de pago tarjeta:
- Se crea un CreditCardPurchase con los datos de la compra
- Se crean N CreditCardInstallment (una por cuota, con fecha estimada y monto proyectado)

**Bonificacion de compra (en MovementPayment):**
Al pagar con tarjeta, el usuario puede indicar una bonificacion:
- Tipo: **porcentaje** o **importe fijo** (no hay tabla de tipos, se elige directo)
- Si porcentaje: valor > 0 y <= 100
- Si importe: valor > 0
- El sistema calcula el monto de bonificacion

**Montos de la compra:**

| Campo | Descripcion | Ejemplo |
|---|---|---|
| GrossAmount | Monto bruto (importe total pagado con la tarjeta) | $10.000 |
| BonificationType | Tipo: "percentage" o "fixed_amount" (nullable) | "percentage" |
| BonificationValue | Valor ingresado (% o importe) | 10 |
| BonificationAmount | Monto calculado de la bonificacion | $1.000 |
| NetAmount | Monto neto = bruto - bonificacion. **Es la deuda real.** | $9.000 |

**Cuotas:**
- Projected_amount = monto bruto / cantidad de cuotas
- La bonificacion se **distribuye en las cuotas en orden** (cuota 1 primero, overflow a cuota 2, etc.)
- Cada cuota tiene: projected_amount, bonification_applied, effective_amount (= projected - bonif)
- Ejemplo: $10.000 / 6 cuotas = $1.667 por cuota

**Distribucion de bonificacion en cuotas:**
```
Compra: $10.000 bruto, bonif $5.000, neto $5.000, 6 cuotas

Cuota 1: projected $1.667, bonif $1.667, effective $0
Cuota 2: projected $1.667, bonif $1.667, effective $0
Cuota 3: projected $1.667, bonif $1.666, effective $1
Cuota 4: projected $1.667, bonif $0,     effective $1.667
Cuota 5: projected $1.667, bonif $0,     effective $1.667
Cuota 6: projected $1.665, bonif $0,     effective $1.665

Suma effective = $5.000 = net_amount ✓
```

**Monto para el resumen:**
- Si el usuario edito actual_amount → se usa actual_amount
- Si no → se usa effective_amount
- Formula: `monto_para_resumen = actual_amount ?? effective_amount`

**Validacion:** suma de effective_amount de todas las cuotas = net_amount.

---

## 2.4 Pagos de Tarjeta (tabla unificada credit_card_payments)

Los pagos de tarjeta se registran en la pagina "Pagos TC" usando una tabla unica `credit_card_payments` con `statement_period_id` opcional.

**Funcionalidad:**
- Registrar pagos parciales o totales contra una tarjeta
- Opcionalmente asociar a un periodo de liquidacion cerrado (combo en el formulario)
- Cada pago sale de una caja o banco cuya **moneda debe coincidir** con la de la tarjeta
- Cajas: solo se muestran las que el usuario tiene permiso Operate
- El usuario marca si el pago es total o parcial (checkbox)
- Pago total (`isTotalPayment = true`) requiere un periodo asociado
- Exchange rates: se auto-fetchean segun la moneda de la tarjeta vs primaria/secundaria de la familia
- Al registrar: se descuenta del saldo de la caja/banco. Al eliminar: se revierte.

**Pagos huerfanos (sin periodo):**
- `statement_period_id = null` → pago no asociado a ningun periodo
- Al crear un periodo, se muestran los pagos huerfanos para que el usuario seleccione cuales asociar
- No generan prorrateo

**Pagos asociados a periodo:**
- `statement_period_id != null` → asociado a un periodo cerrado
- Al cerrar un periodo, se ejecuta el prorrateo sobre los pagos asociados

Ver data-model.md seccion 2.4 para el esquema de la tabla `credit_card_payments`.

---

## 3. Circuito de Liquidacion (nuevo)

### 3.1 Conceptos

| Concepto | Descripcion |
|---|---|
| **Periodo** | Ciclo de facturacion de la tarjeta (ej: 15/02 al 15/03) |
| **Resumen / Statement** | El cierre del periodo con el detalle de lo que se debe |
| **Cuota confirmada** | Cuota cuyo monto real fue verificado contra el resumen del banco |
| **Cuota proyectada** | Cuota cuyo monto es estimado (total compra / cantidad cuotas) |
| **Cargo** | Costo adicional del banco: interes, seguro, mantenimiento, etc. Se interpreta como **egreso**. |
| **Bonificacion de resumen** | Descuento del banco en el resumen: cashback, reintegro, descuento debito automatico. Se interpreta como **ingreso**. |
| **Bonificacion de compra** | Descuento aplicado al momento de pagar con TC (ej: 10% por promocion banco). Se distribuye automaticamente en las cuotas en orden, reduciendo el effective_amount de cada una. |
| **Pago** | Dinero que sale de caja/banco para cancelar el resumen |
| **Prorrateo** | Distribucion proporcional de un pago entre las lineas del resumen |

### 3.2 Estados del periodo (dos dimensiones independientes)

El estado del periodo se modela con dos campos independientes:

**closed_at** (DateTime?): null = Abierto, con valor = Cerrado
**payment_status** (enum PaymentStatus): Unpaid, PartiallyPaid, FullyPaid

| closed_at | payment_status | Significado | Acciones permitidas |
|---|---|---|---|
| null | Unpaid | Abierto, sin pagos | Agregar/editar cuotas, cargos, bonificaciones. Editar montos de cuotas. |
| null | PartiallyPaid | Abierto, con pagos anticipados | Igual que abierto sin pagos. |
| timestamp | Unpaid | Cerrado, sin pagos | Registrar pagos. No se editan montos. |
| timestamp | PartiallyPaid | Cerrado, pagos parciales | Registrar mas pagos. |
| timestamp | FullyPaid | Cerrado, pagado total | Solo lectura. Todo bloqueado. |

### 3.3 Transiciones y reglase

**Cierre del periodo:**
- Accion manual del usuario (nunca automatica)
- Al cerrar, el usuario ingresa la fecha de cierre y vencimiento del **proximo** periodo
- Se calcula: cuotas (effective/actual) + cargos - bonificaciones banco + saldo anterior - pagos anticipados = SALDO
- Se abre automaticamente el siguiente periodo con las fechas ingresadas

**Reapertura:**
- Un periodo cerrado se puede reabrir si NO esta en estado "pagado total"
- Si tiene pagos parciales: se mantienen, se deshace el prorrateo, se recalcula al volver a cerrar
- Si esta pagado total: primero se debe eliminar el pago que completo el total → vuelve a cerrado → ahi se puede reabrir

**Pago total:**
- El usuario marca explicitamente un pago como "pago total" (checkbox/toggle)
- Al marcar pago total, el sistema valida que el importe cubre el saldo pendiente del periodo
- Se dispara el prorrateo completo y se marca el periodo como "pagado total"
- Diferencias minimas por redondeo se ajustan en la ultima linea
- Un pago total **requiere** que exista un resumen cerrado

---

## 4. Detalle de cada componente

### 4.1 Cuotas del periodo

Al abrir el detalle del periodo, aparecen las cuotas que caen dentro del rango de fechas. El usuario **selecciona cuales incluir** en este resumen — las no seleccionadas pasan al siguiente periodo (cubre casos donde el banco pasa una cuota al mes siguiente).

**Edicion inline:**
El usuario ve dos columnas y edita directamente el monto real:

| Fecha | Compra | Cuota | Proyectado | Real |
|---|---|---|---|---|
| 05/02 | Netflix | 3/12 | $1.500 | [$1.500] |
| 08/02 | Super | 1/1 | $15.000 | [$15.000] |
| 12/02 | Nafta | 2/6 | $3.200 | [$3.200] |
| 20/02 | Electrodom. | 5/12 | $8.333 | [$8.300] |

- Si el usuario no modifica, se usa el proyectado
- Solo editable cuando el periodo esta **abierto**
- El sistema trackea por compra: total original, cuotas confirmadas, cuotas pendientes

**Tracking por compra:**

| | Monto |
|---|---|
| Monto bruto compra | $100.000 |
| Bonificacion total | -$10.000 |
| Monto neto (deuda real) | $90.000 |
| Cuotas confirmadas (actual_amount o effective) | $33.832 |
| Cuotas pendientes (effective_amount) | $56.168 |

### 4.2 Cargos y bonificaciones

Son lineas adicionales dentro del resumen. A efectos de analisis:
- **Cargos** se interpretan como **egresos** (interes, seguro, mantenimiento = gasto)
- **Bonificaciones** se interpretan como **ingresos** (cashback, reintegro = ingreso)

Esto permite que aparezcan correctamente en reportes de ingresos/egresos y cashflow.

**Tipos de cargo:**
- Interes por saldo anterior
- Cuota de mantenimiento / seguro
- Impuestos
- Otros (texto libre)

**Tipos de bonificacion de resumen:**
- Cashback / reintegro
- Descuento por debito automatico
- Otros (texto libre)

**Bonificaciones de compra:**
- Nacen con la compra (registradas en MovementPayment al pagar con TC)
- Se distribuyen automaticamente en las cuotas en orden al crear la compra
- Reducen el effective_amount de cada cuota (no son linea separada en el resumen)
- En el detalle del periodo, la cuota muestra: proyectado, bonif aplicada, efectivo, real
- **Son distintas** de las bonificaciones de resumen (cashback, etc.): las de compra reducen cuotas, las de resumen son lineas del banco

Cada linea tiene: concepto, tipo, monto en moneda de la tarjeta.

### 4.3 Saldo anterior

- Se calcula automaticamente: saldo pendiente del periodo anterior
- **No es editable** por el usuario
- Si el periodo anterior quedo con saldo pendiente → aparece como primera linea
- Los intereses sobre ese saldo son un **cargo** que el usuario agrega manualmente

### 4.4 Primer periodo

La tarjeta necesita configurar las fechas del primer periodo:
- Fecha de inicio del primer periodo
- Fecha de cierre del primer periodo
- Fecha de vencimiento del primer periodo

Esto se puede hacer al crear la tarjeta o al momento de cerrar el primer periodo.

---

## 5. Pagos

### 5.1 Reglas generales

- Un pago siempre sale de **un solo origen** (caja o banco). Nunca tarjeta.
- Si el usuario quiere pagar de multiples origenes → registra multiples pagos parciales
- Cada pago tiene: fecha, origen (caja/banco), importe, tipo de cambio (del dia)
- Un pago se asocia a un periodo

### 5.2 Pagos anticipados

- Se pueden registrar pagos **antes** de que el periodo este cerrado
- Se asocian al periodo abierto actual
- Al cerrar el periodo, los pagos anticipados se restan del total
- Si no hay periodo abierto aun → se crea como "pago pendiente de aplicar" (concepto especial para cashflow/presupuesto)

### 5.3 Pagos parciales y totales

- Cada pago se registra como **parcial** o **total** (el usuario lo indica)
- **Pago parcial**: importe libre, no marca el periodo como pagado
- **Pago total**: el usuario marca el checkbox "pago total". El sistema:
  - Valida que el importe cubra el saldo pendiente
  - Marca el periodo como "pagado total"
  - Bloquea todo el periodo
- Un pago total **requiere** resumen cerrado (no se puede completar sin resumen)
- Un pago parcial que excede el saldo del periodo: el excedente pasa al siguiente como pago anticipado

### 5.4 Inmutabilidad

- Un pago asociado a un periodo **pagado total** no se puede editar ni eliminar directamente
- Para modificarlo: primero se elimina ese pago → el periodo vuelve a "cerrado" → se puede editar/eliminar pagos → registrar nuevos

---

## 6. Prorrateo bimonetario

### 6.1 Proposito

En modo bimonetario, cada pago debe distribuirse proporcionalmente entre las lineas del resumen para saber **cuanto se cancelo en moneda primaria y secundaria** por cada concepto. Esto permite analisis como: "cuanto le gane/perdi en dolares a esta compra".

### 6.2 Metodo de prorrateo: proporcional (pro-rata)

Cada pago se distribuye entre todas las lineas del resumen en proporcion al monto de cada linea.

**Ejemplo:**

```
Lineas del resumen:
  Linea A: $20.000 (40%)
  Linea B: $15.000 (30%)
  Linea C: $10.000 (20%)
  Linea D:  $5.000 (10%)
  Total:   $50.000

Pago 1: $10.000 ARS (TC del dia: 1 USD = 1.100 ARS)
  → Linea A absorbe: $4.000 ARS = 3,64 USD
  → Linea B absorbe: $3.000 ARS = 2,73 USD
  → Linea C absorbe: $2.000 ARS = 1,82 USD
  → Linea D absorbe: $1.000 ARS = 0,91 USD

Pago 2: $40.000 ARS (TC del dia: 1 USD = 1.200 ARS)
  → Linea A absorbe: $16.000 ARS = 13,33 USD
  → Linea B absorbe: $12.000 ARS = 10,00 USD
  → Linea C absorbe:  $8.000 ARS =  6,67 USD
  → Linea D absorbe:  $4.000 ARS =  3,33 USD
```

**Resultado por linea:**

| Linea | Devengado | Cancelado ARS | Cancelado USD | Diferencia USD |
|---|---|---|---|---|
| A ($20.000) | 20 USD (TC compra 1.000) | $20.000 | 16,97 USD | -3,03 USD |
| B ($15.000) | 14,29 USD (TC compra 1.050) | $15.000 | 12,73 USD | -1,56 USD |

Esto permite ver: "la compra A me costo 3,03 USD mas por la devaluacion entre la compra y el pago".

### 6.3 Cuando se prorratea

| Situacion | Prorrateo |
|---|---|
| Pago (parcial o total) antes del resumen | **No.** Queda sin prorratear (no hay lineas contra las cuales distribuir). |
| Se cierra el resumen y habia pagos sin prorratear | **Si.** Al cerrar, el sistema detecta pagos pendientes de aplicar y los prorratea automaticamente hasta donde cubran. |
| Pago (parcial o total) con resumen ya cerrado | **Si.** Se prorratea inmediatamente contra las lineas del resumen. |
| Pago que excede el periodo | Se prorratea hasta cubrir el 100% del periodo. El excedente pasa al siguiente como pago sin prorratear. |

### 6.4 Redondeo

- Prorrateo proporcional puede generar diferencias de centavos
- La **ultima linea** del resumen absorbe la diferencia para que la suma cierre exacto
- Tolerancia maxima de ajuste: a definir (sugerido: < $10 o < 0.5% del total, el menor)

### 6.5 Re-prorrateo

Cuando se **reabre un periodo** que ya tenia pagos prorrateados:
- Se eliminan TODOS los prorrateos del periodo
- Al volver a cerrar, se recalculan desde cero con los montos actualizados
- Esto es lo mas seguro: evita inconsistencias por ediciones parciales

Cuando se **elimina un pago** ya prorrateado:
- Se eliminan los prorrateos de ESE pago
- Se re-prorratean los pagos restantes en orden cronologico

---

## 7. Pantallas (UX)

### 7.1 Vista principal de tarjeta

```
┌─────────────────────────────────────────────────┐
│  TARJETA DE CREDITO                             │
│                                                 │
│  Visa ****1234                                  │
│  Moneda: ARS | Cierre: dia 15 | Vto: dia 5     │
│                                                 │
│  ┌─────────────────────────────────────────┐    │
│  │ PERIODO ACTUAL (Abierto)                │    │
│  │ 15/02 al 15/03                          │    │
│  │                                         │    │
│  │ Cuotas del periodo:          $45.000    │    │
│  │ Pagos anticipados:           -$10.000   │    │
│  │ Saldo estimado:              $35.000    │    │
│  │                                         │    │
│  │ [Ver detalle]  [Cerrar periodo]         │    │
│  └─────────────────────────────────────────┘    │
│                                                 │
│  Historial de periodos:                         │
│  ┌──────────┬──────────┬──────────┬────────┐    │
│  │ Periodo  │ Total    │ Pagado   │ Estado │    │
│  ├──────────┼──────────┼──────────┼────────┤    │
│  │ Ene 2026 │ $52.000  │ $52.000  │ Pagado │    │
│  │ Feb 2026 │ $48.000  │ $30.000  │Parcial │    │
│  │ Mar 2026 │ ~$45.000 │ $10.000  │Abierto │    │
│  └──────────┴──────────┴──────────┴────────┘    │
└─────────────────────────────────────────────────┘
```

### 7.2 Detalle del periodo

```
┌──────────────────────────────────────────────────────┐
│  DETALLE DEL PERIODO (Feb 2026 - Cerrado)            │
│  Cierre: 15/02 | Vencimiento: 05/03                  │
│                                                       │
│  ── Saldo anterior ──────────────────── $12.000 ──   │
│                                                       │
│  ── Cuotas del periodo ──────────────────────────────  │
│                             Proyect.  Bonif  Efect.  Real   │
│  [x] 05/02 Netflix (3/12)  $1.500     -    $1.500 [$1.500] │
│  [x] 08/02 Super (1/1)     $15.000    -    $15.000[$15.000] │
│  [x] 12/02 Nafta (2/6)     $3.200     -    $3.200 [$3.200] │
│  [x] 20/02 Electro (1/6)   $1.667  -$1.667  $0      [$0  ] │
│  [x] 20/02 Electro (2/6)   $1.667  -$1.333  $334   [$334 ] │
│  [ ] 28/02 Ropa (1/3)      $2.000     -    $2.000 [$2.000] │
│                              ──────         ──────  ──────  │
│                Incluidas (5):               $20.034 $20.034 │
│                Excluidas (1):                $2.000          │
│                                                       │
│  ── Cargos ────────────────────────────────────────   │
│  Interes saldo anterior                   $3.600     │
│  Seguro de vida                              $800     │
│  [+ Agregar cargo]                                    │
│                                                       │
│  ── Bonificaciones (banco) ───────────────────────    │
│  Cashback del mes                         -$1.200     │
│  [+ Agregar bonificacion]                             │
│                                                       │
│  ═══════════════════════════════════════════════════  │
│  TOTAL PERIODO:                           $43.200     │
│  Pagos realizados:                       -$30.000     │
│  SALDO PENDIENTE:                         $13.200     │
│  ═══════════════════════════════════════════════════  │
│                                                       │
│  ── Pagos ─────────────────────────────────────────   │
│  01/02 Pago anticipado (Banco Nacion)      $10.000    │
│  10/03 Pago parcial (Banco Nacion)         $20.000    │
│                                                       │
│  [Registrar pago]  [Reabrir periodo]                  │
└──────────────────────────────────────────────────────┘
```

**Nota:** Los campos "Real" de las cuotas solo son editables si el periodo esta **abierto**.
Cuando esta cerrado o pagado, se muestran en solo lectura.

### 7.3 Cerrar periodo

```
┌─────────────────────────────────────────────────┐
│  CERRAR PERIODO                                  │
│                                                  │
│  Periodo actual: 15/02 al 15/03                  │
│                                                  │
│  Resumen:                                        │
│    Cuotas (efectivo):  $20.034                   │
│    Cargos:              $4.400                   │
│    Bonificaciones:     -$1.200                   │
│    Saldo anterior:     $12.000                   │
│    Total:              $35.234                   │
│    Pagos antic.:      -$10.000                   │
│    A pagar:            $25.234                   │
│                                                  │
│  Proximo periodo:                                │
│    Fecha cierre:    [15/04/2026]                 │
│    Vencimiento:     [05/05/2026]                 │
│                                                  │
│  [Cancelar]  [Confirmar cierre]                  │
└─────────────────────────────────────────────────┘
```

### 7.4 Registrar pago

```
┌─────────────────────────────────────────────────┐
│  REGISTRAR PAGO                                  │
│                                                  │
│  Tarjeta: Visa ****1234                          │
│  Periodo: Feb 2026 (Cerrado)                     │
│  Saldo pendiente: $13.200                        │
│                                                  │
│  Origen:     [Banco Nacion    v]                 │
│  Fecha:      [19/03/2026]                        │
│  Importe:    [$________]                         │
│                                                  │
│  [x] Pago total ($13.200)  ← autocompleta        │
│      importe y valida que cubra el saldo         │
│                                                  │
│  [Cancelar]  [Registrar pago]                    │
└─────────────────────────────────────────────────┘
```

### 7.5 Flujo de navegacion

```
Tarjetas (listado)
  └→ Tarjeta X (vista principal)
       ├→ Periodo actual [Ver detalle]
       │    ├→ Editar cuotas (inline)
       │    ├→ Agregar cargo/bonificacion
       │    └→ Cerrar periodo → ingresa fechas proximo periodo
       ├→ Periodo cerrado [Ver detalle]
       │    ├→ Registrar pago
       │    └→ Reabrir periodo
       └→ Periodo pagado [Ver detalle] (solo lectura)
```

---

## 8. Reglas de negocio consolidadas

### 8.1 Periodos
1. El cierre es siempre **manual**
2. Al cerrar se ingresa fecha de cierre y vencimiento del proximo periodo
3. El proximo periodo se abre automaticamente al cerrar el actual
4. Solo puede haber **un periodo abierto** por tarjeta a la vez
5. El saldo anterior se calcula automaticamente (no editable)

### 8.2 Cuotas y bonificaciones de compra
1. Se generan automaticamente al registrar una compra con tarjeta
2. Monto proyectado = **monto bruto** / cantidad de cuotas (NO el neto)
3. Monto real es editable solo con periodo **abierto**
4. El sistema trackea: monto bruto, bonificacion, monto neto, cuotas confirmadas, cuotas pendientes
5. No se valida que la suma de cuotas reales = total compra (puede haber intereses del banco)
6. **Bonificacion de compra**: se distribuye automaticamente en las cuotas en orden al crear la compra. Reduce el effective_amount de cada cuota.
7. **Saldo de la compra** = net_amount = suma de effective_amount de todas las cuotas.
8. Bonificaciones de compra reducen cuotas. Bonificaciones de resumen (cashback, etc.) son lineas separadas del banco.
9. **Seleccion de cuotas**: al consolidar, el usuario selecciona que cuotas incluir en el periodo. Las no seleccionadas pasan al siguiente.
10. **Bonificacion sin tabla de tipos**: se elige directo al pagar con TC (porcentaje: >0 y <=100, o importe fijo: >0)

### 8.3 Pagos
1. Un pago sale de **un solo origen** (caja o banco)
2. Pagos anticipados se pueden registrar con periodo abierto
3. **Pago total** lo marca el usuario explicitamente (checkbox). El sistema valida que cubra el saldo.
4. No se puede hacer pago total sin resumen cerrado
5. Excedente de un pago pasa al siguiente periodo como pago anticipado
6. Periodo pagado total → todo bloqueado
7. Para modificar un pago en periodo pagado: eliminar el pago → vuelve a cerrado → operar
8. Cualquier pago (parcial o total) dispara prorrateo si hay resumen cerrado
9. Al cerrar resumen: si hay pagos sin prorratear, se aplican automaticamente

### 8.4 Prorrateo bimonetario
1. Metodo: proporcional (pro-rata) entre todas las lineas del resumen
2. Se guarda importe en moneda primaria y secundaria por cada asociacion pago-linea
3. El importe en moneda de la tarjeta asociado a una linea no puede superar el monto de esa linea
4. Cada linea del resumen puede tener N pagos asociados
5. Redondeo: la ultima linea absorbe la diferencia
6. Re-prorrateo al reabrir: se eliminan todos y se recalculan desde cero
7. Re-prorrateo al eliminar pago: se elimina su prorrateo, se re-prorratean los restantes cronologicamente

### 8.5 Inmutabilidad
1. Periodo abierto: todo editable
2. Periodo cerrado: solo pagos (no editar cuotas/cargos/bonificaciones)
3. Periodo pagado total: nada editable. Solo lectura.
4. Para reabrir un periodo pagado: eliminar el pago que completo el total primero

### 8.6 Proteccion de movimientos con pago TC
1. Si un movimiento tiene un pago con tarjeta cuyas cuotas ya estan incluidas en un resumen (cerrado o pagado): **no se puede editar ni eliminar** el movimiento ni el pago con tarjeta
2. Esto protege la integridad del circuito de liquidacion
3. Para corregir: reabrir el periodo → sacar la cuota del resumen → recien ahi se puede editar/eliminar el movimiento

---

## 9. Datos para analisis futuro

El prorrateo bimonetario permite generar los siguientes reportes y graficos:

### 9.1 Analisis por compra
- Importe devengado en moneda primaria y secundaria (al TC del dia de la compra)
- Importe cancelado en moneda primaria y secundaria (al TC de cada pago)
- Diferencia: ganancia o perdida cambiaria por compra

### 9.2 Analisis por periodo
- Total devengado vs total cancelado en ambas monedas
- Costo real de financiacion (si hubo pagos parciales con interes)

### 9.3 Analisis por tarjeta
- Costo total de cargos por periodo (intereses, seguros, etc.)
- Total bonificaciones obtenidas
- Evolucion de deuda en ambas monedas
- Cuotas comprometidas futuras (proyeccion de pagos pendientes)

### 9.4 Cash flow
- **Devengado**: gasto registrado al momento de la compra
- **Financiero**: dinero sale al momento del pago de la tarjeta
- **Pendiente**: cuotas futuras comprometidas por tarjeta (para planificacion)
- **Pago pendiente de aplicar**: pagos anticipados sin resumen aun (visible en cashflow como compromiso cumplido anticipadamente)

---

## 10. Preguntas resueltas

| Pregunta | Resolucion |
|---|---|
| Cierre automatico o manual | **Manual** (el usuario decide cuando tiene todo cargado) |
| Trackear cuotas futuras | **Si** (sirve para planificacion y cashflow) |
| Saldo pendiente entre periodos | **Arrastra** al siguiente como saldo anterior |
| Edicion de cuotas | **Inline** en el detalle del periodo (sin modal) |
| Pago de multiples origenes | **No.** Un origen por pago. Multiples pagos parciales si necesario. |
| Reabrir periodo pagado | Eliminar pago que completo el total → reabrir |
| Prorrateo | Proporcional (pro-rata), guarda ambas monedas |
| Re-prorrateo | Se borra todo y recalcula desde cero |
| Tabla de tipos de bonificacion | **No.** Se elige directo al pagar con TC: porcentaje o importe fijo |
| Cuotas sobre bruto o neto | **Bruto** para projected. Bonificacion se distribuye en cuotas reduciendo effective_amount. |
| Seleccion de cuotas al cerrar | **Si.** El usuario elige cuales incluir, las demas pasan al siguiente periodo |
| Pago total: deteccion auto o manual | **Manual.** El usuario marca "pago total" y el sistema valida |
| Pagos TC en resumenes: editables? | **No.** Movimientos con pagos TC en resumenes cerrados/pagados no se pueden editar ni eliminar |
| Cuando se prorratea | Al cerrar (si hay pagos pendientes) o al registrar pago (si hay resumen cerrado) |

---

## 11. Control de acceso

Ver documento transversal: `documentation/permissions-and-roles.md`

### Entidad tarjeta (CRUD)
- **FamilyAdmin**: CRUD completo sobre tarjetas, miembros de tarjeta
- **Member**: solo lectura

### Pagos TC (CreditCardPayments)
- **Crear pago**: requiere Operate en la caja de origen (CashBoxPermission). Bancos: sin restriccion.
- **Cajas en combo de origen**: solo se muestran las que el usuario tiene Operate
- **Editar pago**: exchange rates, descripcion y fecha son editables siempre. Caja origen y monto NO son editables sin Operate en la caja origen.
- **Eliminar pago**: requiere Operate en la caja de origen
- **No se puede cambiar moneda** del pago sin Operate en la caja origen

### Resumenes de tarjeta (Statement Periods)
- Todos los miembros activos pueden ver resumenes
- Todos los miembros activos pueden editar resumenes **no cerrados** (cuotas, cargos, bonificaciones)
- Pagos dentro del resumen: siguen reglas de pagos TC (arriba)

### Compras con tarjeta (desde movimientos)
- Las reglas de permisos de la compra se rigen por el movimiento padre (ver movements/functional-spec.md seccion 7)
- Si el movimiento tiene pago TC cuyas cuotas ya estan en resumen cerrado/pagado: no se puede editar ni eliminar
