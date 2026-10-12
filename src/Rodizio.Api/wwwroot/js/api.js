// Cliente único da API (D-19) e utilidades comuns a todas as telas.

// Erro da API, já traduzido do formato ProblemDetails (D-25).
class ErroApi extends Error {
  constructor(status, titulo, detalhe, campos) {
    super(detalhe || titulo || `Erro ${status}`);
    this.status = status;
    this.titulo = titulo;
    this.detalhe = detalhe;        // regra de negócio -> aviso geral
    this.campos = campos || {};    // erros de campo -> { "EscalaId": ["mensagem"] }
  }
}

// Toda chamada à API passa por aqui.
// Uso: await api('/api/Escalas')  ou  await api('/api/Servicos', { metodo: 'POST', corpo: {...} })
async function api(caminho, { metodo = 'GET', corpo } = {}) {
  let resposta;
  try {
    resposta = await fetch(caminho, {
      method: metodo,
      headers: corpo !== undefined ? { 'Content-Type': 'application/json' } : {},
      body: corpo !== undefined ? JSON.stringify(corpo) : undefined
    });
  } catch {
    throw new ErroApi(0, 'Sem conexão', 'Não foi possível falar com o servidor. Verifique se a API está rodando.');
  }

  if (resposta.status === 204) {
    return null;   // sucesso sem corpo (PUT, PATCH, DELETE)
  }

  const texto = await resposta.text();
  let dados = null;
  try { dados = texto ? JSON.parse(texto) : null; } catch { dados = null; }

  if (!resposta.ok) {
    const p = dados || {};
    throw new ErroApi(resposta.status, p.title, p.detail, p.errors);
  }

  return dados;
}

// Mostra um erro na tela (D-25): o "detail" no aviso geral; cada erro de campo
// embaixo do campo correspondente (elemento com data-erro="nomedocampo").
function mostrarErro(erro, { aviso, formulario } = {}) {
  limparErros({ aviso, formulario });

  const semLugar = [];
  for (const [nome, mensagens] of Object.entries(erro.campos || {})) {
    const alvo = formulario?.querySelector(`[data-erro="${nome.toLowerCase()}"]`);
    if (alvo) {
      alvo.textContent = mensagens.join(' ');
    } else {
      semLugar.push(...mensagens);
    }
  }

  const textoGeral = [erro.detalhe, ...semLugar].filter(Boolean).join(' ')
    || (Object.keys(erro.campos || {}).length ? '' : erro.message);

  if (aviso && textoGeral) {
    aviso.className = 'aviso';
    aviso.textContent = textoGeral;
    aviso.hidden = false;
  }
}

function limparErros({ aviso, formulario } = {}) {
  formulario?.querySelectorAll('[data-erro]').forEach(el => { el.textContent = ''; });
  if (aviso) { aviso.hidden = true; aviso.textContent = ''; }
}

// Aviso de sucesso ou de alerta.
function avisar(elemento, texto, tipo = 'sucesso') {
  elemento.className = `aviso ${tipo}`;
  elemento.textContent = texto;
  elemento.hidden = false;
}

// Protege o HTML montado com dados: um nome como "<b>" aparece como texto, não vira HTML.
function esc(valor) {
  return String(valor ?? '').replace(/[&<>"']/g, c =>
    ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
}

// ---------- Datas ----------
// A API usa "AAAA-MM-DD" (DateOnly). As datas são montadas no fuso LOCAL.
// Não use toISOString(): ela converte para UTC, e depois das 21h no horário de Brasília
// já devolve o dia seguinte.
function dataIso(d = new Date()) {
  const mes = String(d.getMonth() + 1).padStart(2, '0');
  const dia = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mes}-${dia}`;
}

function somarDias(iso, quantidade) {
  const [a, m, d] = iso.split('-').map(Number);
  const data = new Date(a, m - 1, d);
  data.setDate(data.getDate() + quantidade);
  return dataIso(data);
}

function formatarData(iso) {
  if (!iso) return '—';
  const [a, m, d] = iso.split('-');
  return `${d}/${m}/${a}`;
}

const DIAS_DA_SEMANA = ['domingo', 'segunda-feira', 'terça-feira', 'quarta-feira',
                        'quinta-feira', 'sexta-feira', 'sábado'];

function diaDaSemana(iso) {
  const [a, m, d] = iso.split('-').map(Number);
  return DIAS_DA_SEMANA[new Date(a, m - 1, d).getDay()];
}

// Rótulos legíveis para os valores que a API envia como texto.
const MOTIVOS = { AtestadoMedico: 'Atestado médico', ViagemAServico: 'Viagem a serviço' };

// Selo da fila do dia: a API envia o nome técnico ("DiasUteis"); a tela mostra o rótulo.
const FILAS = {
  DiasUteis: { classe: 'dias-uteis', rotulo: 'Fila de dias úteis' },
  FinsDeSemanaEFeriados: { classe: 'fins-de-semana', rotulo: 'Fila de fins de semana e feriados' }
};

function seloFila(fila) {
  const f = FILAS[fila];
  return `<span class="fila ${f.classe}">${f.rotulo}</span>`;
}


function comMaiuscula(texto) {
  return texto.charAt(0).toUpperCase() + texto.slice(1);
}

// Troca o nome técnico do motivo pelo rótulo: "Impedido (AtestadoMedico)" -> "Impedido (atestado médico)"
function textoMotivo(texto) {
  return Object.entries(MOTIVOS).reduce(
    (resultado, [chave, rotulo]) => resultado.replace(chave, rotulo.toLowerCase()), texto);
}