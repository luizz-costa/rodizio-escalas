// Tela Dias especiais (T-05): feriados concedidos entram na fila de fins de semana e feriados (RN-17, RN-18).
montarLayout('dias-especiais.html');

const aviso = document.getElementById('aviso');
const formulario = document.getElementById('formulario');
const campoData = document.getElementById('data');
const campoDescricao = document.getElementById('descricao');
const linhas = document.getElementById('linhas');

let dias = [];

async function carregar() {
  dias = await api('/api/DiasEspeciais');   // a API já devolve em ordem de data
  const hoje = dataIso();

  linhas.innerHTML = dias.map(d => {
    // Comodidade da tela: só oferece "Remover" onde a API permite (D-24).
    const futuro = d.data > hoje;
    const acao = futuro
      ? `<button type="button" class="secundario pequeno" data-acao="remover" data-id="${d.id}">Remover</button>`
      : '<span class="apoio">Já faz parte do histórico</span>';

    return `
      <tr class="${futuro ? '' : 'desativado'}">
        <td>${formatarData(d.data)}</td>
        <td>${diaDaSemana(d.data)}</td>
        <td>${esc(d.descricao) || '—'}</td>
        <td><div class="acoes-linha">${acao}</div></td>
      </tr>`;
  }).join('') || '<tr><td colspan="4" class="apoio">Nenhum dia especial cadastrado.</td></tr>';
}

formulario.addEventListener('submit', async (evento) => {
  evento.preventDefault();
  limparErros({ aviso, formulario });

  // A data só vai no JSON se foi preenchida (mesmo motivo do C3).
  const corpo = { descricao: campoDescricao.value.trim() };
  if (campoData.value) {
    corpo.data = campoData.value;
  }

  try {
    const criado = await api('/api/DiasEspeciais', { metodo: 'POST', corpo });
    avisar(aviso, `${formatarData(criado.data)} (${diaDaSemana(criado.data)}) agora é dia especial e entra na fila de fins de semana e feriados.`);
    formulario.reset();
    await carregar();
  } catch (erro) {
    mostrarErro(erro, { aviso, formulario });
  }
});

linhas.addEventListener('click', async (evento) => {
  const botao = evento.target.closest('button[data-acao="remover"]');
  if (!botao) return;

  const d = dias.find(x => x.id === Number(botao.dataset.id));
  if (!confirm(`Remover ${formatarData(d.data)} dos dias especiais? A data deixa de contar como feriado na escala.`)) {
    return;
  }

  try {
    await api(`/api/DiasEspeciais/${d.id}`, { metodo: 'DELETE' });
    avisar(aviso, `${formatarData(d.data)} removido dos dias especiais.`);
    await carregar();
  } catch (erro) {
    mostrarErro(erro, { aviso });
  }
});

carregar().catch(erro => mostrarErro(erro, { aviso }));