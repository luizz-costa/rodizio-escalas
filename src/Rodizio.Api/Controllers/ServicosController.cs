using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Dtos;
using Rodizio.Api.Models;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicosController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Servicos?data=2026-09-28&escalaId=5&funcionarioId=3
    // Por padrão só os ativos; incluirCancelados=true mostra todos.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Servico>>> GetServicos(
        [FromQuery] DateOnly? data,
        [FromQuery] int? escalaId,
        [FromQuery] int? funcionarioId,
        [FromQuery] bool incluirCancelados = false)
    {
        var consulta = _context.Servicos.AsQueryable();

        if (!incluirCancelados)
        {
            consulta = consulta.Where(s => !s.Cancelado);
        }

        if (data.HasValue)
        {
            var dia = data.Value;
            consulta = consulta.Where(s => s.Data == dia);
        }

        if (escalaId.HasValue)
        {
            consulta = consulta.Where(s => s.EscalaId == escalaId.Value);
        }

        if (funcionarioId.HasValue)
        {
            consulta = consulta.Where(s => s.FuncionarioId == funcionarioId.Value);
        }

        return await consulta
            .OrderBy(s => s.Data)
            .ThenBy(s => s.EscalaId)
            .ThenBy(s => s.Id)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Servico>> GetServico(int id)
    {
        var servico = await _context.Servicos.FindAsync(id);

        if (servico == null)
        {
            return NotFound();
        }

        return servico;
    }

    [HttpPost]
    public async Task<ActionResult<Servico>> PostServico(Servico servico)
    {
        var erro = await ValidarAsync(servico.FuncionarioId, servico.EscalaId, servico.Data,
            ignorarServicoId: null, campoFuncionario: nameof(Servico.FuncionarioId));
        if (erro != null)
        {
            return erro;
        }

        // Campos controlados pela API (D-27). O escalado original só é preenchido pela troca.
        servico.Id = 0;
        servico.EscaladoOriginalId = null;
        servico.Cancelado = false;
        servico.CriadoEm = DateTime.UtcNow;
        servico.AtualizadoEm = servico.CriadoEm;

        _context.Servicos.Add(servico);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetServico), new { id = servico.Id }, servico);
    }

    // PUT /api/Servicos/7/troca    corpo: { "novoFuncionarioId": 5 }
    [HttpPut("{id}/troca")]
    public async Task<IActionResult> TrocarServico(int id, TrocaServicoRequest troca)
    {
        var servico = await _context.Servicos.FindAsync(id);

        if (servico == null)
        {
            return NotFound();
        }

        if (servico.Cancelado)
        {
            return Regra("Serviço cancelado", "Um serviço cancelado não pode ser trocado.");
        }

        if (troca.NovoFuncionarioId == servico.FuncionarioId)
        {
            ModelState.AddModelError(nameof(TrocaServicoRequest.NovoFuncionarioId),
                "O substituto é o mesmo funcionário que já está escalado.");
            return ValidationProblem(ModelState);
        }

        // B5-4: o substituto passa pelas mesmas regras de um lançamento novo.
        var erro = await ValidarAsync(troca.NovoFuncionarioId, servico.EscalaId, servico.Data,
            ignorarServicoId: id, campoFuncionario: nameof(TrocaServicoRequest.NovoFuncionarioId));
        if (erro != null)
        {
            return erro;
        }

        // Guarda o PRIMEIRO escalado original (B5-4).
        // Se a troca devolver o serviço a ele, deixa de ser uma troca.
        servico.EscaladoOriginalId ??= servico.FuncionarioId;
        if (servico.EscaladoOriginalId == troca.NovoFuncionarioId)
        {
            servico.EscaladoOriginalId = null;
        }

        servico.FuncionarioId = troca.NovoFuncionarioId;
        servico.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Cancelamento lógico (D-17): o registro continua no banco, visível para o ETL.
    [HttpPatch("{id}/cancelar")]
    public async Task<IActionResult> CancelarServico(int id)
    {
        var servico = await _context.Servicos.FindAsync(id);

        if (servico == null)
        {
            return NotFound();
        }

        servico.Cancelado = true;
        servico.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Devolve null se o funcionário pode cumprir este serviço; senão, a resposta de erro pronta.
    private async Task<ActionResult?> ValidarAsync(int funcionarioId, int escalaId, DateOnly data,
        int? ignorarServicoId, string campoFuncionario)
    {
        // 1) Erros de campo (D-25): acumulados e devolvidos juntos.
        if (data == default)
        {
            ModelState.AddModelError(nameof(Servico.Data), "Informe a data do serviço.");
        }

        if (!await _context.Escalas.AnyAsync(e => e.Id == escalaId))
        {
            ModelState.AddModelError(nameof(Servico.EscalaId), "Escala inexistente.");
        }

        var funcionario = await _context.Funcionarios.FindAsync(funcionarioId);
        if (funcionario == null)
        {
            ModelState.AddModelError(campoFuncionario, "Funcionário inexistente.");
        }
        else if (!funcionario.Ativo)
        {
            ModelState.AddModelError(campoFuncionario, "Funcionário desativado não pode ser escalado.");
        }

        if (!ModelState.IsValid || funcionario == null)
        {
            return ValidationProblem(ModelState);
        }

        // 2) Regras de negócio (D-25), na ordem; devolve a primeira que falhar.
        var idIgnorado = ignorarServicoId ?? 0;

        // RN-03: cada escala tem sua própria lista.
        if (funcionario.EscalaId != escalaId)
        {
            return Regra("Funcionário de outra escala",
                "Este funcionário não pertence a esta escala e não pode ser escalado nela.");
        }

        // B5-2: um serviço ativo por pessoa por dia.
        var jaTemNoDia = await _context.Servicos.AnyAsync(s => s.FuncionarioId == funcionarioId
            && !s.Cancelado && s.Id != idIgnorado && s.Data == data);
        if (jaTemNoDia)
        {
            return Regra("Serviço já registrado",
                $"Este funcionário já tem um serviço em {data:dd/MM/yyyy}.");
        }

        // RN-14: proibido dobrar. Olha o dia anterior E o seguinte,
        // porque uma escala de vários dias (ex.: o fim de semana) pode ser lançada fora de ordem.
        var anterior = data.AddDays(-1);
        var seguinte = data.AddDays(1);
        var vizinho = await _context.Servicos
            .Where(s => s.FuncionarioId == funcionarioId && !s.Cancelado && s.Id != idIgnorado
                && (s.Data == anterior || s.Data == seguinte))
            .Select(s => (DateOnly?)s.Data)
            .FirstOrDefaultAsync();
        if (vizinho.HasValue)
        {
            var qual = vizinho.Value == anterior ? "no dia anterior" : "no dia seguinte";
            return Regra("Não pode dobrar",
                $"Este funcionário tem serviço {qual} ({vizinho.Value:dd/MM/yyyy}). Ninguém pode cumprir dois serviços seguidos.");
        }

        // RN-19: impedido na data (impedimento ativo que cobre o dia).
        var impedimento = await _context.Impedimentos
            .Where(i => i.FuncionarioId == funcionarioId && !i.Cancelado
                && i.DataInicio <= data && (i.DataFim == null || i.DataFim >= data))
            .FirstOrDefaultAsync();
        if (impedimento != null)
        {
            return Regra("Funcionário impedido",
                $"Este funcionário está impedido em {data:dd/MM/yyyy} ({impedimento.Motivo}).");
        }

        // RN-28: dia permitido. A regra mora no próprio Funcionario.
        var ehDiaEspecial = await _context.DiasEspeciais.AnyAsync(d => d.Data == data);
        if (!funcionario.PodeServirEm(data, ehDiaEspecial))
        {
            var dia = ehDiaEspecial ? "feriado" : NomeDoDia(data);
            return Regra("Dia não permitido",
                $"Este funcionário não pode cumprir serviço neste dia ({dia}).");
        }

        return null;
    }

    private ObjectResult Regra(string titulo, string detalhe)
    {
        return Problem(title: titulo, detail: detalhe, statusCode: 400);
    }

    private static string NomeDoDia(DateOnly data) => data.DayOfWeek switch
    {
        DayOfWeek.Monday => "segunda-feira",
        DayOfWeek.Tuesday => "terça-feira",
        DayOfWeek.Wednesday => "quarta-feira",
        DayOfWeek.Thursday => "quinta-feira",
        DayOfWeek.Friday => "sexta-feira",
        DayOfWeek.Saturday => "sábado",
        _ => "domingo"
    };
}