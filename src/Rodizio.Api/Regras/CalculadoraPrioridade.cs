using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Dtos;

namespace Rodizio.Api.Regras;

// D-18: a regra central do sistema ("há mais tempo sem serviço"), isolada numa classe própria.
public class CalculadoraPrioridade
{
    private readonly AppDbContext _context;

    public CalculadoraPrioridade(AppDbContext context)
    {
        _context = context;
    }

    // Devolve null se a escala não existir.
    public async Task<ListaPrioridadeResponse?> CalcularAsync(int escalaId, DateOnly data)
    {
        var escala = await _context.Escalas.FindAsync(escalaId);
        if (escala == null)
        {
            return null;
        }

        // Passo 1: a fila do dia.
        var diasEspeciais = (await _context.DiasEspeciais
            .Select(d => d.Data)
            .ToListAsync()).ToHashSet();
        var fila = Calendario.FilaDoDia(data, diasEspeciais);
        var ehDiaEspecial = diasEspeciais.Contains(data);

        // Passo 2: os candidatos (ativos desta escala).
        var funcionarios = await _context.Funcionarios
            .Where(f => f.Ativo && f.EscalaId == escalaId)
            .ToListAsync();
        var ids = funcionarios.Select(f => f.Id).ToList();

        // Passo 3: serviços e impedimentos de todos os candidatos, numa consulta cada.
        var anterior = data.AddDays(-1);
        var seguinte = data.AddDays(1);

        var servicos = await _context.Servicos
            .Where(s => !s.Cancelado && ids.Contains(s.FuncionarioId) && s.Data <= seguinte)
            .Select(s => new { s.FuncionarioId, s.Data })
            .ToListAsync();

        var impedimentos = await _context.Impedimentos
            .Where(i => !i.Cancelado && ids.Contains(i.FuncionarioId)
                && i.DataInicio <= data && (i.DataFim == null || i.DataFim >= data))
            .ToListAsync();

        // Passo 4: avaliar cada pessoa.
        var avaliados = funcionarios.Select(f =>
        {
            var datasDeServico = servicos
                .Where(s => s.FuncionarioId == f.Id)
                .Select(s => s.Data)
                .ToList();

            // Último serviço NA FILA DO DIA, antes da data (RN-10: cada fila conta separado).
            DateOnly? ultimo = datasDeServico
                .Where(d => d < data && Calendario.FilaDoDia(d, diasEspeciais) == fila)
                .Select(d => (DateOnly?)d)
                .Max();

            int? dias = ultimo.HasValue
                ? Calendario.ContarDiasDaFila(ultimo.Value, data, fila, diasEspeciais)
                : null;

            // Quem não pode, e por quê (mesmos bloqueios do registro de serviço).
            string? motivo = null;
            var impedimento = impedimentos.FirstOrDefault(i => i.FuncionarioId == f.Id);

            // Escalado e, depois, impedido (B5-5): continua escalado, mas precisa de troca.
            // Verificado ANTES do "já escalado", senão o impedimento ficaria escondido.
            var precisaTroca = datasDeServico.Contains(data) && impedimento != null;

            if (precisaTroca)
            {
                motivo = $"Escalado, mas impedido ({impedimento!.Motivo}): faça a troca";
            }
            else if (datasDeServico.Contains(data))
            {
                motivo = "Já está escalado nesta data";
            }
            else if (impedimento != null)
            {
                motivo = $"Impedido ({impedimento.Motivo})";
            }
            else if (datasDeServico.Contains(anterior))
            {
                motivo = "Cumpriu serviço no dia anterior";
            }
            else if (datasDeServico.Contains(seguinte))
            {
                motivo = "Tem serviço no dia seguinte";
            }
            else if (!f.PodeServirEm(data, ehDiaEspecial))
            {
                motivo = "Dia não permitido";
            }

            return new { Funcionario = f, Ultimo = ultimo, Dias = dias, Motivo = motivo, PrecisaTroca = precisaTroca };
        }).ToList();

        // Passo 5: ordenar. Há mais tempo sem serviço = último serviço na fila mais antigo (RN-09).
        var ordenados = avaliados
            .OrderBy(a => a.Motivo != null)                             // quem pode vem primeiro
            .ThenBy(a => a.Ultimo.HasValue)                             // nunca serviu vem primeiro
            .ThenBy(a => a.Ultimo)                                      // último serviço mais antigo primeiro
            .ThenBy(a => a.Funcionario.Antiguidade.HasValue ? 0 : 1)    // sem antiguidade por último (D-21)
            .ThenBy(a => a.Funcionario.Antiguidade)                     // número menor (mais antigo) primeiro
            .ThenBy(a => a.Funcionario.Id)                              // desempate final, para a ordem ser sempre a mesma
            .ToList();

        // Passo 6: numerar só quem pode.
        var posicao = 0;
        var itens = ordenados.Select(a => new ItemPrioridade(
            Posicao: a.Motivo == null ? ++posicao : null,
            FuncionarioId: a.Funcionario.Id,
            Nome: a.Funcionario.Nome,
            Cargo: a.Funcionario.Cargo,
            Antiguidade: a.Funcionario.Antiguidade,
            UltimoServicoNaFila: a.Ultimo,
            DiasSemServicoNaFila: a.Dias,
            Elegivel: a.Motivo == null,
            Motivo: a.Motivo,
            PrecisaTroca: a.PrecisaTroca)).ToList();

        return new ListaPrioridadeResponse(escala.Id, escala.Nome, data, fila, itens);
    }
}