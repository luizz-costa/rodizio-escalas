using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Models;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImpedimentosController : ControllerBase
{
    private readonly AppDbContext _context;

    public ImpedimentosController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Impedimentos                      -> todos, inclusive cancelados
    // GET /api/Impedimentos?ativosEm=2026-09-25  -> só os que valem nesse dia (não cancelados)
    // GET /api/Impedimentos?funcionarioId=4      -> só os dessa pessoa
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Impedimento>>> GetImpedimentos(
        [FromQuery] DateOnly? ativosEm, [FromQuery] int? funcionarioId)
    {
        var consulta = _context.Impedimentos.AsQueryable();

        if (ativosEm.HasValue)
        {
            var dia = ativosEm.Value;
            consulta = consulta.Where(i => !i.Cancelado
                && i.DataInicio <= dia
                && (i.DataFim == null || i.DataFim >= dia));
        }

        if (funcionarioId.HasValue)
        {
            consulta = consulta.Where(i => i.FuncionarioId == funcionarioId.Value);
        }

        return await consulta
            .OrderBy(i => i.DataInicio)
            .ThenBy(i => i.Id)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Impedimento>> GetImpedimento(int id)
    {
        var impedimento = await _context.Impedimentos.FindAsync(id);

        if (impedimento == null)
        {
            return NotFound();
        }

        return impedimento;
    }

    [HttpPost]
    public async Task<ActionResult<Impedimento>> PostImpedimento(Impedimento impedimento)
    {
        var erro = await ValidarAsync(impedimento.FuncionarioId, impedimento.Motivo,
            impedimento.DataInicio, impedimento.DataFim, ignorarId: null);
        if (erro != null)
        {
            return erro;
        }

        // Campos controlados pela API: o que o cliente enviar neles é ignorado.
        impedimento.Id = 0;
        impedimento.Cancelado = false;
        impedimento.CriadoEm = DateTime.UtcNow;
        impedimento.AtualizadoEm = impedimento.CriadoEm;

        _context.Impedimentos.Add(impedimento);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetImpedimento), new { id = impedimento.Id }, impedimento);
    }

    // Corrige motivo e datas. A pessoa não muda: se foi a pessoa errada, cancele (B4-4).
    [HttpPut("{id}")]
    public async Task<IActionResult> PutImpedimento(int id, Impedimento entrada)
    {
        var impedimento = await _context.Impedimentos.FindAsync(id);

        if (impedimento == null)
        {
            return NotFound();
        }

        if (impedimento.Cancelado)
        {
            return ProblemaCancelado();
        }

        var erro = await ValidarAsync(impedimento.FuncionarioId, entrada.Motivo,
            entrada.DataInicio, entrada.DataFim, ignorarId: id);
        if (erro != null)
        {
            return erro;
        }

        impedimento.Motivo = entrada.Motivo;
        impedimento.DataInicio = entrada.DataInicio;
        impedimento.DataFim = entrada.DataFim;
        impedimento.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // PATCH /api/Impedimentos/1/encerrar?data=2026-09-30  (sem data = hoje)
    [HttpPatch("{id}/encerrar")]
    public async Task<IActionResult> EncerrarImpedimento(int id, [FromQuery] DateOnly? data)
    {
        var impedimento = await _context.Impedimentos.FindAsync(id);

        if (impedimento == null)
        {
            return NotFound();
        }

        if (impedimento.Cancelado)
        {
            return ProblemaCancelado();
        }

        var fim = data ?? DateOnly.FromDateTime(DateTime.Now);

        var erro = await ValidarAsync(impedimento.FuncionarioId, impedimento.Motivo,
            impedimento.DataInicio, fim, ignorarId: id);
        if (erro != null)
        {
            return erro;
        }

        impedimento.DataFim = fim;
        impedimento.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Cancelamento lógico (B4-4): o registro continua no banco, visível para o ETL.
    [HttpPatch("{id}/cancelar")]
    public async Task<IActionResult> CancelarImpedimento(int id)
    {
        var impedimento = await _context.Impedimentos.FindAsync(id);

        if (impedimento == null)
        {
            return NotFound();
        }

        impedimento.Cancelado = true;
        impedimento.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Devolve null se estiver tudo certo; senão, a resposta de erro pronta.
    private async Task<ActionResult?> ValidarAsync(int funcionarioId, MotivoImpedimento motivo,
        DateOnly inicio, DateOnly? fim, int? ignorarId)
    {
        // 1) Erros de campo (D-25): acumulados e devolvidos juntos.
        if (!await _context.Funcionarios.AnyAsync(f => f.Id == funcionarioId))
        {
            ModelState.AddModelError(nameof(Impedimento.FuncionarioId), "Funcionário inexistente.");
        }

        if (!Enum.IsDefined(motivo))
        {
            ModelState.AddModelError(nameof(Impedimento.Motivo), "Motivo inválido. Use AtestadoMedico ou ViagemAServico.");
        }

        if (inicio == default)
        {
            ModelState.AddModelError(nameof(Impedimento.DataInicio), "Informe a data de início.");
        }

        if (fim.HasValue && fim.Value < inicio)
        {
            ModelState.AddModelError(nameof(Impedimento.DataFim), "A data de fim não pode ser anterior à data de início.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        // 2) Regra de negócio (D-25; B4-5): sem sobreposição
        //    com outro impedimento ativo da mesma pessoa.
        //    Sobrepõe quando: o outro começa até o nosso fim E termina a partir do nosso início.
        var fimConsiderado = fim ?? DateOnly.MaxValue;
        var idIgnorado = ignorarId ?? 0;

        var conflito = await _context.Impedimentos
            .Where(o => o.FuncionarioId == funcionarioId
                && !o.Cancelado
                && o.Id != idIgnorado
                && o.DataInicio <= fimConsiderado
                && (o.DataFim == null || o.DataFim >= inicio))
            .FirstOrDefaultAsync();

        if (conflito != null)
        {
            var periodo = conflito.DataFim.HasValue
                ? $"de {conflito.DataInicio:dd/MM/yyyy} até {conflito.DataFim.Value:dd/MM/yyyy}"
                : $"desde {conflito.DataInicio:dd/MM/yyyy}, sem data de fim";

            return Problem(
                title: "Impedimento sobreposto",
                detail: $"Esta pessoa já tem um impedimento ativo ({conflito.Motivo}) {periodo}. " +
                        "Ajuste, encerre ou cancele esse impedimento antes.",
                statusCode: 400);
        }

        return null;
    }

    private ObjectResult ProblemaCancelado()
    {
        return Problem(
            title: "Impedimento cancelado",
            detail: "Um impedimento cancelado não pode ser alterado nem encerrado.",
            statusCode: 400);
    }
}