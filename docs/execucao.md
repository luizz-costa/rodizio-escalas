# Como executar

Passo a passo para executar o sistema do zero. O jeito principal é com Docker: um comando sobe o banco do sistema, o Data Warehouse e a API com as telas, sem instalar o .NET. Os comandos são de terminal Linux (bash); no Windows, as diferenças estão indicadas.

## Pré-requisitos

| Ferramenta | Para quê | Como conferir |
| --- | --- | --- |
| Docker com Docker Compose | Executar o banco, o Data Warehouse e a API | `docker compose version` |
| Git | Baixar o repositório | `git --version` |

No Linux, o usuário precisa estar no grupo `docker` para usar o Docker sem `sudo`. Se aparecer `permission denied ... docker.sock`, rode `sudo usermod -aG docker $USER` e entre de novo na sessão.

## 1. Baixar o repositório

```bash
git clone https://github.com/luizz-costa/rodizio-escalas.git
cd rodizio-escalas
```

## 2. Configurar as credenciais

Crie o arquivo `.env` a partir do modelo e preencha com valores seus (usuário, senha e nome do banco). As portas são opcionais: `API_PORT` (padrão 5001) e `DB_PORT` (padrão 5432). O `.env` não vai para o repositório: ele está no `.gitignore`. A senha não deve ter ponto e vírgula (`;`), porque ela entra na connection string da API.

```bash
cp .env.example .env
```

No Windows (PowerShell): `Copy-Item .env.example .env`.

## 3. Subir tudo

Na raiz do repositório:

```bash
docker compose up -d --build
```

Na primeira vez, o comando baixa as imagens do .NET e do PostgreSQL e compila a API; leva alguns minutos. Ele sobe dois contêineres:

| Contêiner | O que é | Porta |
| --- | --- | --- |
| `postgres-rodizio` | PostgreSQL 16 com o banco do sistema e o Data Warehouse (`rodizio_dw`) | `127.0.0.1:5432` (ou `DB_PORT`) |
| `api-rodizio` | API e telas, compiladas pelo `Dockerfile` | `127.0.0.1:5001` (ou `API_PORT`) |

O que acontece sozinho:
- **Data Warehouse:** na primeira subida, com o volume ainda vazio, o script `db/init/01-cria-dw.sh` cria a base `rodizio_dw` e roda o `db/modelo-dimensional.sql`.
- **Tabelas do sistema:** ao iniciar, a API aplica as migrations pendentes. Num banco já atualizado, não faz nada.
- **Ordem:** a API só sobe depois que o banco responde (healthcheck).

Para conferir:

```bash
docker compose ps
```

Os dois contêineres devem estar `Up`, e o banco `(healthy)`.

## 4. Gerar os dados fictícios (opcional)

```bash
docker compose run --rm api-rodizio --demo
```

O comando cria um contêiner temporário da API, preenche o banco com dados fictícios de demonstração e encerra:
- 51 pessoas nas 6 escalas (Escala A a Escala F);
- 2 dias especiais e 5 impedimentos;
- histórico de serviços desde 01/09/2026 até a data de hoje.

Ele só funciona com o banco vazio.

## 5. Usar

| Endereço | O que abre |
| --- | --- |
| `http://localhost:5001/` | Telas do sistema (a inicial é a Escala do dia) |

Para conferir se deu certo:
- a tela Escala do dia mostra os totais e os quadros de cada escala;
- a tela Montar escala mostra a lista de prioridade de amanhã.

As portas só aceitam conexões do próprio computador (`127.0.0.1`). Se o sistema roda num servidor e você quer abrir as telas no navegador de outro computador, crie um túnel SSH a partir desse outro computador e abra `http://localhost:5001/` nele:

```bash
ssh -L 5001:localhost:5001 USUARIO@SERVIDOR
```

Para consultar o Data Warehouse pelo terminal:

```bash
docker compose exec db-rodizio bash
psql -U $POSTGRES_USER -d rodizio_dw
```

Dentro do `psql`, `\dt` lista as tabelas; `\q` sai do `psql` e `exit` sai do contêiner.

## Parar, religar e recomeçar

| Para | Comando |
| --- | --- |
| Parar tudo, mantendo os dados | `docker compose down` |
| Religar | `docker compose up -d` |
| Recompilar a API depois de mudar o código | `docker compose up -d --build` |
| Ver o log da API | `docker compose logs api-rodizio` |
| **Apagar tudo** (banco do sistema e Data Warehouse) e recomeçar do zero | `docker compose down -v` e depois os passos 3 e 4 |

Os dados ficam guardados no volume `rodizio_db_data`. O `-v` apaga esse volume: use só quando quiser perder os dados.

## Desenvolvimento sem Docker para a API

Para depurar a API no editor, dá para rodar só o banco no Docker e a API com o .NET 8 SDK instalado. Antes, pare o contêiner da API, que usa a mesma porta 5001:

```bash
docker compose stop api-rodizio
```

A connection string fica no User Secrets do .NET, fora do código, com os mesmos valores do `.env`:

```bash
cd src/Rodizio.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=NOME_DO_BANCO;Username=USUARIO;Password=SENHA"
dotnet run
```

Nesse modo, a API também aplica as migrations ao iniciar, e o Swagger fica em `http://localhost:5001/swagger` (só em ambiente de desenvolvimento). Os dados fictícios: `dotnet run -- --demo`.

## Data Warehouse num volume antigo

O script de criação do Data Warehouse só roda quando o volume do banco está vazio. Num volume criado antes dele, crie o Data Warehouse uma vez, à mão, na raiz do repositório, trocando `USUARIO` pelo valor do `.env`:

```bash
docker exec -i postgres-rodizio psql -U USUARIO -d postgres -c "CREATE DATABASE rodizio_dw;"
docker exec -i postgres-rodizio psql -U USUARIO -d rodizio_dw < db/modelo-dimensional.sql
```

## Problemas comuns

| Sintoma | Causa provável | Solução |
| --- | --- | --- |
| `permission denied ... docker.sock` | O usuário não está no grupo `docker` | `sudo usermod -aG docker $USER` e entrar de novo na sessão |
| `no configuration file provided` | Comando rodado fora da raiz do repositório | Volte para a pasta onde está o `docker-compose.yml` |
| O Docker avisa que as variáveis `POSTGRES_*` não estão definidas | Falta o arquivo `.env` na raiz | Refaça o passo 2 |
| `api-rodizio` fica em `Restarting` | A API não conseguiu conectar no banco ou aplicar as migrations | Veja o motivo em `docker compose logs api-rodizio`; confira o `.env` |
| `O banco já tem dados. Zere o banco...` ao rodar `--demo` | O gerador só roda com o banco vazio | `docker compose down -v` e os passos 3 e 4 |
| O banco não cria o `rodizio_dw` e o log mostra `$'\r': command not found` | O script `.sh` ficou com fim de linha do Windows (CRLF) | O `.gitattributes` evita isso; se acontecer, clone de novo ou converta o arquivo para LF e rode `docker compose down -v` |
| Porta 5432 ou 5001 já em uso | Outro PostgreSQL, ou a API rodando com `dotnet run` | Pare o outro serviço ou troque a porta no `.env` (`DB_PORT`, `API_PORT`) |
| Tela com comportamento antigo depois de uma atualização | Cache do navegador | Recarregue com Ctrl+F5 |
