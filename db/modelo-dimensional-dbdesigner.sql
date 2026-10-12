CREATE TABLE dim_tempo (
    id_mes INTEGER NOT NULL,
    mes SMALLINT NOT NULL,
    nome_mes VARCHAR(10) NOT NULL,
    trimestre SMALLINT NOT NULL,
    semestre SMALLINT NOT NULL,
    ano SMALLINT NOT NULL,
    dias_no_mes SMALLINT NOT NULL,
    PRIMARY KEY (id_mes)
);

CREATE TABLE dim_funcionario (
    id_funcionario INTEGER NOT NULL,
    id_origem INTEGER NOT NULL,
    nome VARCHAR(100) NOT NULL,
    matricula VARCHAR(20) NOT NULL,
    cargo VARCHAR(50) NOT NULL,
    antiguidade INTEGER,
    ativo BOOLEAN NOT NULL,
    PRIMARY KEY (id_funcionario)
);

CREATE TABLE dim_escala (
    id_escala INTEGER NOT NULL,
    id_origem INTEGER NOT NULL,
    nome VARCHAR(50) NOT NULL,
    PRIMARY KEY (id_escala)
);

CREATE TABLE dim_situacao (
    id_situacao SMALLINT NOT NULL,
    descricao VARCHAR(50) NOT NULL,
    categoria VARCHAR(20) NOT NULL,
    PRIMARY KEY (id_situacao)
);

CREATE TABLE dim_motivo_troca (
    id_motivo_troca SMALLINT NOT NULL,
    descricao VARCHAR(50) NOT NULL,
    conta_como_falta BOOLEAN NOT NULL,
    PRIMARY KEY (id_motivo_troca)
);

CREATE TABLE fato_servico (
    id_mes INTEGER NOT NULL,
    id_funcionario INTEGER NOT NULL,
    id_escala INTEGER NOT NULL,
    id_motivo_troca SMALLINT NOT NULL,
    qtd_servico SMALLINT NOT NULL,
    qtd_substituido SMALLINT NOT NULL,
    PRIMARY KEY (id_mes, id_funcionario, id_escala, id_motivo_troca),
    FOREIGN KEY (id_mes) REFERENCES dim_tempo (id_mes),
    FOREIGN KEY (id_funcionario) REFERENCES dim_funcionario (id_funcionario),
    FOREIGN KEY (id_escala) REFERENCES dim_escala (id_escala),
    FOREIGN KEY (id_motivo_troca) REFERENCES dim_motivo_troca (id_motivo_troca)
);

CREATE TABLE fato_situacao (
    id_mes INTEGER NOT NULL,
    id_funcionario INTEGER NOT NULL,
    id_escala INTEGER NOT NULL,
    id_situacao SMALLINT NOT NULL,
    qtd_dias SMALLINT NOT NULL,
    PRIMARY KEY (id_mes, id_funcionario, id_escala, id_situacao),
    FOREIGN KEY (id_mes) REFERENCES dim_tempo (id_mes),
    FOREIGN KEY (id_funcionario) REFERENCES dim_funcionario (id_funcionario),
    FOREIGN KEY (id_escala) REFERENCES dim_escala (id_escala),
    FOREIGN KEY (id_situacao) REFERENCES dim_situacao (id_situacao)
);
