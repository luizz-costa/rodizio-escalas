   #!/bin/bash
   # Roda uma única vez, quando o volume do PostgreSQL ainda está vazio
   # (recurso da imagem oficial: pasta /docker-entrypoint-initdb.d).
   # Cria a base do Data Warehouse e aplica o modelo dimensional.
   set -e

   psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
        -c "CREATE DATABASE rodizio_dw;"

   psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname rodizio_dw \
        -f /scripts/modelo-dimensional.sql

   echo "Data Warehouse rodizio_dw criado."
