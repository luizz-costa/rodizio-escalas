// Cabeçalho e navegação, iguais em todas as telas: um lugar só para mudar.
const TELAS = [
  { arquivo: 'index.html',          titulo: 'Escala do dia' },
  { arquivo: 'montar-escala.html',  titulo: 'Montar escala' },
  { arquivo: 'impedimentos.html',   titulo: 'Impedimentos' },
  { arquivo: 'funcionarios.html',   titulo: 'Funcionários' },
  { arquivo: 'dias-especiais.html', titulo: 'Dias especiais' }
];

function montarLayout(telaAtual) {
  const links = TELAS.map(t =>
    `<a href="${t.arquivo}"${t.arquivo === telaAtual ? ' aria-current="page"' : ''}>${t.titulo}</a>`
  ).join('');

  document.body.insertAdjacentHTML('afterbegin', `
    <header class="topo">
      <span class="marca">Gestão de Escalas</span>
      <span class="ambiente">Organização Exemplo · dados fictícios</span>
    </header>
    <nav class="navegacao" aria-label="Telas do sistema">${links}</nav>`);
}