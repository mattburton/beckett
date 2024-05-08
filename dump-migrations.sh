sed "s/__schema__/$1/g" cat src/Beckett/Database/Migrations/*.sql > $2
