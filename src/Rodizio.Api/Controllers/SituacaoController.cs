using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Dtos;
using Rodizio.Api.Regras;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SituacaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public SituacaoController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Situacao?data=2026-09-28
    // Só leitura: junta, numa chamada, o que a tela "Escala do dia" precisa.
    [HttpGet]
    public async Task<ActionResult<SituacaoDiaResponse>> GetSituacao([FromQuery] DateOnly? data)
    {
        if (!data.HasValue)
        {
            ModelState.AddModelError("data", "Informe a data.");
            return ValidationProblem(ModelState);
        }

        var dia = data.Value;

        // A fila do dia, pela mesma regra da lista de prioridade.
        var diasEspeciais = (await _context.DiasEspeciais
            .Select(d => d.Data)
            .ToListAsync()).ToHashSet();
        var fila = Calendario.FilaDoDia(dia, diasEspeciais);

        var escalas = await _context.Escalas.OrderBy(e => e.Id).ToListAsync();

        // Todos os funcionários: o escalado original de uma troca pode estar desativado hoje.
        var funcionarios = await _context.Funcionarios.ToListAsync();
        var nomes = funcionarios.ToDictionary(f => f.Id, f => f.Nome);
        var ativos = funcionarios.Where(f => f.Ativo && f.EscalaId != null).ToList();

        var servicos = await _context.Servicos
            .Where(s => !s.Cancelado && s.Data == dia)
            .OrderBy(s => s.Id)
            .ToListAsync();

        var impedimentos = await _context.Impedimentos
            .Where(i => !i.Cancelado && i.DataInicio <= dia && (i.DataFim == null || i.DataFim >= dia))
            .ToListAsync();

        // Um impedimento por pessoa (a D-30 impede sobreposição; o GroupBy protege mesmo assim).
        var impedidoPorFuncionario = impedimentos
            .GroupBy(i => i.FuncionarioId)
            .ToDictionary(g => g.Key, g => g.First());

        // Um quadro por escala.
        var porEscala = escalas.Select(e =>
        {
            var escalados = servicos
                .Where(s => s.EscalaId == e.Id)
                .Select(s => new EscaladoDia(
                    s.Id,
                    s.FuncionarioId,
                    nomes[s.FuncionarioId],
                    s.EscaladoOriginalId,
                    s.EscaladoOriginalId.HasValue ? nomes[s.EscaladoOriginalId.Value] : null,
                    impedidoPorFuncionario.ContainsKey(s.FuncionarioId)))
                .ToList();

            var impedidos = ativos
                .Where(f => f.EscalaId == e.Id && impedidoPorFuncionario.ContainsKey(f.Id))
                .Select(f =>
                {
                    var i = impedidoPorFuncionario[f.Id];
                    return new ImpedidoDia(i.Id, f.Id, f.Nome, i.Motivo, i.DataInicio, i.DataFim);
                })
                .OrderBy(x => x.Nome)
                .ToList();

            return new SituacaoEscala(e.Id, e.Nome, ativos.Count(f => f.EscalaId == e.Id), escalados, impedidos);
        }).ToList();

        // Alertas (B5-5): escalado que ficou impedido depois de lançado.
        var alertas = servicos
            .Where(s => impedidoPorFuncionario.ContainsKey(s.FuncionarioId))
            .Select(s =>
            {
                var i = impedidoPorFuncionario[s.FuncionarioId];
                return new AlertaDia(
                    "EscaladoImpedido",
                    s.EscalaId,
                    s.Id,
                    $"{nomes[s.FuncionarioId]} está escalado em {dia:dd/MM/yyyy} e impedido ({i.Motivo}). Faça a troca.");
            })
            .ToList();

        var qtdImpedidos = ativos.Count(f => impedidoPorFuncionario.ContainsKey(f.Id));

        var totais = new TotaisDia(
            Ativos: ativos.Count,
            EmServico: servicos.Count,
            Impedidos: qtdImpedidos,
            Disponiveis: ativos.Count - qtdImpedidos);   // ativos menos impedidos

        return new SituacaoDiaResponse(dia, fila, totais, porEscala, alertas);
    }
}