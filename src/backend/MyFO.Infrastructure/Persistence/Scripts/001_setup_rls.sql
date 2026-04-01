-- ============================================================
-- MyFO: Row-Level Security (RLS) Setup
-- ============================================================
-- Run this as postgres (superuser) AFTER migrations.
-- This script is idempotent (safe to re-run).
--
-- What it does:
--   1. Creates a non-superuser role 'myfo_app' for the application
--   2. Grants DML permissions on all schemas/tables/sequences
--   3. Creates a helper function current_family_id() that reads
--      the session variable set by the application
--   4. Enables RLS on all tenant-scoped tables
--   5. Creates policies that restrict access by family_id
--
-- Schemas: public (Identity), cmn (global), cfg (config), txn (transactions)
-- ============================================================

-- 1. Application role (non-superuser, so RLS applies)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'myfo_app') THEN
        CREATE ROLE myfo_app WITH LOGIN PASSWORD 'MyFO_App_2024';
    END IF;
END
$$;

-- Create schemas if they don't exist
CREATE SCHEMA IF NOT EXISTS cmn;
CREATE SCHEMA IF NOT EXISTS cfg;
CREATE SCHEMA IF NOT EXISTS txn;

GRANT CONNECT ON DATABASE myfo TO myfo_app;

-- Grant permissions on all schemas
DO $$
DECLARE
    s TEXT;
    schemas TEXT[] := ARRAY['public', 'cmn', 'cfg', 'txn'];
BEGIN
    FOREACH s IN ARRAY schemas LOOP
        EXECUTE format('GRANT USAGE ON SCHEMA %I TO myfo_app', s);
        EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO myfo_app', s);
        EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO myfo_app', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO myfo_app', s);
        EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO myfo_app', s);
    END LOOP;
END
$$;

-- 2. Helper function: reads session variable set by TenantConnectionInterceptor
CREATE OR REPLACE FUNCTION current_family_id()
RETURNS uuid AS $$
BEGIN
    RETURN NULLIF(current_setting('app.current_family_id', true), '')::uuid;
EXCEPTION
    WHEN OTHERS THEN RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE;

-- 3. Enable RLS + create policies on all tenant-scoped tables
-- Policy logic:
--   USING  = filter for SELECT/UPDATE/DELETE (which rows you can see)
--   WITH CHECK = filter for INSERT/UPDATE (which rows you can write)

-- 3b. Special policy for login: allows SELECT on family_members by user_id.
-- At login time, family_id is not known yet. This policy lets the app
-- look up "which families does this user belong to?" after password verification.
-- Safe because user_id is verified by ASP.NET Identity password check first.
DROP POLICY IF EXISTS user_membership_lookup ON cfg.family_members;
CREATE POLICY user_membership_lookup ON cfg.family_members
    FOR SELECT
    USING (user_id = NULLIF(current_setting('app.current_user_id', true), '')::uuid);

DO $$
DECLARE
    tbl TEXT;
    tenant_tables TEXT[] := ARRAY[
        'cfg.family_members',
        'cfg.categories',
        'cfg.subcategories',
        'cfg.cost_centers',
        'cfg.family_currencies',
        'cfg.cash_boxes',
        'cfg.bank_accounts',
        'cfg.cash_box_permissions',
        'cfg.credit_cards',
        'cfg.credit_card_members',
        'txn.movements',
        'txn.movement_payments',
        'txn.transfers',
        'txn.frequent_movements',
        'txn.credit_card_installments',
        'txn.credit_card_payments',
        'txn.statement_periods',
        'txn.statement_line_items',
        'txn.statement_payment_allocations'
    ];
BEGIN
    FOREACH tbl IN ARRAY tenant_tables LOOP
        -- Skip tables that don't exist yet (created by later migrations)
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.tables
            WHERE table_schema || '.' || table_name = tbl
        ) THEN
            RAISE NOTICE 'Skipping % — table does not exist yet', tbl;
            CONTINUE;
        END IF;

        EXECUTE format('ALTER TABLE %s ENABLE ROW LEVEL SECURITY', tbl);

        -- Drop old combined policy
        EXECUTE format('DROP POLICY IF EXISTS tenant_isolation ON %s', tbl);

        -- Tenant isolation: RLS only enforces family_id check.
        -- Soft delete filtering (deleted_at IS NULL) is handled by EF Core
        -- global query filters, NOT by RLS. This avoids conflicts when
        -- soft-deleting rows (UPDATE SET deleted_at = ...) because PostgreSQL
        -- re-checks all applicable policies against the new row version.
        EXECUTE format('DROP POLICY IF EXISTS tenant_select ON %s', tbl);
        EXECUTE format('DROP POLICY IF EXISTS tenant_insert ON %s', tbl);
        EXECUTE format('DROP POLICY IF EXISTS tenant_update ON %s', tbl);
        EXECUTE format('DROP POLICY IF EXISTS tenant_delete ON %s', tbl);
        EXECUTE format(
            'CREATE POLICY tenant_isolation ON %s
             USING (family_id = current_family_id())
             WITH CHECK (family_id = current_family_id())',
            tbl
        );
    END LOOP;
END
$$;
