-- init.sql: Runs on first container boot for database-level settings only.
-- Schema tables and seed data are managed by application migrations
-- in src/IdentityService.Infrastructure/Migrations/.

DO $$
BEGIN
    EXECUTE format('ALTER DATABASE %I SET timezone TO ''UTC''', current_database());
END $$;

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS pgcrypto;
