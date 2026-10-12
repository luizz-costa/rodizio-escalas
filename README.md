# Rodizio — Gestão de Escalas de Serviço

Sistema web para montar as escalas de serviço de uma equipe de segurança patrimonial. Ele substitui o controle em planilhas: registra funcionários, feriados e impedimentos e calcula, para cada escala e cada data, a ordem de prioridade de quem deve cumprir o próximo serviço (quem está há mais tempo sem serviço vem primeiro). Os dados do sistema alimentam uma camada de Business Intelligence (Data Warehouse e painéis) para acompanhar o efetivo e as faltas.

O cenário é fictício: o sistema atende a **Organização Exemplo**, com seis escalas (Escala A a Escala F), e todos os nomes, cargos e números usados no código, na documentação e nos dados de demonstração são inventados.

Esta é uma versão de portfólio de um sistema desenvolvido originalmente num projeto de extensão universitário.

## Autor

* Luiz Felipe Alves Costa ([@luizz-costa](https://github.com/luizz-costa))

---

## Tecnologias utilizadas

* **Backend/Sistema:** C# com ASP.NET Core (.NET 8), Entity Framework Core 8 com migrations e Swagger. Telas em HTML, CSS e JavaScript, sem framework, entregues pela própria API.
* **Banco de dados:** PostgreSQL 16 em contêiner Docker (Docker Compose).
* **Business Intelligence:** Data Warehouse em PostgreSQL, numa base separada, com o modelo dimensional pronto em [`db/modelo-dimensional.sql`](db/modelo-dimensional.sql). Integração (ETL) em Python com transformações em SQL e ferramenta de visualização de software livre, previstas.

## Estrutura do repositório

* `/docs`: documentação técnica.
  * [`arquitetura.md`](docs/arquitetura.md): diagrama, componentes, ambiente de execução e rotas da API.
  * [`modelo-de-dados.md`](docs/modelo-de-dados.md): banco do sistema, modelo dimensional e dicionário de dados.
  * [`relatorio-metadados.md`](docs/relatorio-metadados.md): metadados do Data Warehouse, gerados pelo PostgreSQL.
  * [`funcionamento.md`](docs/funcionamento.md): como o sistema é usado no dia a dia.
  * [`execucao.md`](docs/execucao.md): como executar o projeto do zero.
  * [`decisoes.md`](docs/decisoes.md): decisões técnicas e o motivo de cada uma.
  * [`roteiro-testes-mvp.md`](docs/roteiro-testes-mvp.md): roteiro de testes do MVP.
* `/src`: código-fonte do sistema (API e telas). Mapa das pastas em [`src/README.md`](src/README.md).
* `/db`: scripts SQL do Data Warehouse.
* `/img`: diagrama da arquitetura.

## Como executar o projeto

1. **Credenciais:** crie o arquivo `.env` a partir do `.env.example`.
2. **Sistema:** suba o banco, o Data Warehouse e a API com `docker compose up -d --build`. A API aplica as migrations ao iniciar, e as telas abrem em `http://localhost:5001`.
   As portas podem ser trocadas no `.env`, pelas variáveis `API_PORT` (padrão 5001) e `DB_PORT` (padrão 5432). Elas sempre ficam em `127.0.0.1`, ou seja, só aceitam conexões do próprio computador. Exemplo, para rodar ao lado de outro projeto que já usa as portas padrão: `API_PORT=5002` e `DB_PORT=5433`; as telas passam a abrir em `http://localhost:5002`.
3. **Dados de demonstração (opcional):** `docker compose run --rm api-rodizio --demo`.

Passo a passo completo, com os comandos e a solução de problemas comuns: [`docs/execucao.md`](docs/execucao.md).
