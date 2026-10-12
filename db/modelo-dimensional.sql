-- =====================================================================
-- Data Warehouse – Gestão de Escalas
-- Modelo dimensional v3: 2 fatos e 5 dimensões, esquema estrela.
-- Grão mensal: cada linha das fatos resume um mês.
-- Banco: PostgreSQL 16, numa base separada do banco do sistema.
-- Documentação: docs/modelo-de-dados.md
--
-- Como usar (base vazia):
--   createdb rodizio_dw
--   psql -d rodizio_dw -f db/modelo-dimensional.sql
--
-- Este script cria a estrutura, as descrições de cada coluna (metadados),
-- as listas fixas (situações e motivos de troca) e os meses de 2026 a 2030.
-- Quem preenche funcionários, escalas e fatos é o ETL [PREVISTO].
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- Dimensões
-- ---------------------------------------------------------------------

CREATE TABLE dim_tempo (
    id_mes       INTEGER      PRIMARY KEY,
    mes          SMALLINT     NOT NULL CHECK (mes BETWEEN 1 AND 12),
    nome_mes     VARCHAR(10)  NOT NULL,
    trimestre    SMALLINT     NOT NULL CHECK (trimestre BETWEEN 1 AND 4),
    semestre     SMALLINT     NOT NULL CHECK (semestre IN (1, 2)),
    ano          SMALLINT     NOT NULL,
    dias_no_mes  SMALLINT     NOT NULL CHECK (dias_no_mes BETWEEN 28 AND 31),
    UNIQUE (ano, mes)
);

CREATE TABLE dim_funcionario (
    id_funcionario  INTEGER       GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_origem       INTEGER       NOT NULL UNIQUE,
    nome            VARCHAR(100)  NOT NULL,
    matricula       VARCHAR(20)   NOT NULL,
    cargo           VARCHAR(50)   NOT NULL,
    antiguidade     INTEGER,
    ativo           BOOLEAN       NOT NULL
);

CREATE TABLE dim_escala (
    id_escala  INTEGER      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_origem  INTEGER      NOT NULL UNIQUE,
    nome       VARCHAR(50)  NOT NULL
);

CREATE TABLE dim_situacao (
    id_situacao  SMALLINT     PRIMARY KEY,
    descricao    VARCHAR(50)  NOT NULL UNIQUE,
    categoria    VARCHAR(20)  NOT NULL CHECK (categoria IN ('Em serviço', 'Impedido', 'Alerta', 'Livre'))
);

CREATE TABLE dim_motivo_troca (
    id_motivo_troca   SMALLINT     PRIMARY KEY,
    descricao         VARCHAR(50)  NOT NULL UNIQUE,
    conta_como_falta  BOOLEAN      NOT NULL
);

-- ---------------------------------------------------------------------
-- Fatos (chave primária composta pelas chaves das dimensões)
-- ---------------------------------------------------------------------

CREATE TABLE fato_servico (
    id_mes           INTEGER   NOT NULL REFERENCES dim_tempo (id_mes),
    id_funcionario   INTEGER   NOT NULL REFERENCES dim_funcionario (id_funcionario),
    id_escala        INTEGER   NOT NULL REFERENCES dim_escala (id_escala),
    id_motivo_troca  SMALLINT  NOT NULL REFERENCES dim_motivo_troca (id_motivo_troca),
    qtd_servico      SMALLINT  NOT NULL DEFAULT 0 CHECK (qtd_servico >= 0),
    qtd_substituido  SMALLINT  NOT NULL DEFAULT 0 CHECK (qtd_substituido >= 0),
    PRIMARY KEY (id_mes, id_funcionario, id_escala, id_motivo_troca),
    CHECK (qtd_servico + qtd_substituido > 0)
);

CREATE TABLE fato_situacao (
    id_mes          INTEGER   NOT NULL REFERENCES dim_tempo (id_mes),
    id_funcionario  INTEGER   NOT NULL REFERENCES dim_funcionario (id_funcionario),
    id_escala       INTEGER   NOT NULL REFERENCES dim_escala (id_escala),
    id_situacao     SMALLINT  NOT NULL REFERENCES dim_situacao (id_situacao),
    qtd_dias        SMALLINT  NOT NULL CHECK (qtd_dias BETWEEN 1 AND 31),
    PRIMARY KEY (id_mes, id_funcionario, id_escala, id_situacao)
);

CREATE INDEX ix_fato_servico_funcionario  ON fato_servico (id_funcionario);
CREATE INDEX ix_fato_servico_escala       ON fato_servico (id_escala);
CREATE INDEX ix_fato_situacao_escala      ON fato_situacao (id_escala);
CREATE INDEX ix_fato_situacao_situacao    ON fato_situacao (id_situacao);

-- ---------------------------------------------------------------------
-- Metadados: descrição de cada tabela e coluna, gravada no próprio banco.
-- O relatório de metadados (anexo do documento) é gerado a partir daqui.
-- ---------------------------------------------------------------------

COMMENT ON TABLE dim_tempo IS 'Dimensão de tempo. Uma linha por mês. Hierarquia: mês, trimestre, semestre, ano.';
COMMENT ON COLUMN dim_tempo.id_mes      IS 'Chave do mês, no formato AAAAMM (ex.: 202610).';
COMMENT ON COLUMN dim_tempo.mes         IS 'Número do mês, de 1 a 12.';
COMMENT ON COLUMN dim_tempo.nome_mes    IS 'Nome do mês, de Janeiro a Dezembro.';
COMMENT ON COLUMN dim_tempo.trimestre   IS 'Trimestre do ano, de 1 a 4.';
COMMENT ON COLUMN dim_tempo.semestre    IS 'Semestre do ano, 1 ou 2.';
COMMENT ON COLUMN dim_tempo.ano         IS 'Ano com quatro dígitos.';
COMMENT ON COLUMN dim_tempo.dias_no_mes IS 'Quantidade de dias do mês. Usada para calcular o efetivo médio.';

COMMENT ON TABLE dim_funcionario IS 'Dimensão de funcionário. Uma linha por funcionário do sistema. Dados fictícios no projeto.';
COMMENT ON COLUMN dim_funcionario.id_funcionario IS 'Chave do funcionário no Data Warehouse (chave substituta).';
COMMENT ON COLUMN dim_funcionario.id_origem      IS 'Id do funcionário no banco do sistema (Funcionarios.Id). Usado pelo ETL para atualizar o registro.';
COMMENT ON COLUMN dim_funcionario.nome           IS 'Nome do funcionário (fictício no projeto).';
COMMENT ON COLUMN dim_funcionario.matricula      IS 'Matrícula do funcionário (fictícia no projeto).';
COMMENT ON COLUMN dim_funcionario.cargo          IS 'Cargo do funcionário (ex.: Supervisor, Agente).';
COMMENT ON COLUMN dim_funcionario.antiguidade    IS 'Ordem de antiguidade: número menor = mais antigo. Vazio quando não informado.';
COMMENT ON COLUMN dim_funcionario.ativo          IS 'Verdadeiro se ainda é funcionário (não foi desativado no sistema).';

COMMENT ON TABLE dim_escala IS 'Dimensão de escala. Uma linha por escala de serviço (6 escalas).';
COMMENT ON COLUMN dim_escala.id_escala IS 'Chave da escala no Data Warehouse (chave substituta).';
COMMENT ON COLUMN dim_escala.id_origem IS 'Id da escala no banco do sistema (Escalas.Id).';
COMMENT ON COLUMN dim_escala.nome      IS 'Nome da escala (ex.: Escala A, Escala E).';

COMMENT ON TABLE dim_situacao IS 'Dimensão de situação. Lista fixa das situações possíveis de uma pessoa num dia.';
COMMENT ON COLUMN dim_situacao.id_situacao IS 'Chave da situação.';
COMMENT ON COLUMN dim_situacao.descricao   IS 'Nome da situação (ex.: Em serviço, Impedido – Atestado médico).';
COMMENT ON COLUMN dim_situacao.categoria   IS 'Agrupamento da situação: Em serviço, Impedido, Alerta ou Livre.';

COMMENT ON TABLE dim_motivo_troca IS 'Dimensão de motivo da troca. Lista fixa; indica se o motivo conta como falta.';
COMMENT ON COLUMN dim_motivo_troca.id_motivo_troca  IS 'Chave do motivo. 0 = Sem troca (serviço normal).';
COMMENT ON COLUMN dim_motivo_troca.descricao        IS 'Nome do motivo (ex.: Atestado médico, Troca combinada).';
COMMENT ON COLUMN dim_motivo_troca.conta_como_falta IS 'Verdadeiro se a troca por este motivo conta como falta de quem estava escalado.';

COMMENT ON TABLE fato_servico IS 'Fato de serviço. Grão: um funcionário, numa escala, num mês, por motivo de troca. Só serviços válidos (não cancelados).';
COMMENT ON COLUMN fato_servico.id_mes          IS 'Mês do serviço (FK para dim_tempo).';
COMMENT ON COLUMN fato_servico.id_funcionario  IS 'Funcionário (FK para dim_funcionario).';
COMMENT ON COLUMN fato_servico.id_escala       IS 'Escala do serviço (FK para dim_escala).';
COMMENT ON COLUMN fato_servico.id_motivo_troca IS 'Motivo da troca (FK para dim_motivo_troca). Sem troca = serviço normal.';
COMMENT ON COLUMN fato_servico.qtd_servico     IS 'Quantidade de serviços que o funcionário cumpriu no mês, com este motivo. Com motivo diferente de Sem troca, são serviços que ele cumpriu no lugar de outra pessoa.';
COMMENT ON COLUMN fato_servico.qtd_substituido IS 'Quantidade de vezes no mês em que o funcionário estava escalado e foi substituído por este motivo.';

COMMENT ON TABLE fato_situacao IS 'Fato de situação. Grão: um funcionário, numa escala, num mês, por situação. Conta os dias em cada situação.';
COMMENT ON COLUMN fato_situacao.id_mes         IS 'Mês (FK para dim_tempo).';
COMMENT ON COLUMN fato_situacao.id_funcionario IS 'Funcionário ativo (FK para dim_funcionario).';
COMMENT ON COLUMN fato_situacao.id_escala      IS 'Escala da pessoa no mês (FK para dim_escala).';
COMMENT ON COLUMN fato_situacao.id_situacao    IS 'Situação (FK para dim_situacao).';
COMMENT ON COLUMN fato_situacao.qtd_dias       IS 'Quantidade de dias do mês em que o funcionário esteve nesta situação.';

-- ---------------------------------------------------------------------
-- Listas fixas
-- ---------------------------------------------------------------------

INSERT INTO dim_situacao (id_situacao, descricao, categoria) VALUES
    (1, 'Em serviço',                   'Em serviço'),
    (2, 'Impedido – Atestado médico',   'Impedido'),
    (3, 'Impedido – Viagem a serviço',    'Impedido'),
    (4, 'Impedido – Férias',            'Impedido'),
    (5, 'Impedido – Licença',           'Impedido'),
    (6, 'Impedido – Curso',             'Impedido'),
    (7, 'Escalado e impedido',          'Alerta'),
    (8, 'Livre',                        'Livre');

INSERT INTO dim_motivo_troca (id_motivo_troca, descricao, conta_como_falta) VALUES
    (0, 'Sem troca',                       FALSE),
    (1, 'Atestado médico',                 TRUE),
    (2, 'Viagem a serviço',                  TRUE),
    (3, 'Não compareceu',                  TRUE),
    (4, 'Troca combinada',                 FALSE),
    (5, 'Férias, licença ou curso',        FALSE);

-- ---------------------------------------------------------------------
-- Meses de 2026 a 2030
-- ---------------------------------------------------------------------
INSERT INTO dim_tempo (id_mes, mes, nome_mes, trimestre, semestre, ano, dias_no_mes)
SELECT
    CAST(TO_CHAR(m, 'YYYYMM') AS INTEGER),
    EXTRACT(MONTH FROM m),
    (ARRAY['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho',
           'Agosto','Setembro','Outubro','Novembro','Dezembro'])[EXTRACT(MONTH FROM m)],
    EXTRACT(QUARTER FROM m),
    CASE WHEN EXTRACT(MONTH FROM m) <= 6 THEN 1 ELSE 2 END,
    EXTRACT(YEAR FROM m),
    EXTRACT(DAY FROM (m + INTERVAL '1 month' - INTERVAL '1 day'))
FROM generate_series(DATE '2026-01-01', DATE '2030-12-01', INTERVAL '1 month') AS g(m);

COMMIT;
