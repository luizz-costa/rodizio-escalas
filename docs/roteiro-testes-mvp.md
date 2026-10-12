# Roteiro de Testes do MVP Operacional

> **Arquivo:** `docs/roteiro-testes-mvp.md`
> **Versão:** 1.2 · 12/10/2026 (cenário fictício da Organização Exemplo: escalas A a F e dados de demonstração atuais)
> **Escopo:** sistema web (API + banco operacional + telas)
> **Uso:** validar o MVP antes de uma demonstração. A ordem das seções segue o roteiro da demonstração.

---

## 1. Registro da execução

| Campo | Valor |
|---|---|
| Data da execução (chamada de **D** abaixo) | |
| Versão testada | |
| Navegador | |
| Resultado geral | a executar |

> **Nota:** Roteiro executado na versão original em 26/09/2026. Nesta versão, o CT-02 e os casos de pessoas fixas foram reconferidos pela API.

---

## 2. Pré-condições

Execute na ordem, em `src/Rodizio.Api`:

1. `dotnet ef database drop`, respondendo **y**.
2. `dotnet ef database update`, que deve terminar em `Done` com a migration `InitialCreate`.
3. `dotnet run -- --demo`, que deve terminar com *"Demonstração gerada: 51 pessoas, 2 dias especiais, 5 impedimentos, … serviços de 01/09/2026 até D-1"*.
4. `dotnet run`, e abrir `http://localhost:5001/`.
5. No navegador: **F12 → Rede → "Desativar cache"** marcado, e recarregar com **Ctrl+F5**.

**Convenção de datas:** **D** = o dia da execução; **D+1** = amanhã; **D-1** = ontem. O roteiro usa datas relativas para poder ser repetido em qualquer dia.

**Pessoas fixas do gerador**, úteis nos testes:

| Pessoa | Escala | Particularidade |
|---|---|---|
| Func. 029 | Escala D | Desativado |
| Func. 012, 028, 051 | Escala B, Escala D, Escala F | Só fila de fins de semana e feriados |
| Func. 039 | Escala E | Só sábado |
| Func. 035 | Escala E | Atestado de **D+1 a D+3** |
| Func. 043 | Escala F | Viagem a serviço desde 23/09, em aberto |

---

## 3. Casos de teste

Na coluna **Resultado**, preencha OK ou descreva o que aconteceu.

### 3.1 Abertura e visão do dia

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-01 | Abrir `http://localhost:5001/` | Tela "Escala do dia" em **D**, com dia da semana, selo da fila correto (fins de semana e feriados em sábado, domingo ou feriado), 4 cartões e 6 quadros | RF-09, RN-08 | a executar |
| CT-02 | Clicar em "Dia anterior" até **D-1** | Cartão "De serviço" = **10**; quadros com Escala A 1, Escala B 1, Escala C 2, Escala D 1, Escala E 2, Escala F 3 | RF-09, D-40, D-41 | a executar |
| CT-03 | Conferir o cabeçalho | "Organização Exemplo · dados fictícios" visível | RNF-01, D-45 | a executar |

### 3.2 Cadastros

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-04 | Funcionários → cadastrar sem escolher escala | "Escala não informada ou inexistente." **embaixo do campo Escala** | RF-01, D-20, D-25 | a executar |
| CT-05 | Cadastrar "Func. 052", matrícula FIC-052, cargo Agente, escala Escala E, antiguidade 152, todos os dias | Aviso verde; aparece na tabela com "Todos os dias" | RF-01 | a executar |
| CT-06 | Editar o Func. 052: deixar só "Sáb" | Formulário vem preenchido; após salvar, "Só Sáb" na tabela | RF-01, D-22, D-46 | a executar |
| CT-07 | Montar escala → Escala E em uma data de **segunda a sexta** | Func. 052 aparece no fim com "Dia não permitido" | RF-05, RN-28 | a executar |
| CT-08 | Desativar o Func. 052 e voltar ao Montar escala (mesma data) | Func. 052 **não aparece** mais na lista | RF-01, D-05 | a executar |

### 3.3 Feriados (dias especiais)

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-09 | Dias especiais → conferir 07/09 | Em cinza, "Já faz parte do histórico", **sem** botão Remover | RF-03, D-24, D-49 | a executar |
| CT-10 | Adicionar sem data | "Informe a data." embaixo do campo Data | RF-03, D-49 | a executar |
| CT-11 | Adicionar um **dia útil futuro** (ex.: D+5, se for de segunda a sexta) | Aviso verde com o dia da semana | RF-03, RN-18 | a executar |
| CT-12 | Montar escala nessa data | Selo **"Fila de fins de semana e feriados"**, mesmo sendo dia útil | RF-03, RN-17 | a executar |
| CT-13 | Remover esse dia especial e voltar ao Montar escala | Selo volta a **"Fila de dias úteis"** | RF-03, RN-17, D-24 | a executar |

### 3.4 Montar a escala de amanhã

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-14 | Abrir Montar escala | Abre em **D+1**, aba Escala A | RF-02, RN-01, D-50 | a executar |
| CT-15 | Aba Escala E | Lista numerada; quem tem **mais** dias sem serviço vem primeiro; em empate de dias, a antiguidade menor vem antes | RF-05, RN-09, RN-11, D-38 | a executar |
| CT-16 | Conferir o Func. 035 na Escala E | No fim, sem posição, com a etiqueta amarela "Impedido (atestado médico)" | RF-05, RN-19 | a executar |
| CT-17 | Escalar, **em cada uma das 6 abas**, os primeiros da lista: Escala A 1, Escala B 1, Escala C 2, Escala D 1, Escala E 2, Escala F 3 | A cada clique, o escalado vai para a coluna da direita e para o fim da lista ("Já está escalado nesta data") | RF-02, RF-06 | a executar |
| CT-18 | "Próximo dia" (**D+2**), aba Escala E | Os 2 escalados em D+1 aparecem com "Cumpriu serviço no dia anterior" | RF-05, RN-14 | a executar |
| CT-19 | Escala do dia em **D+1** | Cartão "De serviço" = **10**; cada quadro mostra quem foi escalado | RF-09 | a executar |

### 3.5 Alteração de última hora (o fluxo principal do responsável pela escala)

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-20 | Impedimentos → registrar **atestado** para um escalado da Escala E em D+1, início e fim D+1 | Aviso verde (o sistema aceita) | RF-04, D-35 | a executar |
| CT-21 | Escala do dia em **D+1** | Alerta no topo: "… está escalado em … e impedido (atestado médico). Faça a troca." com botão **Resolver**; no quadro Escala E, a etiqueta "Impedido: fazer troca" | RF-09, D-35 | a executar |
| CT-22 | Clicar em **Resolver** | Abre o Montar escala na Escala E, em D+1. O escalado impedido aparece à esquerda com "Escalado, mas impedido (atestado médico): faça a troca" e, à direita, com "Impedido: fazer troca" e o botão Trocar destacado | RF-07, D-51, D-52 | a executar |
| CT-23 | **Trocar** o escalado impedido pelo primeiro sugerido | A janela lista os elegíveis na ordem, com o primeiro marcado; após confirmar, aparece "Troca: no lugar de …" | RF-07, RN-30, D-34 | a executar |
| CT-24 | Voltar à Escala do dia em D+1 | **O alerta sumiu** | RF-09, D-35 | a executar |

### 3.6 Impedimentos: regras e ciclo de vida

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-25 | Registrar **viagem a serviço** para o Func. 035, início D+2 | Aviso **no topo**: "Esta pessoa já tem um impedimento ativo (AtestadoMedico)…" | RF-04, D-30 | a executar |
| CT-26 | Registrar sem funcionário e sem data de início | **Dois** erros, cada um embaixo do seu campo | RF-04, D-25 | a executar |
| CT-27 | **Encerrar** o atestado do Func. 035 com a data D+1 | Janela com o nome dele; após confirmar, a data de fim muda na tabela | RF-04, D-28, D-48 | a executar |
| CT-28 | Montar escala, Escala E, em **D+2** | O Func. 035 **não** está mais impedido | RF-04, RN-19 | a executar |
| CT-29 | **Cancelar** o impedimento do CT-20 e mostrar "Todos, inclusive cancelados" | Linha em cinza, "Cancelado", sem botões | RF-04, D-29 | a executar |

### 3.7 Correção de lançamento

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-30 | Montar escala em D+1, **Desfazer** um escalado da Escala F | Confirmação; ele sai da direita e volta à lista | RF-08, D-17 | a executar |

### 3.8 Integridade dos dados (banco)

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-31 | Rodar a consulta de "dobrar" (abaixo) | **Nenhuma linha** | RN-14 | a executar |
| CT-32 | `SELECT COUNT(*) FROM "Servicos" WHERE "Cancelado";` | **Pelo menos 1**: o cancelamento do CT-30 ficou guardado, visível para o ETL | RF-08, D-17, D-27 | a executar |

```sql
-- CT-31: ninguém com serviço em dois dias seguidos
SELECT a."FuncionarioId", a."Data"
FROM "Servicos" a
JOIN "Servicos" b
  ON b."FuncionarioId" = a."FuncionarioId"
 AND b."Data" = a."Data" + 1
WHERE NOT a."Cancelado" AND NOT b."Cancelado";
```

### 3.9 Segurança e ambiente

| ID | Passo | Resultado esperado | Rastreia | Resultado |
|---|---|---|---|---|
| CT-33 | Com F12 → Rede aberto, navegar pelas 5 telas | **Todas** as requisições vão para `localhost:5001`; nenhuma para servidores externos | RNF-14, D-43 | a executar |
| CT-34 | Tentar remover o dia 07/09 pelo Swagger (`DELETE /api/DiasEspeciais/1`) | 400, "Só é possível remover dias especiais futuros…" | RF-03, D-24 | a executar |

---

## 4. Critério de aceite

O MVP operacional está **aprovado** quando todos os casos de CT-01 a CT-34 estiverem OK.

Uma falha em caso marcado com regra de negócio (RN) bloqueia a entrega. Uma falha só visual pode ser registrada como ressalva.

---

## 5. Decisões de produto que os testes confirmam

Comportamentos definidos para o produto e verificados pelos casos acima:

| Regra | Comportamento |
|---|---|
| RN-12 | No empate de dias sem serviço, o número de antiguidade **menor** vai primeiro |
| RN-05 | Quem **nunca serviu** na fila vai primeiro |
| RN-06 | O sistema não fixa quantas pessoas são escaladas por dia: decide o responsável |
| RN-24 | Motivos de impedimento: atestado médico e viagem a serviço (novos motivos entram sem migration) |
| RN-29 (D-33) | Em dia especial vale "pode em feriado", mesmo que caia no fim de semana |
| D-48 | Encerrar um impedimento sugere **hoje** como último dia |

---

## 6. Limitações conhecidas (não bloqueiam o MVP)

- A API aceita nome vazio no cadastro de funcionário (melhoria candidata, D-46).
- Os bloqueios da lista de prioridade repetem as verificações do registro de serviço (dívida técnica, D-38).
- Não há login nem perfis de acesso (fora do MVP).
- Não há testes automatizados; a verificação é feita por este roteiro.
