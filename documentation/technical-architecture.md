# MyFO – Arquitectura Tecnica
*Version 1.4 – Marzo 2026*

---

## 1. Stack Tecnologico

| Capa | Tecnologia | Justificacion |
|---|---|---|
| Frontend | React + Vite + TypeScript | SPA moderna, tipado fuerte |
| UI Components | shadcn/ui + Tailwind CSS | Componentes en tu codebase, ideal para Claude Code |
| Graficos | Recharts | Liviano, declarativo, bien soportado |
| State (server) | TanStack Query (React Query) | Cache, sincronizacion, loading/error states |
| State (cliente) | Zustand | Simple, liviano (familia activa, moneda elegida, etc.) |
| Formularios | React Hook Form + Zod | Validacion tipada en cliente |
| Routing | React Router v6 | Estandar de la industria |
| Backend | .NET 10 + ASP.NET Core | Clean Architecture |
| ORM | Entity Framework Core | Con PostgreSQL provider |
| CQRS | MediatR | Commands / Queries / Handlers / Domain Events |
| Validacion | FluentValidation | Validaciones en capa Application |
| Mapeo | Mapster | DTO mapping performante |
| Base de datos | PostgreSQL 16 | RLS nativo, robusto, open source |
| Autenticacion | ASP.NET Core Identity + JWT | Access token + Refresh token |
| OAuth | Google (via Identity) | Login social |
| Hosting | Railway | Simple, sin DevOps, escala cuando sea necesario |

---

## 2. Convenciones Generales

### 2.1 Lenguaje

| Contexto | Idioma |
|---|---|
| Codigo fuente (clases, metodos, variables) | Ingles |
| Tablas y columnas de base de datos | Ingles (snake_case) |
| Documentacion tecnica | Ingles |
| UI de la aplicacion | Multilenguaje (español por defecto) |

### 2.2 Naming

| Elemento | Convencion | Ejemplo |
|---|---|---|
| Tablas BD | snake_case | `credit_cards` |
| Columnas BD | snake_case | `family_id`, `created_at` |
| PKs de entidades | prefijo de entidad + `_id` | `user_id`, `movement_id` |
| Clases/Propiedades/Metodos C# | PascalCase | `CreateMovementCommand` |
| Variables locales C# | camelCase | `movementId` |
| Componentes React | PascalCase | `MovementForm` |
| Variables/funciones JS/TS | camelCase | `fetchMovements` |

### 2.3 Fechas y Horas

- Todas las fechas y horas se almacenan en **UTC** en la base de datos
- El frontend (React) convierte UTC al timezone local del usuario para mostrar

---

## 3. Arquitectura Backend – Clean Architecture

### 3.1 Principio

Clean Architecture clasica con 4 proyectos. Organizacion interna por feature/dominio usando carpetas. Los Domain Events se ejecutan de forma sincronica dentro de la misma transaccion via MediatR.

Cuando el producto crezca y los boundaries de dominio esten claros, se pueden extraer modulos reales (Modular Monolith) sin cambiar la logica.

### 3.2 Reglas de dependencia

```
API              --> Application
Application      --> Domain
Infrastructure   --> Application, Domain
Domain           --> nada (cero dependencias externas)
```

### 3.3 Estructura del repositorio

```
MyFO/
├── src/
│   ├── backend/
│   │   ├── MyFO.Domain/
│   │   ├── MyFO.Application/
│   │   ├── MyFO.Infrastructure/
│   │   └── MyFO.API/
│   └── frontend/
│       └── ...
├── documentation/
└── docker-compose.yml
```

### 3.4 MyFO.Domain

Contiene entidades de negocio, value objects, interfaces, enums, domain events y excepciones. Sin dependencias externas.

```
MyFO.Domain/
├── Common/
│   ├── BaseEntity.cs
│   ├── TenantEntity.cs
│   ├── IDomainEvent.cs
│   └── ValueObject.cs
├── Identity/
│   ├── Family.cs
│   ├── FamilyMember.cs
│   └── Enums/
│       └── UserRole.cs
├── Accounting/
│   ├── Movement.cs
│   ├── MovementPayment.cs
│   ├── Category.cs
│   ├── Subcategory.cs
│   ├── CostCenter.cs
│   ├── ValueObjects/
│   │   ├── Money.cs
│   │   └── ExchangeRate.cs
│   ├── Enums/
│   │   ├── MovementType.cs              # Income / Expense
│   │   ├── AccountingType.cs            # Asset / Liability / Income / Expense
│   │   ├── MovementCharacter.cs         # Ordinary / Extraordinary
│   │   └── SubcategoryDirection.cs      # Income / Expense / Both
│   └── Events/
│       └── MovementCreatedEvent.cs
├── Transactions/
│   ├── Caja.cs
│   ├── CajaPermission.cs
│   ├── BankAccount.cs
│   ├── BankAccountPermission.cs
│   ├── Transfer.cs
│   ├── Currency.cs
│   └── Enums/
│       └── TransferStatus.cs
├── CreditCards/
│   ├── CreditCard.cs
│   ├── CreditCardMember.cs
│   ├── CreditCardPurchase.cs
│   ├── CreditCardInstallment.cs
│   ├── BonificationType.cs
│   └── Enums/
│       └── BonificationValueType.cs     # Amount / Percentage
├── Interfaces/
│   ├── Repositories/
│   │   ├── IRepository.cs
│   │   ├── IMovementRepository.cs
│   │   ├── ICajaRepository.cs
│   │   └── ...
│   └── Services/
│       ├── ICurrentUserService.cs
│       └── ICurrencyRateService.cs
└── Exceptions/
    ├── DomainException.cs
    ├── NotFoundException.cs
    └── ForbiddenException.cs
```

**BaseEntity.cs:**
```csharp
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public Guid? ModifiedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public bool IsDeleted => DeletedAt.HasValue;

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void AddDomainEvent(IDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
```

Nota: Las PKs especificas (`MovementId`, `CajaId`, etc.) se definen en cada entidad concreta, no en BaseEntity. En BD se mapean como PKs compuestas `(tenant_id, entity_id)`.

### 3.5 MyFO.Application

Casos de uso organizados por feature (CQRS).

```
MyFO.Application/
├── Common/
│   ├── Behaviours/
│   │   ├── ValidationBehaviour.cs
│   │   └── LoggingBehaviour.cs
│   ├── Mappings/
│   │   └── MappingConfig.cs
│   └── Models/
│       └── PaginatedResult.cs
├── Identity/
│   ├── Commands/
│   │   ├── RegisterUser/
│   │   ├── LoginUser/
│   │   └── CreateFamily/
│   ├── Queries/
│   │   └── GetFamilyMembers/
│   └── DTOs/
├── Accounting/
│   ├── Commands/
│   │   ├── CreateMovement/
│   │   │   ├── CreateMovementCommand.cs
│   │   │   ├── CreateMovementHandler.cs
│   │   │   └── CreateMovementValidator.cs
│   │   ├── UpdateMovement/
│   │   └── DeleteMovement/
│   ├── Queries/
│   │   ├── GetMovements/
│   │   └── GetMovementById/
│   ├── EventHandlers/
│   │   └── MovementCreatedHandler.cs
│   └── DTOs/
├── Transactions/
│   ├── Commands/
│   ├── Queries/
│   └── DTOs/
├── CreditCards/
│   ├── Commands/
│   ├── Queries/
│   ├── EventHandlers/
│   │   └── OnMovementCreated_RegisterCreditCardPurchase.cs
│   └── DTOs/
├── Dashboard/
│   └── Queries/
│       ├── GetBalances/
│       └── GetIncomeVsExpenses/
└── Administration/
    ├── Commands/
    ├── Queries/
    └── DTOs/
```

### 3.6 MyFO.Infrastructure

```
MyFO.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   ├── Identity/
│   │   │   ├── FamilyConfiguration.cs
│   │   │   └── FamilyMemberConfiguration.cs
│   │   ├── Accounting/
│   │   │   ├── MovementConfiguration.cs
│   │   │   └── CategoryConfiguration.cs
│   │   ├── Transactions/
│   │   │   ├── CajaConfiguration.cs
│   │   │   └── TransferConfiguration.cs
│   │   └── CreditCards/
│   │       └── CreditCardConfiguration.cs
│   ├── Migrations/
│   ├── Repositories/
│   ├── Interceptors/
│   │   ├── AuditInterceptor.cs         # Setea created/modified/deleted automaticamente
│   │   └── DomainEventDispatcher.cs    # Despacha domain events al guardar
│   └── Seeds/
│       └── CategorySeedData.cs         # Categorias precargadas
├── Services/
│   ├── CurrencyRateService.cs
│   └── CurrentUserService.cs
├── Identity/
│   └── JwtService.cs
└── DependencyInjection.cs
```

**ApplicationDbContext – Tenant isolation + Soft delete:**
```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    // Aplica todas las configuraciones de entidad
    builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

    // Global query filters: soft delete + tenant isolation
    foreach (var entityType in builder.Model.GetEntityTypes())
    {
        if (typeof(TenantEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Filtra por tenant Y soft delete a nivel EF Core
            // Esto se combina con RLS de PostgreSQL como doble seguridad
        }
        else if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
        {
            // Solo soft delete
        }
    }
}
```

### 3.7 MyFO.API

```
MyFO.API/
├── Controllers/
│   ├── AuthController.cs
│   ├── FamiliesController.cs
│   ├── CajasController.cs
│   ├── BankAccountsController.cs
│   ├── TransfersController.cs
│   ├── CategoriesController.cs
│   ├── CostCentersController.cs
│   ├── MovementsController.cs
│   ├── CreditCardsController.cs
│   ├── DashboardController.cs
│   └── Admin/
│       └── TenantsController.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── TenantResolutionMiddleware.cs
└── Program.cs
```

---

## 4. Base de Datos – Multi-Tenancy y Seguridad

### 4.1 Primary Keys compuestas en tablas con scope de tenant

Todas las tablas con scope de familia usan **PKs compuestas**: `(tenant_id, entity_id)`.

```sql
CREATE TABLE movements (
    tenant_id   UUID        NOT NULL,
    movement_id UUID        NOT NULL,
    -- ... resto de columnas
    PRIMARY KEY (tenant_id, movement_id)
);
```

Las foreign keys entre tablas de tenant **siempre incluyen `tenant_id`**:

```sql
ALTER TABLE movement_payments
    ADD CONSTRAINT fk_movement
    FOREIGN KEY (tenant_id, movement_id)
    REFERENCES movements (tenant_id, movement_id);
```

### 4.2 Row Level Security (RLS) en PostgreSQL

RLS se habilita en **todas las tablas con `tenant_id`**. Las politicas:
1. Filtran por el tenant del usuario conectado
2. **Filtran soft deletes** (`deleted_at IS NULL`)

```sql
ALTER TABLE movements ENABLE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON movements
    USING (
        tenant_id = current_setting('app.current_tenant_id')::uuid
        AND deleted_at IS NULL
    );
```

La aplicacion setea `app.current_tenant_id` al inicio de cada request:
```sql
SET LOCAL app.current_tenant_id = 'uuid-de-la-familia';
```

**Doble capa de seguridad:**
- Capa aplicacion: EF Core global query filters
- Capa base de datos: RLS policies de PostgreSQL

### 4.3 Auditoria

```sql
created_at    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
created_by    UUID         NOT NULL,
modified_at   TIMESTAMPTZ,
modified_by   UUID,
deleted_at    TIMESTAMPTZ,
deleted_by    UUID
```

### 4.4 Almacenamiento de importes y cotizaciones

Un movimiento es siempre en **una sola moneda**. Se almacenan dos exchange rates.

```sql
amount                    DECIMAL(18,2)   -- Importe en moneda del movimiento
currency_code             VARCHAR(3)      -- ISO 4217 (ARS, USD, EUR...)
exchange_rate             DECIMAL(18,6)   -- Unidades de moneda primaria por 1 unidad
                                          -- de la moneda del movimiento.
                                          -- NULL si la moneda del movimiento = primaria.
secondary_exchange_rate   DECIMAL(18,6)   -- Unidades de moneda primaria por 1 unidad
                                          -- de la moneda secundaria.
                                          -- NULL si el tenant no tiene modo bimonetario.
amount_in_primary         DECIMAL(18,2)   -- Calculado: amount * exchange_rate (o = amount si primaria)
amount_in_secondary       DECIMAL(18,2)   -- Calculado: amount_in_primary / secondary_exchange_rate
                                          -- NULL si no hay modo bimonetario.
```

**Ejemplo:** ARS (primaria), USD (secundaria), cotizacion 1 USD = 1500 ARS:

| Movimiento | exchange_rate | secondary_exchange_rate | amount_in_primary | amount_in_secondary |
|---|---|---|---|---|
| 15000 ARS | NULL | 1500 | 15000 | 10 USD |
| 100 USD | 1500 | 1500 | 150000 | 100 USD |
| 50 EUR | 1600 | 1500 | 80000 | 53.33 USD |

**Todas las formas de pago de un movimiento deben estar en la misma moneda que el movimiento.** No se almacenan exchange rates en `movement_payments`.

### 4.5 Diagrama de entidades principales

```
families (no tenant-scoped, es el tenant en si)
  ├── family_members              (tenant_id, member_id)
  ├── currencies                  (tenant_id, currency_id)
  ├── cajas                       (tenant_id, caja_id)
  │   └── caja_permissions        (tenant_id, caja_id, user_id)
  ├── bank_accounts               (tenant_id, bank_account_id)
  │   └── bank_account_permissions
  ├── categories                  (tenant_id, category_id)
  │   └── subcategories           (tenant_id, subcategory_id)
  ├── cost_centers                (tenant_id, cost_center_id)
  ├── movements                   (tenant_id, movement_id)
  │   └── movement_payments       (tenant_id, payment_id)
  ├── transfers                   (tenant_id, transfer_id)
  ├── credit_cards                (tenant_id, credit_card_id)
  │   ├── credit_card_members     (tenant_id, cc_member_id)
  │   └── credit_card_purchases   (tenant_id, purchase_id)
  │       └── credit_card_installments (tenant_id, installment_id)
  ├── bonification_types          (tenant_id, bonification_type_id)
  ├── caja_closings               (tenant_id, closing_id)              [v1.1]
  ├── fiscal_year_closings        (tenant_id, closing_id)              [v1.1]
  ├── recurring_movements         (tenant_id, recurring_movement_id)   [v1.1]
  ├── credit_card_statements      (tenant_id, statement_id)            [v1.1]
  │   └── credit_card_payments    (tenant_id, payment_id)              [v1.1]
  │       └── credit_card_payment_methods (tenant_id, method_id)       [v1.1]
  └── counterparts                (tenant_id, counterpart_id)          [v2]
      └── loans                   (tenant_id, loan_id)                 [v2]
```

---

## 5. Autenticacion y Resolucion de Tenant

### 5.1 Flujo

1. Usuario se loguea (email/password o Google OAuth)
2. Si pertenece a una sola familia: JWT incluye `familyId`
3. Si pertenece a varias: JWT sin `familyId` → elige familia → solicita token de familia
4. Cada request setea `app.current_tenant_id` en PostgreSQL via middleware
5. RLS hace el resto automaticamente

### 5.2 JWT Claims

```json
{
  "sub": "user-uuid",
  "email": "usuario@email.com",
  "role": "FamilyAdmin",
  "familyId": "family-uuid",
  "exp": 1234567890
}
```

### 5.3 Autorizacion por caja

Verificada en capa Application (no en controllers). Cada handler que afecta una caja consulta los permisos del usuario actual sobre esa caja.

---

## 6. Domain Events

Domain Events se ejecutan **de forma sincronica dentro de la misma transaccion**. Se despachan automaticamente al llamar `SaveChangesAsync` via el `DomainEventDispatcher` interceptor.

**Ejemplo:** Cuando se crea un movimiento con forma de pago tarjeta:

```
1. CreateMovementHandler guarda el Movement con su MovementPayment (tipo CC)
2. La entidad Movement agrega MovementCreatedEvent
3. DomainEventDispatcher encuentra el evento y ejecuta handlers registrados
4. OnMovementCreated_RegisterCreditCardPurchase:
   → Crea CreditCardPurchase + CreditCardInstallments
5. Todo se commitea en una sola transaccion
```

Si cualquier handler falla, toda la transaccion se revierte. Simple, predecible, debuggeable.

---

## 7. Arquitectura Frontend

### 7.1 Estructura de carpetas

```
frontend/src/
├── app/
│   ├── router.tsx
│   ├── providers.tsx
│   └── App.tsx
├── features/
│   ├── auth/
│   ├── dashboard/
│   ├── accounting/                # Movimientos, categorias, centros de costo
│   ├── transactions/              # Cajas, bancos, transferencias
│   ├── credit-cards/
│   ├── families/
│   └── admin/
├── components/
│   ├── ui/                        # shadcn/ui (generados)
│   ├── layout/
│   └── shared/
├── hooks/
├── stores/
│   └── appStore.ts                # Zustand: familia activa, moneda elegida
├── lib/
│   ├── api.ts                     # Axios configurado
│   ├── utils.ts
│   └── formatters.ts              # Moneda, fechas
└── types/
```

### 7.2 Estructura interna de cada feature

```
features/accounting/
├── api/
│   └── movements.api.ts           # Llamadas HTTP + React Query hooks
├── components/
│   ├── MovementList.tsx
│   └── MovementForm.tsx
├── schemas/
│   └── movement.schema.ts         # Zod
└── pages/
    └── MovementsPage.tsx
```

### 7.3 Manejo de estado

| Estado | Herramienta |
|---|---|
| Datos del servidor | TanStack Query |
| Estado global UI | Zustand |
| Formularios | React Hook Form |
| Estado local | useState |

---

## 8. API REST – Convenciones

### 8.1 URLs

```
/api/v1/
  auth/login
  auth/register
  auth/refresh
  auth/google

  families/
    {familyId}/
      members/
      currencies/
      cajas/
        {cajaId}/permissions/
      bank-accounts/
      transfers/
      categories/
        {categoryId}/subcategories/
      cost-centers/
      movements/
      credit-cards/
        {cardId}/members/
        {cardId}/purchases/
      bonification-types/
      dashboard/

  admin/
    tenants/
    subscriptions/
```

### 8.2 Respuestas estandar

```json
// Exito lista
{ "data": [...], "meta": { "page": 1, "pageSize": 20, "total": 100 } }

// Exito objeto
{ "data": { ... } }

// Error
{ "error": { "code": "CLOSED_PERIOD", "message": "...", "details": {} } }
```

---

## 9. Entorno de Desarrollo Local

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: myfo_dev
      POSTGRES_USER: myfo
      POSTGRES_PASSWORD: myfo_local
    ports:
      - "5432:5432"
```

---

## 10. Proximos Pasos – MVP (orden sugerido)

1. Crear estructura de solucion .NET (4 proyectos) y proyecto React
2. Configurar PostgreSQL + EF Core + migraciones base + RLS
3. Auth: Identity + JWT + Google OAuth + multitenant middleware
4. Familias y miembros (CRUD + invitaciones)
5. Monedas (CRUD por tenant + config bimonetaria)
6. Cajas + permisos (CRUD + Ver/Operar)
7. Cuentas bancarias + permisos
8. Categorias y subcategorias (CRUD + seed data precargado)
9. Centros de costo (CRUD, asignacion simple)
10. Movimientos (CRUD con multiples formas de pago)
11. Tarjetas de credito (entidad + miembros + registro de compras con cuotas)
12. Transferencias (auto-confirm)
13. Dashboard basico (saldos + grafico ingresos vs gastos)
14. Panel Super Admin basico (tenants + suscripciones)
15. Frontend: estructura base + auth + features en orden
