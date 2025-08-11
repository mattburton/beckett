-- Beckett v0.24.0 - Single Global Reader Architecture

-- Step 0: Create new data types
CREATE TYPE beckett.stream_metadata AS
(
  stream_name text,
  category text,
  latest_position bigint,
  latest_global_position bigint,
  message_count bigint
);

CREATE TYPE beckett.message_metadata AS
(
  id uuid,
  global_position bigint,
  stream_name text,
  stream_position bigint,
  message_type_name text,
  category text,
  correlation_id text,
  tenant text,
  timestamp timestamp with time zone
);

-- Step 1: Create global reader position table
CREATE TABLE IF NOT EXISTS beckett.global_reader_position (
    position bigint NOT NULL DEFAULT 0,
    updated_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE ON beckett.global_reader_position TO beckett;

-- Insert initial record
INSERT INTO beckett.global_reader_position (position) VALUES (0);

-- Step 2: Create stream metadata table
CREATE TABLE IF NOT EXISTS beckett.stream_metadata (
    stream_name text NOT NULL PRIMARY KEY,
    category text NOT NULL,
    latest_position bigint NOT NULL,
    latest_global_position bigint NOT NULL,
    message_count bigint NOT NULL DEFAULT 1,
    first_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    last_updated_at timestamp with time zone DEFAULT now() NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_stream_metadata_category ON beckett.stream_metadata (category);
CREATE INDEX IF NOT EXISTS ix_stream_metadata_last_updated ON beckett.stream_metadata (last_updated_at DESC);

GRANT UPDATE, DELETE ON beckett.stream_metadata TO beckett;

-- Step 3: Create message metadata table (without data)
CREATE TABLE IF NOT EXISTS beckett.message_metadata (
    id uuid NOT NULL,
    global_position bigint NOT NULL,
    stream_name text NOT NULL,
    stream_position bigint NOT NULL,
    type text NOT NULL,
    category text NOT NULL,
    correlation_id text NULL,
    tenant text NULL,
    timestamp timestamp with time zone NOT NULL,
    PRIMARY KEY (global_position, id)
) PARTITION BY RANGE (global_position);

-- Create initial partition for active messages
CREATE TABLE IF NOT EXISTS beckett.message_metadata_active PARTITION OF beckett.message_metadata
    FOR VALUES FROM (0) TO (MAXVALUE);

-- Create indexes on the partition
CREATE INDEX IF NOT EXISTS ix_message_metadata_active_stream_type ON beckett.message_metadata_active (stream_name, type);
CREATE INDEX IF NOT EXISTS ix_message_metadata_active_category_type ON beckett.message_metadata_active (category, type);
CREATE INDEX IF NOT EXISTS ix_message_metadata_active_correlation_id ON beckett.message_metadata_active (correlation_id) 
    WHERE correlation_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_message_metadata_active_tenant ON beckett.message_metadata_active (tenant) 
    WHERE tenant IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_message_metadata_active_timestamp ON beckett.message_metadata_active (timestamp DESC);

GRANT UPDATE, DELETE ON beckett.message_metadata TO beckett;
GRANT UPDATE, DELETE ON beckett.message_metadata_active TO beckett;

-- Step 4: Create stream types lookup table for fast initialization
CREATE TABLE IF NOT EXISTS beckett.stream_types (
    stream_name text NOT NULL,
    message_type text NOT NULL,
    first_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    last_seen_at timestamp with time zone DEFAULT now() NOT NULL,
    message_count bigint NOT NULL DEFAULT 1,
    PRIMARY KEY (stream_name, message_type)
);

CREATE INDEX IF NOT EXISTS ix_stream_types_message_type ON beckett.stream_types (message_type);

GRANT UPDATE, DELETE ON beckett.stream_types TO beckett;

-- Step 5: Add subscription configuration columns to existing subscriptions table
ALTER TABLE beckett.subscriptions 
ADD COLUMN IF NOT EXISTS category text NULL,
ADD COLUMN IF NOT EXISTS stream_name text NULL,
ADD COLUMN IF NOT EXISTS message_types text[] NULL,
ADD COLUMN IF NOT EXISTS priority integer NOT NULL DEFAULT 2147483647,
ADD COLUMN IF NOT EXISTS skip_during_replay boolean NOT NULL DEFAULT false;

-- Add indexes for the new columns
CREATE INDEX IF NOT EXISTS ix_subscriptions_category ON beckett.subscriptions (category) WHERE category IS NOT NULL;

-- Step 6: Create normalized message_types table
CREATE TABLE IF NOT EXISTS beckett.message_types (
    id bigint PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    name text NOT NULL UNIQUE,
    created_at timestamp with time zone DEFAULT now() NOT NULL
);

GRANT UPDATE, DELETE ON beckett.message_types TO beckett;

-- Step 7: Create subscription_message_types junction table
CREATE TABLE IF NOT EXISTS beckett.subscription_message_types (
    subscription_id bigint NOT NULL,
    message_type_id bigint NOT NULL,
    PRIMARY KEY (subscription_id, message_type_id),
    FOREIGN KEY (subscription_id) REFERENCES beckett.subscriptions(id) ON DELETE CASCADE,
    FOREIGN KEY (message_type_id) REFERENCES beckett.message_types(id) ON DELETE CASCADE
);

GRANT UPDATE, DELETE ON beckett.subscription_message_types TO beckett;

-- Step 8: Update message_metadata to reference message_types by foreign key
ALTER TABLE beckett.message_metadata 
DROP COLUMN IF EXISTS type,
ADD COLUMN IF NOT EXISTS message_type_id bigint;

-- Add foreign key constraint and index
ALTER TABLE beckett.message_metadata 
ADD CONSTRAINT IF NOT EXISTS fk_message_metadata_message_type 
FOREIGN KEY (message_type_id) REFERENCES beckett.message_types(id);

CREATE INDEX IF NOT EXISTS ix_message_metadata_message_type_id ON beckett.message_metadata (message_type_id);

-- Step 9: Update stream_types to reference message_types by foreign key  
ALTER TABLE beckett.stream_types
DROP COLUMN IF EXISTS message_type,
ADD COLUMN IF NOT EXISTS message_type_id bigint;

ALTER TABLE beckett.stream_types 
ADD CONSTRAINT IF NOT EXISTS fk_stream_types_message_type 
FOREIGN KEY (message_type_id) REFERENCES beckett.message_types(id);

-- Update the primary key to use message_type_id
ALTER TABLE beckett.stream_types DROP CONSTRAINT IF EXISTS stream_types_pkey;
ALTER TABLE beckett.stream_types ADD PRIMARY KEY (stream_name, message_type_id);

CREATE INDEX IF NOT EXISTS ix_stream_types_message_type_id ON beckett.stream_types (message_type_id);