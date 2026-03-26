# MyFO – Product Brief
**My Family Office**
*Version 1.4 – Marzo 2026*

---

## 1. Vision del Producto

**MyFO** es una aplicacion web SaaS multitenant que permite a familias de cualquier parte del mundo organizar su economia de manera simple pero completa. Combina registro de ingresos y gastos, administracion de cajas y cuentas bancarias, tarjetas de credito con cuotas, y una vision dual financiera/contable, con soporte opcional para contextos bimonetarios.

**Problema que resuelve:** Las familias no tienen una herramienta digital accesible y flexible que les permita entender a donde va su dinero y tomar mejores decisiones economicas.

**Propuesta de valor:**
- Registro facil de movimientos con multiples formas de pago (web responsive + bot WhatsApp en v2)
- Soporte bimonetario configurable (ideal para paises con moneda volatil)
- Dualidad financiero/contable sin que el usuario necesite saber contabilidad
- Multi-usuario con permisos granulares por caja
- Multilenguaje (español por defecto)
- Auditoria completa con soft delete en todo el sistema
- Preparado para escalar a multiples familias (multitenant global)

---

## 2. Stack Tecnologico

| Capa | Tecnologia |
|---|---|
| Frontend | React (SPA, responsive mobile-first) |
| Backend | .NET 10 |
| Base de datos | PostgreSQL 16 |
| Autenticacion | Auth propio + OAuth Google |
| Notificaciones | In-app (v1.1) + WhatsApp (v2) |
| Bot | WhatsApp Business API (v2) |

---

## 3. Modelo de Negocio

**Modelo:** SaaS por tenant con precio basado en usuarios activos.

| Plan | Usuarios activos | Precio |
|---|---|---|
| Trial | Ilimitado | Gratis (primeros 2 meses) |
| Base | 3 | Precio mensual fijo |
| Family+ | Usuarios adicionales | Precio base + diferencial por usuario |

**Principios:**
- Precio mensual bajo para maximizar adopcion
- El Admin del tenant controla quienes son usuarios activos (hasta el limite del plan)
- Sin feature-gating en v1: todos los planes acceden a las mismas funcionalidades
- Bonificaciones manuales: el Super Admin puede aplicar descuentos o periodos gratuitos extendidos por tenant
- En v2 evaluar limites de historial o cajas como diferenciador de tier

**Panel Super Admin (MVP):**
- Vista de todos los tenants activos
- Estado de suscripcion por tenant (trial, activo, vencido)
- Gestion manual de bonificaciones y periodos gratuitos
- Informacion de facturacion basica

---

## 4. Internacionalizacion

- **Idioma:** Multilenguaje desde la arquitectura. Español como idioma por defecto.
- **Monedas:** Configurables por tenant (ver seccion 8.1)
- **Zona horaria:** Configurable por tenant
- **Formato de fechas y numeros:** Adaptado al locale del usuario

---

## 5. Auditoria y Soft Delete

**Principio general:** No existe borrado fisico en el sistema. Toda entidad y todo movimiento tiene ciclo de vida auditado.

**Campos de auditoria presentes en TODAS las entidades:**

| Campo | Detalle |
|---|---|
| created_at | Fecha y hora de creacion |
| created_by | Usuario que creo el registro |
| modified_at | Fecha y hora de ultima modificacion |
| modified_by | Usuario que realizo la ultima modificacion |
| deleted_at | Fecha y hora de borrado logico (null si activo) |
| deleted_by | Usuario que realizo el borrado logico |

Esto aplica a: movimientos, cajas, bancos, tarjetas, categorias, subcategorias, centros de costo, transferencias, movimientos recurrentes, prestamos, contrapartes, y toda entidad de configuracion.

---

## 6. Actores del Sistema

### 6.1 Roles por Plataforma

| Rol | Descripcion |
|---|---|
| **Super Admin** | Administrador de la plataforma. Gestiona tenants, planes, bonificaciones y metricas globales. |
| **Admin de Familia** | Administrador del tenant. Gestiona usuarios, monedas, configuracion e invitaciones. Puede editar/borrar cualquier movimiento. |
| **Miembro** | Usuario operativo. Opera dentro de los permisos asignados por caja. Solo puede editar/borrar movimientos propios. |

### 6.2 Permisos por Caja

| Permiso | Descripcion |
|---|---|
| **Ver** | Puede visualizar movimientos y saldo de la caja |
| **Operar** | Puede agregar, editar y borrar movimientos propios en la caja |

Un usuario puede tener permisos en multiples cajas. Una caja puede tener multiples usuarios asignados.

---

## 7. Modelo Multitenant y Multi-familia

- Cada **tenant = una familia**
- Un usuario (email) puede pertenecer a **multiples familias**
- Al loguearse con acceso a una sola familia: entra directo. Con varias: elige cual administrar
- Autenticacion: email/password propio + Google OAuth

---

## 8. Modulos Funcionales

---

### 8.1 Monedas y Configuracion Bimonetaria

**Configuracion por tenant:**

| Campo | Detalle |
|---|---|
| Moneda principal | Moneda base del tenant |
| Modo bimonetario | Si / No |
| Moneda secundaria | Solo si bimonetario = Si |

**Cotizaciones (exchange rates):**
Al registrar un movimiento se guardan hasta dos cotizaciones:
- `exchange_rate`: cuantas unidades de moneda primaria equivale 1 unidad de la moneda del movimiento (NULL si el movimiento es en moneda primaria). Precision: 6 decimales.
- `secondary_exchange_rate`: cuantas unidades de moneda primaria equivale 1 unidad de la moneda secundaria (NULL si el tenant no tiene modo bimonetario). Precision: 6 decimales.

El sistema calcula y persiste `amount_in_primary` y `amount_in_secondary` al momento de la carga. Esto garantiza que cada movimiento queda auditado con la cotizacion exacta que se uso.

**Modo bimonetario:**
Cuando esta activo, el usuario ingresa el `secondary_exchange_rate` en cada movimiento (puede venir sugerido desde fuente externa configurable: ARCA para Argentina, etc., o ingresarse manualmente). El usuario elige en que moneda ver los reportes y dashboards (no se muestran ambas simultaneamente).

---

### 8.2 Cajas

**Descripcion:** Representan dinero fisico disponible (billeteras, cajas fuertes, sobres, etc.).

**Reglas de negocio:**
- Cada caja tiene una **moneda unica**
- Clasificacion interna: **Activo**
- Campos: nombre, descripcion, moneda, estado (activa/inactiva)
- Permisos por usuario: Ver / Operar (independientes)
- Solo el creador del movimiento o un Admin puede editar/borrar un movimiento

**Arqueo de caja (cierre manual) – v1.1:**
- El usuario ingresa el saldo fisico real y decide si cierra a esa fecha
- El cierre es siempre manual: el sistema no cierra automaticamente aunque los saldos coincidan
- Si hay diferencia entre saldo fisico y saldo del sistema: se genera automaticamente un movimiento de ajuste (ingreso o egreso) hacia una **subcategoria de sistema protegida** (no editable ni borrable por usuarios)
- Movimientos anteriores al cierre: no se pueden agregar, borrar ni modificar importes
- Para corregir un movimiento anterior: se cambia la fecha de cierre (reabrir)
- En periodos cerrados: solo se pueden editar campos no monetarios

**Cierre de ejercicio – v1.1:**
- El Admin puede definir una fecha de cierre de ejercicio a nivel de familia
- A partir de esa fecha hacia atras: **ningun campo** de ningun movimiento puede modificarse (ni importes, ni categorias, ni imputaciones)
- Equivale a un cierre contable. Protege la integridad historica del periodo.

---

### 8.3 Cuentas Bancarias

**Descripcion:** Representan dinero en bancos o billeteras digitales.

**Reglas de negocio:**
- Cada cuenta tiene una **moneda unica**
- Clasificacion interna: **Activo**
- Campos: nombre, banco, CBU/alias/numero de cuenta, moneda, estado (activa/inactiva)
- Mismos permisos que cajas: Ver / Operar
- **Reconciliacion bancaria** (v2)

---

### 8.4 Formas de Pago

**Descripcion:** Entidad que representa como se cancela economicamente un movimiento, una transferencia o un pago de tarjeta. Un mismo movimiento puede tener multiples formas de pago.

**Tipos disponibles:**

| Tipo | Disponible en |
|---|---|
| Caja | Movimientos, transferencias, pagos de tarjeta |
| Cuenta bancaria | Movimientos, transferencias, pagos de tarjeta |
| Tarjeta de credito | Solo movimientos de ingreso/egreso |

**Regla clave:** Las transferencias entre disponibilidades y los pagos de cancelacion de tarjeta de credito **no pueden usar tarjeta de credito como forma de pago**.

**Estructura de una forma de pago:**
- Tipo (caja / cuenta bancaria / tarjeta de credito)
- Referencia a la entidad correspondiente
- Importe (en la misma moneda que el movimiento)
- Si es tarjeta: miembro del plastico, cuotas, tipo de bonificacion, valor de bonificacion

**Todas las formas de pago de un movimiento deben estar en la misma moneda que el movimiento.** La suma de los importes debe coincidir con el importe total del movimiento.

---

### 8.5 Transferencias entre Cajas y Bancos

**Descripcion:** Movimiento de disponibilidad entre activos propios.

**Reglas de negocio:**
- Entidad independiente (no es ingreso ni gasto)
- Genera dos movimientos vinculados: debito en origen / credito en destino
- Si origen y destino tienen diferente moneda: se registra la cotizacion aplicada
- Formas de pago: caja o cuenta bancaria (nunca tarjeta de credito)

**MVP:** las transferencias se confirman automaticamente (sin flujo de aprobacion).

**Flujo de aprobacion (v1.1):**
- Campos de auditoria de aprobacion: fecha de aprobacion/rechazo, usuario que aprobo/rechazo
- Si el usuario tiene permiso Operar en la caja destino: se confirma automaticamente
- Si no tiene permiso en destino: queda pendiente. El responsable recibe notificacion in-app
- El saldo del destino no se impacta hasta que la transferencia sea aceptada

---

### 8.6 Categorias y Subcategorias

**Estructura:**
```
Categoria
  └── Subcategoria
        ├── Tipo de movimiento: Ingreso / Egreso / Ambos
        ├── Tipo contable recomendado: Activo / Pasivo / Ingreso / Gasto
        ├── Centro de costo recomendado
        └── Caracter recomendado: Ordinario / Extraordinario
```

**Subcategorias de sistema:**
- Creadas por el sistema, no modificables ni borrables por usuarios
- Uso interno: ajustes de arqueo de caja (diferencias de faltante/sobrante)

**Reglas de negocio:**
- Configuracion precargada de categorias/subcategorias de uso familiar general
- El Admin puede crear, editar y desactivar categorias y subcategorias propias
- Las entidades inactivas no aparecen en formularios pero se pueden consultar
- El tipo contable y el caracter son recomendaciones sobreescribibles por movimiento
- Al cargar un movimiento tipo Ingreso: solo se muestran subcategorias habilitadas para ingresos (y viceversa)

---

### 8.7 Centros de Costo

**Reglas de negocio:**
- Completamente personalizables por familia
- **MVP:** cada movimiento se asigna a un unico centro de costo
- **v1.1:** asignacion avanzada: porcentajes entre multiples centros (suma exacta 100%)
- Las subcategorias pueden tener un centro de costo recomendado
- Se pueden activar/desactivar

---

### 8.8 Movimientos

**Descripcion:** Nucleo del sistema. Registra ingresos y egresos.

**Campos:**

| Campo | Detalle |
|---|---|
| Fecha | Fecha del movimiento |
| Tipo | Ingreso / Egreso |
| Importe total | Mayor a 0 |
| Moneda | Moneda del movimiento (unica) |
| Exchange rate | Moneda del movimiento → primaria (NULL si es primaria). 6 decimales. |
| Secondary exchange rate | Moneda secundaria → primaria (NULL si no bimonetario). 6 decimales. |
| amount_in_primary | Calculado y persistido |
| amount_in_secondary | Calculado y persistido (NULL si no bimonetario) |
| Descripcion | Texto libre (sugerido desde movimiento recurrente si aplica) |
| Categoria / Subcategoria | Clasificacion |
| Tipo contable | Activo / Pasivo / Ingreso / Gasto |
| Caracter | Ordinario / Extraordinario |
| Centro de costo | Uno (MVP), multiples con % (v1.1) |
| Formas de pago | Una o multiples, en la misma moneda que el movimiento |
| Creado por | Auditoria automatica |

**Reglas de negocio:**
- Solo el creador o un Admin puede editar/borrar
- No se pueden modificar importes si la caja esta cerrada a esa fecha (v1.1)
- Nada se puede modificar si la fecha cae dentro de un cierre de ejercicio (v1.1)
- Todos los importes deben ser mayores a 0
- Todas las formas de pago deben sumar exactamente el importe total del movimiento

---

### 8.9 Movimientos Recurrentes (v1.1)

**Descripcion:** Plantillas de movimientos (ingresos O egresos) que se repiten con una periodicidad definida. Ejemplo de uso: cuota del colegio (egreso mensual), alquiler cobrado (ingreso mensual durante 2 años).

**Campos:**

| Campo | Detalle |
|---|---|
| Tipo | Ingreso / Egreso |
| Descripcion | Texto identificatorio (se pre-carga al generar el movimiento) |
| Subcategoria | Clasificacion predefinida |
| Importe estimado | Referencia, ajustable al confirmar |
| Periodicidad | Mensual, bimestral, trimestral, anual, etc. |
| Fecha de inicio | Desde cuando aplica |
| Fecha de fin | Opcional. Al llegar a esa fecha se desactiva automaticamente |
| Estado | Activo / Inactivo |

**Reglas de negocio:**
- Soporta tanto ingresos como egresos recurrentes
- Cuando vence el periodo y no se cargo: notificacion in-app
- Desde la notificacion el usuario confirma y carga el movimiento (puede ajustar importe y descripcion)
- La fecha de fin y el estado activo/inactivo son complementarios: la fecha de fin automatiza la desactivacion
- Los movimientos recurrentes activos alimentan el cash flow proyectado (tanto ingresos como egresos futuros)

---

### 8.10 Tarjetas de Credito

**Descripcion:** Entidad principal que representa una tarjeta de credito. Puede tener multiples miembros/plasticos asociados.

**Clasificacion interna:** Pasivo

**Entidad Tarjeta:**
- Nombre / descripcion
- Dia de cierre del resumen
- Dia de vencimiento del pago
- Estado (activa/inactiva)

**Entidad Miembro de Tarjeta** (dependiente de la tarjeta):
- Miembro de la familia asignado
- Ultimos 4 digitos del plastico
- Fecha de vencimiento del plastico → alerta configurable previa al vencimiento (v1.1)
- Estado (activo/inactivo)

**MVP – Registro de compra con tarjeta:**
Al usar tarjeta de credito como forma de pago en un movimiento de egreso, esa forma de pago incluye:
- Tarjeta
- Miembro/plastico que realizo la compra
- Importe pagado con esa tarjeta (en la misma moneda que el movimiento)
- Cantidad de cuotas
- Tipo de bonificacion esperada
- Valor de bonificacion (importe fijo o porcentaje segun el tipo)
- El sistema registra los cargos futuros en los resumenes correspondientes

**Tipos de bonificacion:**
- Entidad configurable por tenant
- Cada tipo define si la bonificacion se expresa en importe fijo o porcentaje
- Ejemplo: "Cashback" (porcentaje), "Descuento fijo" (importe), "Bonificacion de cuota" (importe)

**Liquidacion de resumen (v1.1):**

```
CreditCardStatement  (el periodo/resumen mensual)
  └── CreditCardPayment  (cada evento de pago — puede haber varios parciales)
        └── CreditCardPaymentMethod  (formas de pago del evento: caja o banco, nunca tarjeta)
```

*Configuracion del tenant:*
- **Modo simple:** no concilia compras. Cada pago registra solo el importe y sus formas de pago. La deuda de la tarjeta baja automaticamente.
- **Modo conciliacion:** al realizar el pago total del periodo, se concilian los cargos con las compras registradas.

*Pagos parciales del periodo (ambos modos):*
- Se pueden registrar multiples pagos parciales antes del pago total
- Cada pago tiene sus propias formas de pago (caja o banco)
- No se realiza conciliacion contra compras individuales

*Pago total del periodo (modo conciliacion):*
1. El sistema presenta todas las compras del periodo (incluyendo cuotas de compras anteriores)
2. Muestra los pagos parciales ya realizados en el periodo
3. El usuario confirma o ajusta cada compra
4. Opcion de agregar rapidamente compras no registradas
5. Se registran comisiones y bonificaciones del banco (con su tipo)
6. El total del pago debe conciliar con compras + bonificaciones - pagos parciales previos
7. Una vez realizado el pago total del periodo, no se pueden agregar mas pagos a ese resumen

---

### 8.11 Contrapartes (v2)

**Descripcion:** Entidad que representa personas o entidades externas involucradas en prestamos.

**Campos:**
- Nombre
- Descripcion / datos de contacto opcionales
- Estado (activa/inactiva)

Reutilizable: una contraparte puede estar vinculada a multiples prestamos.

---

### 8.12 Prestamos (v2)

**Tipos:**
- **Prestamo otorgado:** sale de una caja/banco hacia una contraparte
- **Prestamo obtenido:** entra a una caja/banco desde una contraparte

**Campos:**
- Contraparte (entidad, no texto libre)
- Importe original
- Moneda
- Caja/banco origen o destino
- Fecha

**Cancelacion:**
- Se registran cancelaciones parciales especificando: capital / interes
- El prestamo no puede cerrarse hasta que capital cancelado = importe original
- Prestamos otorgados: aparecen en dashboard como "dinero a recuperar"
- Prestamos obtenidos: aparecen como deuda

**Cash flow:**
- Importe total aparece en el cash flow del mes de origen
- V3: cronograma de cancelacion para proyectar en la linea de tiempo

---

### 8.13 Dashboard

**MVP:** Dashboard con widgets fijos:
- Disponibilidad por caja/banco (saldo actual segun permisos del usuario)
- Grafico ingresos vs gastos ultimos 6 meses

**v1.1:** Dashboard personalizable. El usuario elige que widgets ver. La moneda de visualizacion es elegida por el usuario (primaria o secundaria si bimonetario activo).

**Widgets adicionales (v1.1):**

| Widget | Detalle |
|---|---|
| Deudas totales | Saldo de tarjetas + prestamos obtenidos |
| Dinero a recuperar | Prestamos otorgados pendientes (v2) |
| Ingresos vs gastos | Toggle: vision financiera / contable |
| Distribucion de gastos | Porcentaje por categoria. Filtrable por periodo |
| Gastos por categoria/subcategoria | Cuanto se gasto en el mes |
| Ordinario vs extraordinario | Gasto mensual esperado vs excepcional |

---

### 8.14 Cash Flow (v1.1)

**Vision financiera:** cuando el dinero efectivamente se mueve
**Vision contable:** cuando se genera el compromiso (devengado)

**Fuentes del cash flow proyectado:**
- Cuotas de tarjeta de credito pendientes
- Movimientos recurrentes activos (proyectados hasta su fecha de fin si tiene)
- Prestamos con cronograma (v3)

---

## 9. Onboarding de Nueva Familia

**MVP (simplificado):**
1. Registro (email/password o Google)
2. Crear familia, elegir idioma y zona horaria
3. Configurar moneda principal
4. Categorias precargadas (se puede personalizar despues)
5. Crear al menos una caja o cuenta bancaria

**v1.1 (guiado):**
6. Configurar monedas adicionales + bimonetario
7. Crear centros de costo con ejemplos sugeridos
8. Invitar miembros
9. Tutorial rapido interactivo de primeras cargas

---

## 10. Notificaciones

**v1.1:**

| Evento | Canal |
|---|---|
| Transferencia pendiente de aceptar | In-app |
| Movimiento recurrente vencido | In-app |
| Tarjeta (plastico) proxima a vencer | In-app |
| Resumen de tarjeta proximo a cerrar | In-app (configurable) |
| Invitacion a familia | Email |

*WhatsApp como canal adicional: v2*

---

## 11. Roadmap

### MVP
- Autenticacion (propio + Google) + multitenant
- Panel Super Admin basico (tenants, suscripciones, bonificaciones)
- Multilenguaje (español por defecto)
- Auditoria completa + soft delete en todo el sistema
- Monedas + modo bimonetario configurable
- Cajas (sin arqueo) + cuentas bancarias
- Categorias/subcategorias (con config precargada + subcategorias de sistema protegidas)
- Centros de costo (asignacion simple: 1 CC por movimiento)
- Movimientos con multiples formas de pago (incluye pago con tarjeta de credito)
- Tarjetas de credito (entidad + miembros/plasticos + registro de compras con cuotas + bonificacion)
- Transferencias entre cajas/bancos (auto-confirm, sin flujo de aprobacion)
- Dashboard basico (saldos + grafico ingresos vs gastos)
- Onboarding simplificado
- Trial gratuito 2 meses

### v1.1
- Arqueo de caja + cierre de ejercicio
- Asignacion multi-CC con porcentajes
- Transferencias con flujo de aprobacion + notificaciones in-app
- Movimientos recurrentes (ingresos y egresos)
- Liquidacion de resumen de tarjeta (modo simple + modo conciliacion)
- Dashboard personalizable con widgets seleccionables
- Cash flow proyectado (financiero y contable)
- Onboarding guiado completo
- Alertas de vencimiento de plastico

### v2
- Bot WhatsApp (registro de gastos por mensaje, con confirmacion)
- Prestamos (otorgados y obtenidos con contrapartes)
- Reconciliacion bancaria
- Presupuestos por categoria
- Notificaciones via WhatsApp
- Exportacion Excel / PDF

### v3
- Cronograma de cancelacion de prestamos
- Interacciones avanzadas con bot WhatsApp
- Reportes avanzados personalizables

---

## 12. Decisiones Pendientes / A Profundizar

| Tema | Notas |
|---|---|
| UX de liquidacion de tarjeta | Disenar en detalle antes de codificar (v1.1) |
| Catalogo inicial de categorias precargadas | Definir conjunto base multilenguaje |
| Fuentes de cotizacion por pais | ARCA para Argentina. Alternativas: Fixer, ECB, Open Exchange Rates |
| Pasarela de pagos para suscripciones | A definir: Stripe, MercadoPago, etc. |
| Formato del mensaje bot WhatsApp | A definir en v2 (keywords por subcategoria + IA) |
| Limite de widgets en dashboard | A definir cantidad maxima y comportamiento en mobile (v1.1) |

---

## 13. Glosario

| Termino | Definicion |
|---|---|
| **Tenant** | Una familia dentro del sistema |
| **Caja** | Dinero fisico disponible (billetera, caja fuerte, etc.) |
| **Modo bimonetario** | Configuracion que permite registrar cada movimiento en dos monedas |
| **Vision financiera** | Registra cuando el dinero efectivamente se mueve |
| **Vision contable** | Registra cuando se genera el compromiso (devengado) |
| **Arqueo de caja** | Control fisico del saldo. Genera un cierre de periodo manual. |
| **Cierre de ejercicio** | Fecha a partir de la cual ningun campo puede modificarse hacia atras |
| **Cuota** | Division de una compra con tarjeta en pagos mensuales |
| **Liquidacion** | Proceso de pago del resumen de tarjeta de credito |
| **Cotizacion** | Tipo de cambio entre monedas al momento del movimiento |
| **Caracter** | Clasificacion de un gasto como ordinario (esperado) o extraordinario (excepcional) |
| **Contraparte** | Persona o entidad externa involucrada en un prestamo |
| **Soft delete** | Borrado logico: el registro se marca como eliminado pero no se borra fisicamente |
