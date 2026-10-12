// Tela Funcionários (T-04): cadastrar, editar, desativar e reativar.
montarLayout('funcionarios.html');

// Os 8 dias permitidos (D-22). Uma lista só gera as caixinhas, o resumo e o corpo do envio.
const DIAS = [
  { campo: 'podeSegunda', rotulo: 'Seg' },
  { campo: 'podeTerca',   rotulo: 'Ter' },
  { campo: 'podeQuarta',  rotulo: 'Qua' },
  { campo: 'podeQuinta',  rotulo: 'Qui' },
  { campo: 'podeSexta',   rotulo: 'Sex' },
  { campo: 'podeSabado',  rotulo: 'Sáb' },
  { campo: 'podeDomingo', rotulo: 'Dom' },
  { campo: 'podeFeriado', rotulo: 'Feriado' }
];

const aviso = document.getElementById('aviso');
const formulario = document.getElementById('formulario');
const campoEscala = document.getElementById('escala');
const filtro = document.getElementById('filtro');

// Estado da tela.
let escalas = [];          // [{ id, nome }]
let funcionarios = [];     // o que está na tabela agora
let editandoId = null;     // null = cadastrando; um número = editando esse funcionário

// Caixinhas dos dias, geradas a partir da lista DIAS (todas marcadas por padrão, RN-19).
document.getElementById('dias').innerHTML = DIAS.map(d =>
  `<label><input type="checkbox" id="${d.campo}" checked> ${d.rotulo}</label>`
).join('');

function nomeDaEscala(id) {
  return escalas.find(e => e.id === id)?.nome ?? 'Sem escala';
}

function resumoDias(f) {
  const marcados = DIAS.filter(d => f[d.campo]);
  if (marcados.length === DIAS.length) return 'Todos os dias';
  if (marcados.length === 0) return 'Nenhum dia';

  const soFimDeSemana = marcados.map(d => d.campo).sort().join() === 'podeDomingo,podeFeriado,podeSabado';
  if (soFimDeSemana) return 'Só fins de semana e feriados';

  return 'Só ' + marcados.map(d => d.rotulo).join(', ');
}

// ---------- Carregar dados ----------

async function carregarEscalas() {
  escalas = await api('/api/Escalas');
  const opcoes = escalas.map(e => `<option value="${e.id}">${esc(e.nome)}</option>`).join('');
  campoEscala.innerHTML = '<option value="">Selecione…</option>' + opcoes;
  filtro.innerHTML = '<option value="">Todas</option>' + opcoes;
}

async function carregarFuncionarios() {
  const caminho = filtro.value ? `/api/Funcionarios?escalaId=${filtro.value}` : '/api/Funcionarios';
  funcionarios = await api(caminho);

  document.getElementById('linhas').innerHTML = funcionarios.map(f => `
    <tr class="${f.ativo ? '' : 'desativado'}">
      <td>${esc(f.nome)}</td>
      <td>${esc(f.matricula)}</td>
      <td>${esc(f.cargo)}</td>
      <td>${esc(nomeDaEscala(f.escalaId))}</td>
      <td class="numero">${f.antiguidade ?? '—'}</td>
      <td>${resumoDias(f)}</td>
      <td>${f.ativo ? 'Ativo' : 'Desativado'}</td>
      <td>
        <div class="acoes-linha">
          <button type="button" class="secundario pequeno" data-acao="editar" data-id="${f.id}">Editar</button>
          <button type="button" class="secundario pequeno" data-acao="${f.ativo ? 'desativar' : 'reativar'}" data-id="${f.id}">
            ${f.ativo ? 'Desativar' : 'Reativar'}
          </button>
        </div>
      </td>
    </tr>`).join('')
    || '<tr><td colspan="8" class="apoio">Nenhum funcionário encontrado.</td></tr>';
}

// ---------- Formulário ----------

function lerFormulario() {
  const antiguidade = document.getElementById('antiguidade').value;
  const corpo = {
    nome: document.getElementById('nome').value.trim(),
    matricula: document.getElementById('matricula').value.trim(),
    cargo: document.getElementById('cargo').value.trim(),
    escalaId: campoEscala.value ? Number(campoEscala.value) : null,
    antiguidade: antiguidade ? Number(antiguidade) : null
  };
  DIAS.forEach(d => { corpo[d.campo] = document.getElementById(d.campo).checked; });
  return corpo;
}

// Na edição, o formulário começa com TODOS os dados atuais:
// o PUT substitui os campos, e o que não for reenviado se perde (lição do B2).
function preencherFormulario(f) {
  document.getElementById('nome').value = f.nome;
  document.getElementById('matricula').value = f.matricula;
  document.getElementById('cargo').value = f.cargo;
  campoEscala.value = f.escalaId ?? '';
  document.getElementById('antiguidade').value = f.antiguidade ?? '';
  DIAS.forEach(d => { document.getElementById(d.campo).checked = f[d.campo]; });
}

function limparFormulario() {
  formulario.reset();   // volta ao estado inicial do HTML (todas as caixinhas marcadas)
  editandoId = null;
  document.getElementById('titulo-form').textContent = 'Novo funcionário';
  document.getElementById('salvar').textContent = 'Cadastrar';
  document.getElementById('cancelar').hidden = true;
  limparErros({ formulario });
}

formulario.addEventListener('submit', async (evento) => {
  evento.preventDefault();   // sem isto, o navegador recarregaria a página
  limparErros({ aviso, formulario });
  const corpo = lerFormulario();

  try {
    if (editandoId) {
      await api(`/api/Funcionarios/${editandoId}`, { metodo: 'PUT', corpo });
      avisar(aviso, `${corpo.nome} atualizado.`);
    } else {
      const criado = await api('/api/Funcionarios', { metodo: 'POST', corpo });
      avisar(aviso, `${criado.nome} cadastrado.`);
    }
    limparFormulario();
    await carregarFuncionarios();
  } catch (erro) {
    mostrarErro(erro, { aviso, formulario });
  }
});

document.getElementById('cancelar').addEventListener('click', limparFormulario);

// ---------- Ações da tabela (delegação de eventos: um ouvinte para todos os botões) ----------

document.getElementById('linhas').addEventListener('click', async (evento) => {
  const botao = evento.target.closest('button[data-acao]');
  if (!botao) return;

  const id = Number(botao.dataset.id);
  const f = funcionarios.find(x => x.id === id);
  const acao = botao.dataset.acao;

  if (acao === 'editar') {
    editandoId = id;
    preencherFormulario(f);
    document.getElementById('titulo-form').textContent = `Editar ${f.nome}`;
    document.getElementById('salvar').textContent = 'Salvar alterações';
    document.getElementById('cancelar').hidden = false;
    limparErros({ aviso, formulario });
    formulario.scrollIntoView();
    return;
  }

  if (acao === 'desativar' &&
      !confirm(`Desativar ${f.nome}? A pessoa sai das listas de prioridade, mas o histórico é mantido.`)) {
    return;
  }

  try {
    await api(`/api/Funcionarios/${id}/${acao}`, { metodo: 'PATCH' });
    avisar(aviso, `${f.nome} ${acao === 'desativar' ? 'desativado' : 'reativado'}.`);
    await carregarFuncionarios();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
});

filtro.addEventListener('change', () => {
  carregarFuncionarios().catch(erro => mostrarErro(erro, { aviso }));
});

// ---------- Início ----------

(async () => {
  try {
    await carregarEscalas();
    await carregarFuncionarios();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
})();