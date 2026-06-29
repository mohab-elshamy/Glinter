-- Run once per database with a role allowed to install trusted extensions.
-- Application migrations intentionally do not create extensions.
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm'
    ) THEN
        RAISE EXCEPTION 'pg_trgm extension installation failed';
    END IF;
END
$$;
