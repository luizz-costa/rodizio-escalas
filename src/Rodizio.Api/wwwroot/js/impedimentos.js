// Tela Impedimentos (T-03): registrar, corrigir, encerrar e cancelar atestados e viagens a serviço.
montarLayout('impedimentos.html');

const aviso = document.getElementById('aviso');
const formulario = document.getElementById('formulario');
const campoFuncionario = document.getElementById('funcionario');
const campoMotivo = document.getElementById('motivo');
const campoInicio = document.getElementById('inicio');
const campoFim = document.getElementById('fim');
const modo = document.getElementById('modo');
const dataFiltro = document.getElementById('data-filtro');
const dialogo = document.getElementById('dialogo-encerrar');

// Estado da tela.
let escalas = [];
let funcionarios = [];
let impedimentos = [];
let editandoId = null;     // null = registrando; um número = corrigindo
let encerrandoId = null;   // qual impedimento o diálogo está encerrando

function funcionario(id) {
  return funcionarios.find(f => f.id === id);
}

function nomeDaEscala(id) {
  return escalas.find(e => e.id === id)?.nome ?? 'Sem escala';
}

// Datas AAAA-MM-DD podem ser comparadas como texto.
function situacao(i) {
  const hoje = dataIso();
  if (i.cancelado) return 'Cancelado';
  if (i.dataInicio > hoje) return 'Previsto';
  if (!i.dataFim) return 'Em aberto';
  if (i.dataFim === hoje) return 'Termina hoje';
  return i.dataFim < hoje ? 'Encerrado' : 'Em andamento';
}

// ---------- Carregar dados ----------

async function carregarCadastros() {
  // As duas consultas saem ao mesmo tempo.
  [escalas, funcionarios] = await Promise.all([api('/api/Escalas'), api('/api/Funcionarios')]);

  // Lista de escolha agrupada por escala, só com quem está ativo.
  const grupos = escalas.map(e => {
    const pessoas = funcionarios.filter(f => f.ativo && f.escalaId === e.id);
    if (pessoas.length === 0) return '';
    return `<optgroup label="${esc(e.nome)}">` +
      pessoas.map(f => `<option value="${f.id}">${esc(f.nome)}</option>`).join('') +
      '</optgroup>';
  }).join('');

  campoFuncionario.innerHTML = '<option value="">Selecione…</option>' + grupos;
}

async function carregarImpedimentos() {
  const caminho = modo.value === 'ativos'
    ? `/api/Impedimentos?ativosEm=${dataFiltro.value}`
    : '/api/Impedimentos';
  impedimentos = await api(caminho);

  document.getElementById('linhas').innerHTML = impedimentos.map(i => {
    const f = funcionario(i.funcionarioId);
    const acoes = i.cancelado ? '' : `
      <button type="button" class="secundario pequeno" data-acao="editar" data-id="${i.id}">Corrigir</button>
      <button type="button" class="secundario pequeno" data-acao="encerrar" data-id="${i.id}">Encerrar</button>
      <button type="button" class="secundario pequeno" data-acao="cancelar" data-id="${i.id}">Cancelar</button>`;

    return `
      <tr class="${i.cancelado ? 'desativado' : ''}">
        <td>${esc(f?.nome ?? '—')}</td>
        <td>${esc(nomeDaEscala(f?.escalaId))}</td>
        <td><span class="etiqueta-impedido">${esc(MOTIVOS[i.motivo] ?? i.motivo)}</span></td>
        <td>${formatarData(i.dataInicio)}</td>
        <td>${i.dataFim ? formatarData(i.dataFim) : 'Sem data de fim'}</td>
        <td>${situacao(i)}</td>
        <td><div class="acoes-linha">${acoes}</div></td>
      </tr>`;
  }).join('') || '<tr><td colspan="7" class="apoio">Nenhum impedimento.</td></tr>';
}

function recarregar() {
  carregarImpedimentos().catch(erro => mostrarErro(erro, { aviso }));
}

// ---------- Formulário ----------

// Só inclui as datas quando preenchidas: dataInicio é DateOnly sem "?" na API.
// Omitida, chega vazia e a NOSSA validação responde em português;
// enviada como null, o conversor de JSON quebraria antes (mensagem em inglês).
function lerFormulario() {
  const corpo = {
    funcionarioId: campoFuncionario.value ? Number(campoFuncionario.value) : 0,
    motivo: campoMotivo.value,
    dataFim: campoFim.value || null
  };
  if (campoInicio.value) {
    corpo.dataInicio = campoInicio.value;
  }
  return corpo;
}

function limparFormulario() {
  formulario.reset();
  editandoId = null;
  campoFuncionario.disabled = false;
  document.getElementById('titulo-form').textContent = 'Registrar impedimento';
  document.getElementById('salvar').textContent = 'Registrar';
  document.getElementById('cancelar-edicao').hidden = true;
  limparErros({ formulario });
}

formulario.addEventListener('submit', async (evento) => {
  evento.preventDefault();
  limparErros({ aviso, formulario });
  const corpo = lerFormulario();

  try {
    if (editandoId) {
      await api(`/api/Impedimentos/${editandoId}`, { metodo: 'PUT', corpo });
      avisar(aviso, 'Impedimento corrigido.');
    } else {
      await api('/api/Impedimentos', { metodo: 'POST', corpo });
      avisar(aviso, `Impedimento registrado para ${funcionario(corpo.funcionarioId).nome}.`);
    }
    limparFormulario();
    await carregarImpedimentos();
  } catch (erro) {
    mostrarErro(erro, { aviso, formulario });
  }
});

document.getElementById('cancelar-edicao').addEventListener('click', limparFormulario);

// ---------- Ações da tabela ----------

document.getElementById('linhas').addEventListener('click', async (evento) => {
  const botao = evento.target.closest('button[data-acao]');
  if (!botao) return;

  const id = Number(botao.dataset.id);
  const i = impedimentos.find(x => x.id === id);
  const nome = funcionario(i.funcionarioId)?.nome ?? '';
  const acao = botao.dataset.acao;

  if (acao === 'editar') {
    // Na correção a pessoa não muda (D-29): o campo fica travado.
    editandoId = id;
    campoFuncionario.value = i.funcionarioId;
    campoFuncionario.disabled = true;
    campoMotivo.value = i.motivo;
    campoInicio.value = i.dataInicio;
    campoFim.value = i.dataFim ?? '';
    document.getElementById('titulo-form').textContent = `Corrigir impedimento de ${nome}`;
    document.getElementById('salvar').textContent = 'Salvar correção';
    document.getElementById('cancelar-edicao').hidden = false;
    limparErros({ aviso, formulario });
    formulario.scrollIntoView();
    return;
  }

  if (acao === 'encerrar') {
    encerrandoId = id;
    document.getElementById('nome-encerrar').textContent = nome;
    document.getElementById('data-encerrar').value = dataIso();
    dialogo.returnValue = '';   // sem isto, fechar com Esc repetiria a escolha anterior
    dialogo.showModal();
    return;
  }

  if (acao === 'cancelar') {
    if (!confirm(`Cancelar o impedimento de ${nome}? Ele deixa de valer e sai da análise, mas o registro continua guardado.`)) {
      return;
    }
    try {
      await api(`/api/Impedimentos/${id}/cancelar`, { metodo: 'PATCH' });
      avisar(aviso, `Impedimento de ${nome} cancelado.`);
      await carregarImpedimentos();
    } catch (erro) {
      mostrarErro(erro, { aviso });
    }
  }
});

// O diálogo fecha sozinho; aqui vemos QUAL botão o fechou.
dialogo.addEventListener('close', async () => {
  if (dialogo.returnValue !== 'confirmar') return;

  const data = document.getElementById('data-encerrar').value;
  try {
    await api(`/api/Impedimentos/${encerrandoId}/encerrar?data=${data}`, { metodo: 'PATCH' });
    avisar(aviso, `Impedimento encerrado em ${formatarData(data)}.`);
    await carregarImpedimentos();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
});

// ---------- Filtros ----------

modo.addEventListener('change', () => {
  document.getElementById('campo-data-filtro').hidden = modo.value !== 'ativos';
  recarregar();
});
dataFiltro.addEventListener('change', recarregar);

// ---------- Início ----------

campoInicio.defaultValue = dataIso();   // o "reset" do formulário volta para hoje
dataFiltro.value = dataIso();

(async () => {
  try {
    await carregarCadastros();
    await carregarImpedimentos();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
})();