# Instruções de utilização

## Instalação

Como configurar o banco, aplicar as migrations e executar o sistema: [`docs/execucao.md`](../docs/execucao.md).

## Mapa do código

```text
src/
├── Rodizio.sln                        solução do .NET
└── Rodizio.Api/                       API em ASP.NET Core (.NET 8), que também entrega as telas
    ├── Program.cs                     configuração da API, do banco e do Swagger; opção --demo
    ├── appsettings.json               configuração sem senha (a connection string vem do .env ou do User Secrets)
    ├── Controllers/                   rotas da API: Funcionarios, Escalas, DiasEspeciais,
    │                                  Impedimentos, Servicos, Situacao e Postos
    ├── Models/                        entidades gravadas no banco
    ├── Dtos/                          formatos de entrada e saída de algumas rotas
    ├── Regras/                        regras de negócio: fila do dia (Calendario) e
    │                                  lista de prioridade (CalculadoraPrioridade)
    ├── Data/
    │   ├── AppDbContext.cs            mapeamento das tabelas e restrições do banco
    │   ├── GeradorDemonstracao.cs     gerador de dados fictícios (dotnet run -- --demo)
    │   └── Migrations/                estrutura do banco (Entity Framework Core): migration InitialCreate
    └── wwwroot/                       telas em HTML, CSS e JavaScript, sem framework
        ├── index.html                 Escala do dia (tela inicial)
        ├── montar-escala.html         Montar escala
        ├── funcionarios.html          Funcionários
        ├── impedimentos.html          Impedimentos
        ├── dias-especiais.html        Dias especiais
        ├── css/app.css                visual comum
        └── js/                        um arquivo por tela, mais api.js (chamadas à API)
                                       e layout.js (cabeçalho e menu)
```

Como o sistema é usado está em [`docs/funcionamento.md`](../docs/funcionamento.md), e o motivo de cada decisão técnica, em [`docs/decisoes.md`](../docs/decisoes.md).

## Histórico de versões

### [0.1.0] - 26/09/2026
#### Adicionado
- API com cadastros (funcionários, escalas, dias especiais e impedimentos), registro de serviços com troca e cancelamento, lista de prioridade e situação do dia.
- Gerador de dados fictícios de demonstração.
- Cinco telas: Escala do dia, Montar escala, Funcionários, Impedimentos e Dias especiais.
- Roteiro de testes do MVP executado (34 casos).
