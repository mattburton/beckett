-- ensure process_at is set to NULL when releasing a checkpoint reservation
CREATE OR REPLACE FUNCTION beckett.release_checkpoint_reservation(
  _id bigint
)
  RETURNS void
  LANGUAGE plpgsql
AS
$$
BEGIN
  UPDATE beckett.checkpoints
  SET process_at = NULL,
      reserved_until = NULL
  WHERE id = _id;
END;
$$;
