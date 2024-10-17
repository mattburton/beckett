#!/usr/bin/env bash

docker exec postgres pg_dump -U postgres --schema-only --no-privileges --no-owner --schema beckett > ./db/schema.sql
