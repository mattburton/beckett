#!/usr/bin/env bash

cat ./db/migrations/*.sql | sed "s/__schema__/$1/g" > "$2"
