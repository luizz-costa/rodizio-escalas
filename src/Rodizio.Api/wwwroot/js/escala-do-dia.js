// Tela Escala do dia (T-01), a tela inicial: como está o dia em cada escala.
// Toda a inteligência está no GET /api/Situacao (B7); a tela só desenha.
montarLayout('index.html');

const aviso = document.getElementById('aviso');
const campoData = document.getElementById('data');

function periodo(impedido) {
  return formatarData(impedido.dataInicio) +
    (impedido.dataFim ? ` a ${formatarData(impedido.dataFim)}` : ', sem data de fim');
}

async function carregar() {
  const data = campoData.value;
  const s = await api(`/api/Situacao?data=${data}`);

  // Cabeçalho: dia e fila.
  document.getElementById('titulo-data').innerHTML =
    `${comMaiuscula(diaDaSemana(data))}, ${formatarData(data)} ${seloFila(s.fila)}`;

  // Alertas (B5-5): cada um leva direto ao Montar escala daquela escala e data.
  document.getElementById('alertas').innerHTML = s.alertas.map(a => `
    <div class="aviso alerta alerta-acao">
      <span>${esc(textoMotivo(a.mensagem))}</span>
      <a class="botao pequeno" href="montar-escala.html?escala=${a.escalaId}&data=${data}">Resolver</a>
    </div>`).join('');

  // Totais do dia.
  const t = s.totais;
  document.getElementById('totais').innerHTML = [
    ['Ativos nas escalas', t.ativos, ''],
    ['De serviço', t.emServico, ''],
    ['Impedidos', t.impedidos, ''],
    ['Disponíveis', t.disponiveis, 'ativos menos impedidos']
  ].map(([rotulo, valor, nota]) => `
    <div class="cartao">
      <span class="valor">${valor}</span>
      <span class="rotulo">${rotulo}</span>
      ${nota ? `<small>${nota}</small>` : ''}
    </div>`).join('');

  // Um quadro por escala.
  document.getElementById('quadros').innerHTML = s.escalas.map(e => {
    const escalados = e.escalados.map(x => `
      <li>
        ${esc(x.nome)}
        ${x.escaladoOriginalNome ? `<small>Troca: no lugar de ${esc(x.escaladoOriginalNome)}</small>` : ''}
        ${x.impedido ? '<span class="etiqueta-impedido">Impedido: fazer troca</span>' : ''}
      </li>`).join('') || '<li class="apoio">Ninguém escalado.</li>';

    const impedidos = e.impedidos.map(x => `
      <li>
        ${esc(x.nome)} <span class="etiqueta-impedido">${esc(MOTIVOS[x.motivo] ?? x.motivo)}</span>
        <small>${periodo(x)}</small>
      </li>`).join('') || '<li class="apoio">Nenhum.</li>';

    return `
      <section class="painel quadro">
        <div class="cabecalho-painel">
          <h2>${esc(e.escala)}</h2>
          <span class="apoio">${e.ativos} ativo(s)</span>
        </div>
        <h3>De serviço</h3>
        <ul class="lista-quadro">${escalados}</ul>
        <h3>Impedidos</h3>
        <ul class="lista-quadro">${impedidos}</ul>
        <a class="botao secundario" href="montar-escala.html?escala=${e.escalaId}&data=${data}">Montar escala</a>
      </section>`;
  }).join('');
}

function recarregar() {
  carregar().catch(erro => mostrarErro(erro, { aviso }));
}

campoData.addEventListener('change', recarregar);
document.getElementById('anterior').addEventListener('click', () => {
  campoData.value = somarDias(campoData.value, -1);
  recarregar();
});
document.getElementById('proximo').addEventListener('click', () => {
  campoData.value = somarDias(campoData.value, 1);
  recarregar();
});

// A tela inicial abre em HOJE (ou na data da URL: index.html?data=2026-09-24).
campoData.value = new URLSearchParams(location.search).get('data') || dataIso();
recarregar();