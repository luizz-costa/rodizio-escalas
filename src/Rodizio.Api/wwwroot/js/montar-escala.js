// Tela Montar escala (T-02): a lista de prioridade (há mais tempo sem serviço primeiro) e o registro dos serviços.
montarLayout('montar-escala.html');

const aviso = document.getElementById('aviso');
const avisoData = document.getElementById('aviso-data');
const campoData = document.getElementById('data');
const abas = document.getElementById('abas');
const linhas = document.getElementById('linhas');
const listaEscalados = document.getElementById('escalados');
const dialogo = document.getElementById('dialogo-troca');
const campoSubstituto = document.getElementById('substituto');

// Estado da tela.
let escalas = [];
let nomes = {};           // id do funcionário -> nome (todos, inclusive desativados)
let escalaAtual = null;   // id da aba selecionada
let prioridade = null;    // última resposta da lista de prioridade
let servicos = [];        // serviços ativos da data e escala
let trocandoId = null;    // serviço que a janela de troca está trocando

// ---------- Textos ----------


function situacaoInelegivel(item) {
  const destacar = item.precisaTroca || item.motivo.startsWith('Impedido');
  return destacar
    ? `<span class="etiqueta-impedido">${esc(textoMotivo(item.motivo))}</span>`
    : esc(item.motivo);
}

function textoDias(item) {
  return item.diasSemServicoNaFila === null
    ? 'sem registro na fila'
    : `${item.diasSemServicoNaFila} dia(s) sem serviço`;
}

// ---------- Carregar ----------

async function carregarCadastros() {
  const [listaEscalas, funcionarios] = await Promise.all([api('/api/Escalas'), api('/api/Funcionarios')]);
  escalas = listaEscalas;
  funcionarios.forEach(f => { nomes[f.id] = f.nome; });

  abas.innerHTML = escalas.map(e =>
    `<button type="button" role="tab" data-escala="${e.id}">${esc(e.nome)}</button>`).join('');
}

async function carregar() {
  const data = campoData.value;

  abas.querySelectorAll('button').forEach(b =>
    b.setAttribute('aria-selected', String(Number(b.dataset.escala) === escalaAtual)));

  // As duas consultas saem juntas.
  [prioridade, servicos] = await Promise.all([
    api(`/api/Escalas/${escalaAtual}/prioridade?data=${data}`),
    api(`/api/Servicos?data=${data}&escalaId=${escalaAtual}`)
  ]);

  // Cabeçalho: dia da semana e fila. A fila vem da API, que conhece os dias especiais.
  document.getElementById('titulo-data').innerHTML =
    `${comMaiuscula(diaDaSemana(data))}, ${formatarData(data)} ${seloFila(prioridade.fila)}`;

  // Aviso ao mexer numa data passada: os lançamentos mudam o histórico.
  avisoData.hidden = data >= dataIso();
  avisoData.textContent = 'Você está vendo uma data passada. Lançamentos aqui alteram o histórico da escala.';

  // Esquerda: a lista de prioridade, exatamente na ordem calculada pela API.
  linhas.innerHTML = prioridade.itens.map(i => `
    <tr class="${i.elegivel ? '' : 'inelegivel'}">
      <td class="posicao">${i.posicao ?? '—'}</td>
      <td>${esc(i.nome)}</td>
      <td>${esc(i.cargo)}</td>
      <td class="numero">${i.diasSemServicoNaFila ?? 'sem registro'}</td>
      <td>${formatarData(i.ultimoServicoNaFila)}</td>
      <td class="numero">${i.antiguidade ?? '—'}</td>
      <td>${i.elegivel ? 'Pode cumprir' : situacaoInelegivel(i)}</td>
      <td>${i.elegivel
        ? `<button type="button" class="pequeno" data-acao="escalar" data-id="${i.funcionarioId}">Escalar</button>`
        : ''}</td>
    </tr>`).join('')
    || '<tr><td colspan="8" class="apoio">Nenhum funcionário ativo nesta escala.</td></tr>';

  // Direita: quem já está escalado nesta data. Quem ficou impedido depois ganha destaque.
  listaEscalados.innerHTML = servicos.map(s => {
    const precisaTroca = prioridade.itens.some(i => i.funcionarioId === s.funcionarioId && i.precisaTroca);
    return `
    <li>
      <span class="nome">${esc(nomes[s.funcionarioId] ?? '—')}</span>
      ${s.escaladoOriginalId ? `<small>Troca: no lugar de ${esc(nomes[s.escaladoOriginalId] ?? '—')}</small>` : ''}
      ${precisaTroca ? '<span class="etiqueta-impedido">Impedido: fazer troca</span>' : ''}
      <div class="acoes-linha inicio">
        <button type="button" class="${precisaTroca ? '' : 'secundario '}pequeno" data-acao="trocar" data-id="${s.id}">Trocar</button>
        <button type="button" class="secundario pequeno" data-acao="desfazer" data-id="${s.id}">Desfazer</button>
      </div>
    </li>`;
  }).join('') || '<li class="apoio">Ninguém escalado ainda.</li>';

  document.getElementById('contagem').textContent = servicos.length;
}

function recarregar() {
  carregar().catch(erro => mostrarErro(erro, { aviso }));
}

// ---------- Navegação: abas e datas ----------

abas.addEventListener('click', (evento) => {
  const botao = evento.target.closest('button[data-escala]');
  if (!botao) return;
  escalaAtual = Number(botao.dataset.escala);
  limparErros({ aviso });
  recarregar();
});

campoData.addEventListener('change', () => { limparErros({ aviso }); recarregar(); });

document.getElementById('anterior').addEventListener('click', () => {
  campoData.value = somarDias(campoData.value, -1);
  limparErros({ aviso });
  recarregar();
});

document.getElementById('proximo').addEventListener('click', () => {
  campoData.value = somarDias(campoData.value, 1);
  limparErros({ aviso });
  recarregar();
});

// ---------- Escalar ----------

linhas.addEventListener('click', async (evento) => {
  const botao = evento.target.closest('button[data-acao="escalar"]');
  if (!botao) return;

  const funcionarioId = Number(botao.dataset.id);
  botao.disabled = true;   // evita o clique duplo (o índice único do banco é a segunda proteção)

  try {
    await api('/api/Servicos', {
      metodo: 'POST',
      corpo: { data: campoData.value, escalaId: escalaAtual, funcionarioId }
    });
    avisar(aviso, `${nomes[funcionarioId]} escalado para ${formatarData(campoData.value)}.`);
    await carregar();   // a API recalcula a lista; a tela não "adivinha" a nova ordem
  } catch (erro) {
    mostrarErro(erro, { aviso });
    botao.disabled = false;
  }
});

// ---------- Trocar e desfazer ----------

listaEscalados.addEventListener('click', async (evento) => {
  const botao = evento.target.closest('button[data-acao]');
  if (!botao) return;

  const s = servicos.find(x => x.id === Number(botao.dataset.id));
  const nome = nomes[s.funcionarioId];

  if (botao.dataset.acao === 'desfazer') {
    if (!confirm(`Desfazer o lançamento de ${nome}? O serviço é cancelado, mas o registro fica guardado.`)) {
      return;
    }
    try {
      await api(`/api/Servicos/${s.id}/cancelar`, { metodo: 'PATCH' });
      avisar(aviso, `Lançamento de ${nome} desfeito.`);
      await carregar();
    } catch (erro) {
      mostrarErro(erro, { aviso });
    }
    return;
  }

  // Trocar: a janela sugere os elegíveis NA ORDEM da prioridade (o 1º, há mais tempo sem serviço, já vem marcado).
  const elegiveis = prioridade.itens.filter(i => i.elegivel);
  if (elegiveis.length === 0) {
    avisar(aviso, 'Não há ninguém disponível para a troca nesta escala e data.', 'alerta');
    return;
  }

  trocandoId = s.id;
  document.getElementById('nome-troca').textContent = nome;
  campoSubstituto.innerHTML = elegiveis.map(i =>
    `<option value="${i.funcionarioId}">${i.posicao}º ${esc(i.nome)} (${textoDias(i)})</option>`).join('');
  dialogo.returnValue = '';
  dialogo.showModal();
});

dialogo.addEventListener('close', async () => {
  if (dialogo.returnValue !== 'confirmar') return;

  const novoFuncionarioId = Number(campoSubstituto.value);
  try {
    await api(`/api/Servicos/${trocandoId}/troca`, { metodo: 'PUT', corpo: { novoFuncionarioId } });
    avisar(aviso, `Troca registrada: ${nomes[novoFuncionarioId]} cumpre o serviço.`);
    await carregar();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
});

// ---------- Início ----------
// A URL pode trazer a escala e a data: montar-escala.html?escala=5&data=2026-09-26
// Sem isso, abre em AMANHÃ: o normal é montar a escala na véspera (RN-01).

(async () => {
  try {
    await carregarCadastros();

    const parametros = new URLSearchParams(location.search);
    campoData.value = parametros.get('data') || somarDias(dataIso(), 1);
    escalaAtual = Number(parametros.get('escala')) || escalas[0].id;

    await carregar();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
})();