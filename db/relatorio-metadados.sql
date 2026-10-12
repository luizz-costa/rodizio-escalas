-- =====================================================================
-- Relatório de metadados do Data Warehouse – Gestão de Escalas
-- Lê o catálogo do PostgreSQL e lista, para cada coluna: tabela, coluna,
-- tipo, se aceita vazio, chave primária, chave estrangeira e descrição.
--
-- Como usar:
--   psql -d rodizio_dw -f db/relatorio-metadados.sql
-- =====================================================================

SELECT
    c.table_name                                       AS tabela,
    c.column_name                                      AS coluna,
    CASE
        WHEN c.data_type = 'character varying' THEN 'varchar(' || c.character_maximum_length || ')'
        ELSE c.data_type
    END                                                AS tipo,
    CASE WHEN c.is_nullable = 'YES' THEN 'Sim' ELSE 'Não' END AS aceita_vazio,
    CASE WHEN pk.column_name IS NOT NULL THEN 'Sim' ELSE '' END AS chave_primaria,
    COALESCE(fk.tabela_ref || '.' || fk.coluna_ref, '') AS chave_estrangeira,
    col_description(format('%I', c.table_name)::regclass, c.ordinal_position) AS descricao
FROM information_schema.columns c
LEFT JOIN (
    SELECT kcu.table_name, kcu.column_name
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu
      ON kcu.constraint_name = tc.constraint_name AND kcu.table_schema = tc.table_schema
    WHERE tc.constraint_type = 'PRIMARY KEY' AND tc.table_schema = 'public'
) pk ON pk.table_name = c.table_name AND pk.column_name = c.column_name
LEFT JOIN (
    SELECT kcu.table_name, kcu.column_name,
           ccu.table_name AS tabela_ref, ccu.column_name AS coluna_ref
    FROM information_schema.table_constraints tc
    JOIN information_schema.key_column_usage kcu
      ON kcu.constraint_name = tc.constraint_name AND kcu.table_schema = tc.table_schema
    JOIN information_schema.constraint_column_usage ccu
      ON ccu.constraint_name = tc.constraint_name AND ccu.table_schema = tc.table_schema
    WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_schema = 'public'
) fk ON fk.table_name = c.table_name AND fk.column_name = c.column_name
WHERE c.table_schema = 'public'
ORDER BY
    CASE WHEN c.table_name LIKE 'fato%' THEN 1 ELSE 0 END,
    c.table_name,
    c.ordinal_position;
