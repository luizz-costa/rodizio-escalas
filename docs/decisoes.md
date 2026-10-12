# Decisões

Registro das decisões técnicas e de projeto, em ordem cronológica. Cada decisão tem um número fixo (D-NN), que não muda nem é reaproveitado. Uma decisão substituída continua aqui, com a indicação da decisão que a substituiu. Os códigos RN, RF e RNF identificam regras de negócio, requisitos funcionais e requisitos não funcionais.

## 22 e 23/09/2026 — Base do projeto

- **D-01:** tecnologias: C# com .NET 8, Entity Framework Core com migrations, PostgreSQL 16 em Docker, Git e GitHub, VS Code.
- **D-02:** o sistema apoia a decisão, não aloca sozinho: calcula a ordem de prioridade e bloqueia quem não pode, e a escolha é do responsável pela escala (RN-38).
- **D-03:** projeto único, com os controllers acessando o `DbContext` diretamente, sem camadas extras, para caber no prazo do projeto.
- **D-04:** o campo `Disponivel` sai de `Funcionario`: a disponibilidade muda a cada dia e deve ser derivada de registros. *Substituída pela D-16, que define esses registros como eventos de serviço e de impedimento.*
- **D-05:** funcionário com consulta (lista e por id), cadastro e edição, **sem exclusão física**. O campo `Ativo` faz a desativação lógica (`PATCH /desativar` e `/reativar`), e a edição não altera `Ativo`.
- **D-06:** `Cargo` é texto livre, sem lista fechada de cargos.
- **D-07:** `Posto` com modelo mínimo (descrição e ativo), no mesmo padrão de funcionário. Depois, ficou fora do fluxo da escala, por estar fora do escopo do MVP: o sistema não registra o posto de cada pessoa (RN-34).
- **D-08:** credenciais fora do Git: a connection string fica no .NET User Secrets, e as variáveis do banco num arquivo `.env`, que o `.gitignore` exclui.
- **D-09:** desenvolvimento incremental: uma mudança por vez, testada, com um commit por bloco e atualização deste arquivo.
- **D-10:** a documentação do projeto fica no repositório, para que ele não dependa de conversas ou de documentos externos.
- **D-11:** a implementação do registro diário ficou pausada até a revisão da arquitetura. *Encerrada em 24/09, com a D-16.*
- **D-12:** a API continua como parte da solução, e a camada analítica (modelagem dimensional, integração e visualização) é estudada em paralelo.
- **D-13:** combinações feitas em reunião só passam a valer como decisão depois de registradas neste arquivo.
- **D-14:** o projeto trabalha apenas com dados fictícios (RNF-01).
- **D-15:** o trabalho segue etapas encadeadas: cada etapa parte do que a anterior entregou.

## 24/09/2026 — Implementação do sistema

- **D-16:** o registro diário passa a ser feito por eventos: `Servico` (quem cumpriu serviço, em que data, em qual escala) e `Impedimento` (atestado ou viagem a serviço, com período). Substitui a proposta antiga de um registro diário por pessoa (D-04).
- **D-17:** serviço lançado por engano é cancelado logicamente, não apagado.
- **D-18:** a regra da lista de prioridade fica numa classe própria. É uma exceção pontual à D-03, justificada por ser a regra central e precisar de testes.
- **D-19:** o frontend é feito em HTML, CSS e JavaScript simples, servidos pela própria API (`wwwroot`), consumindo os endpoints por `fetch`. Mesma origem, sem CORS e sem ferramentas de build.
- **Regras de produto (reversíveis):** desempate por antiguidade com o número menor primeiro (RN-12); quem nunca serviu na fila vai primeiro (RN-05); motivos de impedimento só atestado e viagem a serviço (RN-24); sem limite de pessoas por dia (RN-06); dia especial só pode ser removido se for data futura (D-24).

## 24/09/2026 — Funcionário ampliado

- **D-20:** `EscalaId` é opcional no banco (registros antigos ficaram sem escala); a API exige uma escala existente no cadastro e na edição e responde 400 caso contrário.
- **D-21:** `Antiguidade` é opcional; sem valor, a pessoa fica por último no desempate (RN-13).
- **D-22:** os dias permitidos são guardados em 8 colunas booleanas (segunda a domingo e feriado), respeitando a 1ª forma normal, com padrão verdadeiro (RN-19). Os registros que já existiam foram preenchidos com `true`.

## 25/09/2026 — Dias especiais e convenção de erros

- **D-23:** datas de calendário (feriados, serviços e impedimentos) usam `DateOnly`, gravado como `date` no PostgreSQL, para evitar problemas de hora e fuso.
- **D-24:** dia especial não pode ser cadastrado em duplicidade (índice único na data). Pode ser cadastrado em data passada, para permitir lançar o histórico. Só pode ser removido se a data for futura, porque remover um feriado passado alteraria a fila e a contagem de folgas retroativamente.
- **D-25 (convenção de erros da API):** erro ligado a um campo → `ValidationProblem` (bloco `errors`, campo por campo); regra de negócio que impede a operação → `Problem`, com a explicação em `detail`. Os dois respondem 400 em formato ProblemDetails.

## 25/09/2026 — Impedimento

- **D-26:** o motivo do impedimento é um enum (`AtestadoMedico`, `ViagemAServico`), gravado como texto no banco (`HasConversion<string>`) e no JSON (`JsonStringEnumConverter`, válido para toda a API). Novos motivos entram sem migration.
- **D-27:** os eventos têm `CriadoEm` e `AtualizadoEm` em UTC, preenchidos pela API, para a carga incremental do ETL. Os campos controlados pela API (`Id`, `Cancelado`, `CriadoEm`, `AtualizadoEm`) ignoram o que o cliente enviar.
- **D-28:** `DataFim` é opcional (vazia = impedimento em aberto); o encerramento é feito por `PATCH /encerrar?data=` (padrão: hoje).
- **D-29:** dado errado se corrige pela edição (motivo e datas; a pessoa não muda); registro errado se cancela logicamente (`PATCH /cancelar`).
- **D-30:** uma pessoa não pode ter dois impedimentos ativos com períodos sobrepostos. A validação fica só na API, sem restrição no banco, e pode ser revertida sem migration.

## 25/09/2026 — Serviço

- **D-31:** o serviço guarda a data, a escala (no próprio serviço, para preservar o histórico quando a pessoa muda de escala), quem cumpriu (`FuncionarioId`) e, em caso de troca, quem estava escalado (`EscaladoOriginalId`, opcional). A fila não é gravada: é derivada da data e dos dias especiais.
- **D-32:** uma pessoa só pode ter um serviço ativo por dia, garantido por índice único parcial (`FuncionarioId`, `Data`) com filtro `"Cancelado" = false`, além da validação na API.
- **D-33:** um lançamento é bloqueado se o funcionário estiver desativado (erro de campo), for de outra escala, dobrar (serviço no dia anterior ou no seguinte), estiver impedido na data ou não puder servir naquele dia. Em dia especial vale `PodeFeriado`, mesmo no fim de semana (RN-29). Intervalo de um dia não bloqueia (RN-15).
- **D-34:** na troca, o substituto passa pelas mesmas regras; o serviço passa a ser dele e o primeiro escalado original fica guardado. Se a troca devolver o serviço ao original, a marca de troca é removida.
- **D-35:** um impedimento pode ser lançado sobre um serviço já registrado; a tela do dia sinaliza a necessidade de troca. No Data Warehouse, esse caso é a situação "Escalado e impedido" (D-55).
- **D-36:** a regra "em que dia a pessoa pode servir" é um método da entidade `Funcionario` (`PodeServirEm`). Os formatos de entrada e saída que não são tabelas ficam na pasta `Dtos` (o primeiro foi `TrocaServicoRequest`).

## 26/09/2026 — Lista de prioridade

- **D-37:** a lista de prioridade é calculada pela classe `CalculadoraPrioridade` (D-18), registrada por injeção de dependência e exposta em `GET /api/Escalas/{id}/prioridade?data=`. A ordem vem da data do último serviço na fila do dia; os dias sem serviço são calculados na hora, não guardados. As regras de calendário ficam na classe estática `Calendario`.
- **D-38:** ordenação: quem pode antes de quem não pode; quem nunca serviu na fila primeiro (RN-05); último serviço mais antigo primeiro; antiguidade de número menor primeiro (RN-12); sem antiguidade por último (D-21). Quem não pode aparece no fim, com o motivo.
- **Dívida técnica:** os bloqueios da lista repetem, em memória, as verificações do `ServicosController`. Unificar é melhoria futura; os testes dos dois lados garantem que continuem iguais.

## 26/09/2026 — Situação do dia

- **D-39:** `GET /api/Situacao?data=` é uma rota só de leitura, feita sob medida para a tela Escala do dia: fila do dia, totais, escalados (com troca e marca de impedido) e impedidos por escala, e alertas de escalado impedido (D-35). O total de disponíveis usa "ativos menos impedidos".

## 26/09/2026 — Dados de demonstração

- **D-40:** dados fictícios de demonstração gerados pela classe `GeradorDemonstracao`, acionada só por linha de comando (`dotnet run -- --demo`), nunca por rota da API. Ela recusa rodar se o banco já tiver dados. Para recomeçar: `dotnet ef database drop`, `dotnet ef database update`, `dotnet run -- --demo`.
- **D-41:** conteúdo: 51 pessoas fictícias nas 6 escalas (Escala A a Escala F), restrições de dia, 1 desativado, 2 dias especiais, 5 impedimentos e histórico de serviços desde 01/09/2026 até a véspera, escolhido dia a dia pela própria `CalculadoraPrioridade`. As quantidades de pessoas por dia em cada escala (1, 1, 2, 1, 2, 3) são fictícias e servem só à demonstração (RN-06).
- **Verificação:** banco recriado do zero pelas migrations; serviços gerados sem nenhum caso de "dobrar" (consulta de autojunção sem resultados).

## 26/09/2026 — Base do frontend

- **D-42:** o frontend é uma página HTML por tela, em `wwwroot`, entregue pela própria API (`UseDefaultFiles` + `UseStaticFiles`), sem framework e sem roteador. Cabeçalho e navegação são montados por `js/layout.js`, num lugar só.
- **D-43:** nenhum recurso externo (CDN, fontes do Google): o sistema deve funcionar em rede restrita e não chamar servidores de terceiros. Fonte do sistema operacional.
- **D-44:** toda chamada à API passa por `js/api.js`, que converte o ProblemDetails num `ErroApi` (`detalhe` e `campos`); `mostrarErro` exibe o `detail` no aviso geral e os erros de campo junto de cada campo (`data-erro`). Todo dado vindo da API passa por `esc()` antes de entrar no HTML (proteção contra XSS). Datas são montadas no fuso local (`dataIso`), nunca por `toISOString`.
- **D-45:** cores com significado: grafite para a fila de dias úteis, um tom de alerta para a fila de fins de semana e feriados, amarelo claro para impedido. O cabeçalho mostra sempre "Organização Exemplo · dados fictícios".

## 26/09/2026 — Tela Funcionários

- **D-46 (padrões de tela, repetidos nas seguintes):** um mesmo formulário serve para cadastrar e editar, controlado pela variável `editandoId`; na edição, o formulário vem preenchido com todos os dados atuais, porque a edição substitui os campos. As ações da tabela usam delegação de eventos (um ouvinte no `<tbody>`, com `data-acao` e `data-id`). As confirmações explicam a consequência da ação. A tela não valida regras: envia e mostra o que a API responder (`novalidate`).
- **Melhoria candidata (backend):** a API hoje aceita nome vazio no cadastro de funcionário. Se for desejado, a validação deve entrar na API, não só na tela.

## 26/09/2026 — Tela Impedimentos

- **D-47:** tela de impedimentos com registro, correção (funcionário travado, D-29), encerramento por janela nativa (`<dialog>`) e cancelamento com confirmação. A lista mostra por padrão os impedimentos ativos na data de hoje. Funcionários agrupados por escala (`<optgroup>`).
- **D-48:** a data de fim é o último dia impedido (inclusive). A tela mostra "Termina hoje" quando o fim é hoje, e a janela de encerrar orienta a escolher ontem se a pessoa já voltou. O padrão sugerido é hoje.
- **Técnica:** campos de data não preenchidos são omitidos do JSON (e não enviados como `null`) para que a validação em português da API responda. Regra global `[hidden] { display: none !important; }`.

## 26/09/2026 — Tela Dias especiais

- **D-49:** tela de dias especiais com cadastro e remoção. O botão "Remover" só aparece em datas futuras (comodidade da tela); a proibição continua na API (D-24), que é quem garante a regra.
- **Correção no backend:** o cadastro de dia especial passou a exigir a data (sem ela, o `DateOnly` chegava como 01/01/0001 e era gravado). Encontrada ao planejar os testes da tela; os demais cadastros já validavam.

## 26/09/2026 — Tela Montar escala

- **D-50:** a tela Montar escala abre no dia seguinte (RN-01), ou na escala e data vindas da URL (`?escala=&data=`). Mostra a lista de prioridade exatamente como a API calcula e, depois de cada ação (escalar, trocar, desfazer), pede a lista de novo à API em vez de reordenar na tela. O botão "Escalar" é desabilitado no clique (proteção contra clique duplo; o índice único do banco é a segunda proteção). A troca oferece os elegíveis na ordem de prioridade, com o primeiro pré-selecionado: sugere, não decide (D-02). Datas passadas mostram um aviso de que o lançamento altera o histórico.

## 26/09/2026 — Tela Escala do dia

- **D-51:** a tela inicial (Escala do dia) mostra, para uma data (padrão: hoje), os alertas de escalado impedido com um atalho "Resolver", os totais do dia e um quadro por escala com escalados e impedidos. Consome só o `GET /api/Situacao`. As telas se ligam apenas por URL (`montar-escala.html?escala=&data=`), sem estado compartilhado.
- **Refatoração:** `comMaiuscula` e `textoMotivo` passaram do `montar-escala.js` para o `api.js`, por serem usadas em duas telas. Teste de regressão feito no Montar escala.
- **Observação operacional:** o navegador guarda cópias de arquivos (cache). Em desenvolvimento, manter "Desativar cache" no F12; em demonstrações após atualizações, recarregar com Ctrl+F5.

## 26/09/2026 — Roteiro de testes

- **Execução:** 34 casos executados em 26/09/2026 ([`roteiro-testes-mvp.md`](roteiro-testes-mvp.md)). Achado nos CT-20 e CT-21: um escalado que ficava impedido depois aparecia no Montar escala só como "Já está escalado nesta data", e a coluna de escalados não indicava a necessidade de troca. Causa: a lista de prioridade informa um único motivo por pessoa, e "já escalado" vinha antes de "impedido"; a coluna de escalados usa `/api/Servicos`, que não conhece impedimentos.
- **D-52:** a lista de prioridade passou a verificar "escalado e impedido" antes de "já escalado" e a informar isso num campo próprio, `PrecisaTroca`. A tela decide pelo campo, e não pelo texto da mensagem. O Montar escala destaca esses escalados nas duas colunas e realça o botão Trocar. Após a correção e o reteste, o roteiro foi aprovado.

## 01 e 02/10/2026 — Arquitetura e modelo dimensional

Detalhes em [`arquitetura.md`](arquitetura.md) e [`modelo-de-dados.md`](modelo-de-dados.md).

- **D-53:** o Data Warehouse segue o esquema estrela, com duas tabelas fato (`fato_servico` e `fato_situacao_diaria`) que compartilham as dimensões (data, funcionário, escala e situação). O floco de neve foi descartado: com pouco volume de dados, a redundância da estrela custa pouco, e as consultas ficam mais simples. *As tabelas foram atualizadas pela D-65 a D-68.*
- **D-54:** a chave primária de cada fato é composta pelas chaves das dimensões, sem identificador próprio. O `id_funcionario_original` fica fora da chave da `fato_servico`, por ser opcional. Na `fato_situacao_diaria`, uma restrição única em (`id_data`, `id_funcionario`) garante uma só situação por pessoa por dia. *Substituída pela D-66.*
- **D-55:** a `fato_servico` guarda só serviços válidos. Os cancelados ficam de fora, porque o sistema permite cancelar e lançar de novo no mesmo dia (D-32), e a chave composta não admite os dois; por isso, os cancelamentos não entram nos painéis (RF-14). A situação diária tem uma lista fixa de cinco valores calculados pelo ETL: Em serviço, Impedido por atestado, Impedido por viagem a serviço, Escalado e impedido, e Livre. *A lista de situações foi substituída pela D-69.*
- **D-56:** as duas fatos têm grão diário. A análise por mês, trimestre, semestre e ano, como a das faltas, é feita por drill up na `dim_data`. A alternativa era uma fato mensal de faltas. *Substituída pela D-65.*
- **D-57:** a `dim_funcionario` guarda só os dados atuais, mais o campo `ativo`, sem histórico de versões. A escala de cada dia fica gravada na própria fato, e cargo e antiguidade quase não mudam; isso simplifica o ETL.
- **D-58:** a `dim_data` não tem coluna para o dia do mês: o nível "dia" da hierarquia é a própria `data`. Ela tem nome do mês e semestre, para completar a hierarquia. *Substituída pela D-65.*
- **D-59:** horas trabalhadas ficaram fora do modelo, porque não estão entre as métricas acompanhadas. Se forem pedidas, o cálculo é a quantidade de serviços × a duração do turno.
- **D-60:** o Data Warehouse fica no PostgreSQL, numa base separada do banco do sistema. Os painéis leem só o Data Warehouse, nunca o banco do sistema.
- **D-61:** a integração (ETL) será um script em Python, com as transformações em SQL, agendado para rodar ao fim de cada dia (cron no Linux; Agendador de Tarefas, se o servidor for Windows). A carga é incremental, pelos campos `CriadoEm` e `AtualizadoEm` (D-27). Quando há mudança, o ETL reprocessa os dias afetados: apaga as linhas desses dias nas fatos e grava de novo. O script terá tabela de controle da última carga, log de cada execução e usuário só de leitura no banco do sistema `[PREVISTO]`. *Substituída pela D-70 quanto à frequência e à forma da carga.*
- **D-62:** as planilhas não são importadas: importar exigiria ler formatações e fórmulas e completar informações que a planilha não tem, e o projeto trabalha só com dados fictícios (D-14). Numa implantação real, o ponto de partida seria o cadastro das pessoas, e do último serviço de cada uma, pelas próprias telas, que aceitam datas passadas.
- **D-63:** a ferramenta de visualização será escolhida por critério: software livre, sem custo de licença, rodando no servidor local e com drill up, drill down e cruzamento de dados. O Metabase está em avaliação; a escolha final ainda está em aberto.
- **D-64:** o Data Warehouse guarda o histórico. O painel abre na data mais recente, a mais consultada (RN-39), e permite consultar datas e períodos anteriores (RN-40). A situação do momento, em tempo real, fica na tela Escala do dia do sistema. *Substituída pela D-65 quanto ao painel, que abre no mês mais recente.*

## 03 a 08/10/2026 — Modelo dimensional v3

Detalhes em [`modelo-de-dados.md`](modelo-de-dados.md).

- **D-65:** o grão do tempo passa a ser o **mês** nas duas fatos, e a `dim_data` dá lugar à `dim_tempo` (uma linha por mês, com trimestre, semestre, ano e quantidade de dias). Decisão de modelagem: no grão diário, a quantidade seria sempre 1; no mensal, as medidas passam a ter sentido, e os totais continuam agregáveis por trimestre, semestre e ano. A situação de um dia continua em tempo real na tela Escala do dia. O painel abre no mês mais recente.
- **D-66:** todas as chaves estrangeiras das fatos fazem parte da chave primária composta, inclusive o motivo da troca. Saem da `fato_servico` o `id_funcionario_original` e o `foi_troca`: quem foi trocado passa a ser contado na própria linha, pela medida `qtd_substituido`, ao lado de `qtd_servico`.
- **D-67:** a `fato_situacao_diaria` vira `fato_situacao`, com a medida `qtd_dias` (dias do mês em cada situação). O efetivo atual e o disponível são médias do mês, divididas pelos dias do mês.
- **D-68:** criada a `dim_motivo_troca`, com a marca `conta_como_falta`. Falta é o escalado que não cumpriu o serviço por atestado médico, viagem a serviço ou não comparecimento. A troca combinada entre as pessoas não é falta. Férias, licença e curso não contam como falta, por serem afastamentos planejados. Mudar o critério exige só mudar a marca.
- **D-69:** a lista de situações passa a ter 8 valores: Em serviço; Impedido por atestado médico, viagem a serviço, férias, licença ou curso; Escalado e impedido; e Livre.
- **D-70:** a carga do Data Warehouse é mensal: no primeiro dia de cada mês, o ETL carrega o mês anterior, já fechado. Para carregar um mês, apaga as linhas daquele mês e grava de novo, o que permite recarregar um mês corrigido sem duplicar dados.
- **D-71:** os metadados do Data Warehouse ficam gravados no próprio banco (`COMMENT ON` em cada tabela e coluna), e o relatório de metadados é gerado a partir do catálogo do PostgreSQL ([`db/relatorio-metadados.sql`](../db/relatorio-metadados.sql)), para que a documentação não se separe do banco.
- **D-72:** a fila do dia (dias úteis ou fins de semana e feriados) fica fora do Data Warehouse: é uma informação do dia, não existe no grão mensal e não é usada nos painéis. O RF-15 foi retirado.
- **D-73:** os painéis ficam num portal web na rede local da organização, separado das telas do sistema de escalas e acessado pelo navegador. O acesso é da gestão e de quem monta a escala.

## 12/10/2026 — Execução com Docker

- **D-74:** o sistema passa a rodar inteiro com Docker Compose: o banco do sistema e o Data Warehouse no container `postgres-rodizio`, e a API com as telas no container `api-rodizio`, compilado por um `Dockerfile` em dois estágios (SDK para compilar, runtime para executar, com usuário sem privilégios). Na primeira subida, o script `db/init/01-cria-dw.sh` cria o Data Warehouse; a API aplica as migrations ao iniciar, no lugar do `dotnet ef database update`, e só sobe depois que o banco responde. Assim, o ambiente sobe com um comando, sem instalar o .NET. O `.gitattributes` mantém os scripts `.sh` com fim de linha LF, para funcionarem quando o repositório é clonado no Windows. As portas continuam só em `127.0.0.1`.
