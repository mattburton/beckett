-- Beckett v0.18.10 - subscription initialization fixes and improvements
ALTER TYPE beckett.checkpoint ADD ATTRIBUTE stream_position bigint;

CREATE OR REPLACE FUNCTION beckett.record_checkpoints(
  _checkpoints beckett.checkpoint[]
)
  RETURNS void
  LANGUAGE sql
AS
$$
INSERT INTO beckett.checkpoints (stream_version, stream_position, group_name, name, stream_name)
SELECT c.stream_version, c.stream_position, c.group_name, c.name, c.stream_name
FROM unnest(_checkpoints) c
ON CONFLICT (group_name, name, stream_name) DO UPDATE
  SET stream_version = excluded.stream_version;
$$;
