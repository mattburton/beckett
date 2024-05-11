cat src/Beckett/Database/Migrations/*.sql | sed "s/__schema__/$1/g" > "$2"
