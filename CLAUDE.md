# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MyFO is a family financial organization SaaS (ERP for families). Multi-tenant architecture where each family is a tenant. Spanish-first UI with i18n support.

## Commands

### Backend (.NET 10)

```bash
# Run API (http://localhost:5100)
cd src/backend && dotnet run --project MyFO.API

# Build
cd src/backend && dotnet build MyFO.slnx

# Add EF Core migration
cd src/backend && dotnet ef migrations add MigrationName -p MyFO.Infrastructure -s MyFO.API

# Apply migrations
cd src/backend && dotnet ef database update -s MyFO.API

# Revert last migration
cd src/backend && dotnet ef migrations remove -p MyFO.Infrastructure -s MyFO.API
```

### Frontend (React 19 + Vite)

```bash
cd src/frontend

npm run dev      # Dev server on http://localhost:3000 (proxies /api → :5100)
npm run build    # TypeScript check + Vite production build
npm run lint     # ESLint
```

No test suite exists yet.

## Architecture

**Clean Architecture** with 4 .NET projects:

```
API → Application → Domain (zero deps)
Infrastructure → Application, Domain
```

- **Domain**: Entities, interfaces, domain events. No external dependencies.
- **Application**: CQRS via MediatR (Command/Query + Handler pairs). FluentValidation. DTOs with Mapster.
- **Infrastructure**: EF Core + PostgreSQL, ASP.NET Identity + JWT, interceptors, services.
- **API**: Controllers that delegate to MediatR. ExceptionHandlingMiddleware maps domain exceptions to HTTP codes.

### Multi-Tenancy

- Every tenant-scoped entity extends `TenantEntity` (which adds `FamilyId`).
- EF Core global query filters auto-apply `FamilyId` + `DeletedAt == null` on all queries.
- PostgreSQL RLS via `myfo_app` user enforces tenant isolation at DB level.
- Two connection strings: `DefaultConnection` (postgres, for migrations) and `AppConnection` (myfo_app, for runtime with RLS).
- `TenantConnectionInterceptor` sets `app.current_family_id` and `app.current_user_id` session vars on every connection open.

### Key Patterns

- **PKs**: Composite keys `(family_id, entity_id)` for tenant entities.
- **Soft delete**: `DeletedAt` field (not `IsDeleted`). Global query filters handle exclusion. RLS does NOT filter by `deleted_at`.
- **Domain events**: Synchronous via MediatR `DomainEventNotification<T>` wrapper, dispatched in `DomainEventDispatcher` interceptor.
- **Audit**: `AuditInterceptor` auto-sets `created_at/by`, `modified_at/by`, `deleted_at/by`.
- **Auth flow**: Login uses `IgnoreQueryFilters()` + manual SET for RLS vars. Register sets `app.current_family_id` before INSERT.
- **Commands with many params**: Use `class` (not `record`) for proper JSON deserialization.

### Frontend Structure

- **Routing**: `App.tsx` with guards (`PublicRoute`, `AuthenticatedRoute`, `FamilyRequiredRoute`, `FamilyAdminRoute`, `AdminRoute`).
- **State**: Zustand stores + TanStack Query for server state + React Context for Auth/Theme.
- **Forms**: React Hook Form + Zod validation.
- **UI**: shadcn/ui (Base UI, NOT Radix) + Tailwind CSS v4. Sonner for toasts. Lucide for icons.
- **HTTP**: Axios with JWT interceptor (`src/lib/api-client.ts`).
- **i18n**: i18next, Spanish default. Path alias `@/` → `src/`.

## Naming Conventions

- **Code**: English. C# PascalCase, DB snake_case.
- **UI text**: Spanish (default), all user-facing strings via i18n.
- **PKs**: `entity_id` pattern (e.g., `movement_id`, `cash_box_id`).
- **Dates**: UTC in DB.

## Business Rules

Detailed rules are in root-level files:
- `REGLAS_MOVIMIENTOS.md` — Movement creation/edit/delete rules
- `REGLAS_ENTIDADES.md` — Validation rules for CashBox, BankAccount, CreditCard
- `REGLAS_UI_UX.md` — UI input rules (numeric fields, maxlength, required validation)
- `REGLAS_COTIZACIONES.md` — Exchange rate logic for dual-currency mode

Domain specs live in `documentation/domains/{domain}/functional-spec.md`.

## Database

PostgreSQL 18 (local, no Docker). Two users:
- `postgres` — superuser for migrations
- `myfo_app` — non-superuser for runtime (RLS enforced)

RLS setup script: `src/backend/MyFO.Infrastructure/Persistence/Scripts/001_setup_rls.sql`

Migrations: `src/backend/MyFO.Infrastructure/Persistence/Migrations/`
