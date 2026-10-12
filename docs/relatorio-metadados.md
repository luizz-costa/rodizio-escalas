# Relatório de metadados do Data Warehouse

Gerado a partir do catálogo do PostgreSQL 16 ([`db/relatorio-metadados.sql`](../db/relatorio-metadados.sql)), sobre a base criada por [`db/modelo-dimensional.sql`](../db/modelo-dimensional.sql). As descrições foram gravadas no banco com `COMMENT ON`.


## `dim_escala`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_escala` | integer | Não | Sim |  | Chave da escala no Data Warehouse (chave substituta). |
| `id_origem` | integer | Não |  |  | Id da escala no banco do sistema (Escalas.Id). |
| `nome` | varchar(50) | Não |  |  | Nome da escala (ex.: Escala A, Escala E). |

## `dim_funcionario`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_funcionario` | integer | Não | Sim |  | Chave do funcionário no Data Warehouse (chave substituta). |
| `id_origem` | integer | Não |  |  | Id do funcionário no banco do sistema (Funcionarios.Id). Usado pelo ETL para atualizar o registro. |
| `nome` | varchar(100) | Não |  |  | Nome do funcionário (fictício no projeto). |
| `matricula` | varchar(20) | Não |  |  | Matrícula do funcionário (fictícia no projeto). |
| `cargo` | varchar(50) | Não |  |  | Cargo do funcionário (ex.: Supervisor, Agente). |
| `antiguidade` | integer | Sim |  |  | Ordem de antiguidade: número menor = mais antigo. Vazio quando não informado. |
| `ativo` | boolean | Não |  |  | Verdadeiro se ainda é funcionário (não foi desativado no sistema). |

## `dim_motivo_troca`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_motivo_troca` | smallint | Não | Sim |  | Chave do motivo. 0 = Sem troca (serviço normal). |
| `descricao` | varchar(50) | Não |  |  | Nome do motivo (ex.: Atestado médico, Troca combinada). |
| `conta_como_falta` | boolean | Não |  |  | Verdadeiro se a troca por este motivo conta como falta de quem estava escalado. |

## `dim_situacao`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_situacao` | smallint | Não | Sim |  | Chave da situação. |
| `descricao` | varchar(50) | Não |  |  | Nome da situação (ex.: Em serviço, Impedido – Atestado médico). |
| `categoria` | varchar(20) | Não |  |  | Agrupamento da situação: Em serviço, Impedido, Alerta ou Livre. |

## `dim_tempo`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_mes` | integer | Não | Sim |  | Chave do mês, no formato AAAAMM (ex.: 202610). |
| `mes` | smallint | Não |  |  | Número do mês, de 1 a 12. |
| `nome_mes` | varchar(10) | Não |  |  | Nome do mês, de Janeiro a Dezembro. |
| `trimestre` | smallint | Não |  |  | Trimestre do ano, de 1 a 4. |
| `semestre` | smallint | Não |  |  | Semestre do ano, 1 ou 2. |
| `ano` | smallint | Não |  |  | Ano com quatro dígitos. |
| `dias_no_mes` | smallint | Não |  |  | Quantidade de dias do mês. Usada para calcular o efetivo médio. |

## `fato_servico`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_mes` | integer | Não | Sim | `dim_tempo.id_mes` | Mês do serviço (FK para dim_tempo). |
| `id_funcionario` | integer | Não | Sim | `dim_funcionario.id_funcionario` | Funcionário (FK para dim_funcionario). |
| `id_escala` | integer | Não | Sim | `dim_escala.id_escala` | Escala do serviço (FK para dim_escala). |
| `id_motivo_troca` | smallint | Não | Sim | `dim_motivo_troca.id_motivo_troca` | Motivo da troca (FK para dim_motivo_troca). Sem troca = serviço normal. |
| `qtd_servico` | smallint | Não |  |  | Quantidade de serviços que o funcionário cumpriu no mês, com este motivo. Com motivo diferente de Sem troca, são serviços que ele cumpriu no lugar de outra pessoa. |
| `qtd_substituido` | smallint | Não |  |  | Quantidade de vezes no mês em que o funcionário estava escalado e foi substituído por este motivo. |

## `fato_situacao`

| Coluna | Tipo | Aceita vazio | Chave primária | Chave estrangeira | Descrição |
| --- | --- | --- | --- | --- | --- |
| `id_mes` | integer | Não | Sim | `dim_tempo.id_mes` | Mês (FK para dim_tempo). |
| `id_funcionario` | integer | Não | Sim | `dim_funcionario.id_funcionario` | Funcionário ativo (FK para dim_funcionario). |
| `id_escala` | integer | Não | Sim | `dim_escala.id_escala` | Escala da pessoa no mês (FK para dim_escala). |
| `id_situacao` | smallint | Não | Sim | `dim_situacao.id_situacao` | Situação (FK para dim_situacao). |
| `qtd_dias` | smallint | Não |  |  | Quantidade de dias do mês em que o funcionário esteve nesta situação. |
