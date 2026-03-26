# MyFO – Plan de Implementacion MVP
*Version 1.0 – Marzo 2026*

---

## Fases del MVP

El MVP se divide en 6 fases secuenciales. Cada fase produce algo funcional y testeable.

---

## Fase 1 – Fundacion
> Estructura del proyecto, base de datos, autenticacion y multitenant.

### 1.1 Estructura del proyecto
- Crear solucion .NET con los 4 proyectos (Domain, Application, Infrastructure, API)
- Crear proyecto React con Vite + TypeScript
- Configurar docker-compose.yml con PostgreSQL 16
- Configurar .editorconfig y reglas basicas del proyecto

### 1.2 Base de datos y EF Core
- Configurar ApplicationDbContext
- Implementar BaseEntity y TenantEntity en Domain
- Configurar AuditInterceptor (setea created_at/by, modified_at/by automaticamente)
- Configurar DomainEventDispatcher interceptor
- Configurar Global Query Filters (soft delete + tenant isolation)
- Crear migracion inicial con tabla `families`
- Habilitar RLS en PostgreSQL + politica base
- Crear script SQL de setup de RLS (reutilizable por cada tabla nueva)

### 1.3 Autenticacion
- Configurar ASP.NET Core Identity (ApplicationUser)
- Implementar registro y login con email/password
- Implementar JWT (access token + refresh token)
- Implementar Google OAuth
- Implementar ICurrentUserService (userId, familyId del request)
- Implementar TenantResolutionMiddleware (setea app.current_tenant_id en PostgreSQL)
- ExceptionHandlingMiddleware

### 1.4 Familias y miembros
- Entidades: Family, FamilyMember (con roles: Admin/Member)
- Crear familia al registrarse (el primer usuario es Admin)
- CRUD miembros (invitar, asignar rol, desactivar)
- Flujo multi-familia: si un usuario pertenece a varias, endpoint para elegir familia activa y generar nuevo JWT
- Migracion + RLS para family_members

### 1.5 Frontend base
- Configurar React + Vite + TypeScript + Tailwind + shadcn/ui
- Configurar Axios con interceptors (JWT, refresh token, tenant header)
- Configurar TanStack Query (QueryClient, defaults)
- Configurar Zustand store (familia activa, moneda visualizacion)
- Configurar React Router con rutas protegidas
- Paginas: Login, Register, seleccion de familia (si multiples)
- Layout basico: sidebar + header + content area

**Entregable Fase 1:** Un usuario puede registrarse, loguearse, crear una familia y ver el layout de la app.

---

## Fase 2 – Configuracion del Tenant
> Monedas, modo bimonetario, categorias, subcategorias, centros de costo.

### 2.1 Monedas
- Entidad Currency (tenant-scoped)
- Catalogo de monedas globales (ISO 4217) como referencia
- CRUD monedas habilitadas por tenant
- Configuracion del tenant: moneda principal, bimonetario si/no, moneda secundaria
- Validacion: no se puede desactivar la moneda principal ni la secundaria si bimonetario esta activo
- Migracion + RLS

### 2.2 Categorias y subcategorias
- Entidades: Category, Subcategory (con todos los campos: tipo movimiento, tipo contable recomendado, centro costo recomendado, caracter recomendado)
- Subcategorias de sistema (protegidas, para ajustes de arqueo futuro)
- Seed data: categorias precargadas al crear un tenant
- CRUD con activar/desactivar
- Migracion + RLS

### 2.3 Centros de costo
- Entidad CostCenter (tenant-scoped)
- CRUD con activar/desactivar
- Migracion + RLS

### 2.4 Frontend Fase 2
- Pagina de configuracion de familia (monedas, bimonetario)
- ABM de categorias y subcategorias
- ABM de centros de costo
- Onboarding simplificado: al crear familia, el usuario pasa por config monedas → revision categorias → crear al menos una caja (Fase 3)

**Entregable Fase 2:** El tenant esta completamente configurado: monedas, categorias y centros de costo listos para empezar a cargar datos.

---

## Fase 3 – Cajas, Bancos y Tarjetas
> Las entidades donde se mueve el dinero.

### 3.1 Cajas
- Entidad Caja (single-currency, tenant-scoped)
- Entidad CajaPermission (Ver / Operar por usuario)
- CRUD caja + asignacion de permisos
- Logica de autorizacion en Application: verificar permisos antes de operar
- Migracion + RLS

### 3.2 Cuentas bancarias
- Entidad BankAccount (single-currency, tenant-scoped)
- Entidad BankAccountPermission
- CRUD + permisos (misma logica que cajas)
- Migracion + RLS

### 3.3 Tarjetas de credito
- Entidad CreditCard (nombre, cierre, vencimiento, estado)
- Entidad CreditCardMember (miembro de familia, ultimos 4 digitos, vencimiento plastico)
- Entidad BonificationType (nombre, tipo valor: importe/porcentaje)
- CRUD tarjetas + miembros + tipos de bonificacion
- Migracion + RLS

### 3.4 Frontend Fase 3
- ABM de cajas (con selector de moneda)
- ABM de cuentas bancarias
- ABM de tarjetas de credito + miembros
- ABM de tipos de bonificacion
- Gestion de permisos por caja/banco (asignar usuarios con Ver/Operar)
- Completar onboarding: despues de config monedas + categorias → crear cajas/bancos

**Entregable Fase 3:** El usuario tiene creadas las cajas, bancos y tarjetas donde va a registrar movimientos.

---

## Fase 4 – Movimientos (nucleo del sistema)
> Registro de ingresos y egresos con multiples formas de pago.

### 4.1 Movimientos
- Entidad Movement (con todos los campos: tipo, importe, moneda, exchange_rate, secondary_exchange_rate, amount_in_primary, amount_in_secondary, descripcion, categoria/subcategoria, tipo contable, caracter, centro de costo)
- Entidad MovementPayment (forma de pago: caja, banco o tarjeta de credito)
- Campos extra en MovementPayment cuando es tarjeta: credit_card_id, cc_member_id, installments, bonification_type_id, bonification_value
- Validaciones:
  - Importe > 0
  - Suma de formas de pago = importe total
  - Todas las formas de pago en la misma moneda que el movimiento
  - Exchange rate requerido si moneda != primaria
  - Secondary exchange rate requerido si tenant tiene bimonetario activo
  - Subcategoria compatible con tipo de movimiento (ingreso/egreso)
  - Permisos de caja/banco verificados
- Calculo automatico de amount_in_primary y amount_in_secondary
- Migracion + RLS

### 4.2 Compras con tarjeta de credito
- Domain Event: MovementCreatedEvent
- Event Handler: cuando un MovementPayment es con tarjeta de credito:
  - Crear CreditCardPurchase (compra registrada)
  - Crear CreditCardInstallment por cada cuota (con fecha estimada y monto)
  - Entidades: CreditCardPurchase, CreditCardInstallment
- Migracion + RLS

### 4.3 Reglas de edicion/borrado
- Solo el creador o Admin puede editar/borrar
- Soft delete (nunca borrado fisico)
- Validar que no hay restricciones de cierre (v1.1, pero preparar la estructura)

### 4.4 Frontend Fase 4
- Formulario de movimiento:
  - Selector de tipo (ingreso/egreso)
  - Importe + moneda
  - Exchange rate (si moneda != primaria, con sugerencia automatica si disponible)
  - Secondary exchange rate (si bimonetario, con sugerencia)
  - Categoria → subcategoria (filtrada segun tipo de movimiento)
  - Tipo contable (pre-cargado de subcategoria, editable)
  - Caracter (pre-cargado de subcategoria, editable)
  - Centro de costo (pre-cargado de subcategoria, editable)
  - Descripcion
  - Formas de pago (agregar multiples):
    - Tipo: caja / banco / tarjeta
    - Si caja/banco: selector de la entidad + importe
    - Si tarjeta: selector tarjeta → miembro → cuotas → bonificacion
- Lista de movimientos con filtros (fecha, tipo, categoria, caja/banco, tarjeta)
- Vista detalle de movimiento
- Edicion y borrado (con validacion de permisos)

**Entregable Fase 4:** El usuario puede registrar movimientos completos con multiples formas de pago, incluyendo pagos con tarjeta de credito que generan cuotas futuras. Este es el corazon del producto.

---

## Fase 5 – Transferencias
> Movimiento de dinero entre cajas y bancos propios.

### 5.1 Transferencias
- Entidad Transfer (origen, destino, importe, moneda, exchange_rate si cross-currency)
- MVP: auto-confirm (sin flujo de aprobacion)
- Validaciones:
  - Origen != destino
  - Permisos en origen Y destino (MVP: usuario debe tener Operar en ambos)
  - Si cross-currency: exchange rate requerido
- Impacto en saldos: debito en origen + credito en destino en la misma transaccion
- Migracion + RLS

### 5.2 Frontend Fase 5
- Formulario de transferencia: origen → destino → importe → cotizacion si aplica
- Lista de transferencias
- Indicador visual de transferencias cross-currency

**Entregable Fase 5:** El usuario puede mover dinero entre sus cajas y bancos.

---

## Fase 6 – Dashboard y Administracion
> Visualizacion de datos y gestion de la plataforma.

### 6.1 Dashboard
- Query: saldos actuales por caja y banco (respetando permisos del usuario)
- Query: deuda total de tarjetas de credito (suma de cuotas pendientes)
- Query: ingresos vs gastos ultimos 6 meses (agrupado por mes, en moneda elegida)
- Endpoint dashboard que consolida todo

### 6.2 Panel Super Admin
- Entidad Subscription (tenant_id, plan, status, trial_start, trial_end, bonification)
- Crear suscripcion trial automaticamente al crear familia
- CRUD tenants (listar, ver detalle, cambiar estado)
- Gestion de bonificaciones y periodos gratuitos
- Dashboard de metricas: cantidad de tenants, activos, en trial, vencidos

### 6.3 Frontend Fase 6
- Dashboard: tarjetas de saldo por caja/banco + grafico de barras ingresos vs gastos
- Selector de moneda de visualizacion (si bimonetario)
- Panel Super Admin (ruta separada /admin):
  - Lista de tenants con estado de suscripcion
  - Detalle de tenant
  - Acciones: bonificar, extender trial, cambiar estado

**Entregable Fase 6:** El producto MVP esta completo. El usuario ve su situacion financiera resumida y el Super Admin puede gestionar la plataforma.

---

## Resumen de fases y dependencias

```
Fase 1: Fundacion
  └── Fase 2: Configuracion del Tenant
        └── Fase 3: Cajas, Bancos y Tarjetas
              └── Fase 4: Movimientos ← nucleo del sistema
                    └── Fase 5: Transferencias
                          └── Fase 6: Dashboard y Admin
```

Cada fase depende de la anterior. No se puede paralelizar el orden de las fases, pero dentro de cada fase el backend y el frontend pueden avanzar en paralelo una vez que los endpoints estan definidos.

---

## Scope recordatorio: lo que NO esta en el MVP

Diferido a v1.1 (primera iteracion post-MVP):
- Arqueo de caja + cierre de ejercicio
- Asignacion multi-CC con porcentajes
- Transferencias con flujo de aprobacion
- Movimientos recurrentes (ingresos y egresos)
- Liquidacion de resumen de tarjeta de credito
- Dashboard personalizable (widgets seleccionables)
- Cash flow proyectado
- Onboarding guiado completo
- Alertas de vencimiento de plastico
- Notificaciones in-app
