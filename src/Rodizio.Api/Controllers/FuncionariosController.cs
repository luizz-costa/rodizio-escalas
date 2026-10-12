using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Models;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FuncionariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public FuncionariosController(AppDbContext context)
    {
        _context = context;
    }

    // GET /api/Funcionarios            -> todos
    // GET /api/Funcionarios?escalaId=5 -> só os da escala 5
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Funcionario>>> GetFuncionarios([FromQuery] int? escalaId)
    {
        var consulta = _context.Funcionarios.AsQueryable();

        if (escalaId.HasValue)
        {
            consulta = consulta.Where(f => f.EscalaId == escalaId.Value);
        }

        return await consulta.OrderBy(f => f.Id).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Funcionario>> GetFuncionario(int id)
    {
        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
        {
            return NotFound();
        }

        return funcionario;
    }

    [HttpPost]
    public async Task<ActionResult<Funcionario>> PostFuncionario(Funcionario funcionario)
    {
        // Decisão B2-1: todo cadastro precisa de uma escala existente.
        if (!await EscalaExisteAsync(funcionario.EscalaId))
        {
            ModelState.AddModelError(nameof(Funcionario.EscalaId), "Escala não informada ou inexistente.");
            return ValidationProblem(ModelState);
        }

        _context.Funcionarios.Add(funcionario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetFuncionario), new { id = funcionario.Id }, funcionario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutFuncionario(int id, Funcionario funcionarioInput)
    {
        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
        {
            return NotFound();
        }

        // Decisão B2-1: toda edição precisa de uma escala existente.
        if (!await EscalaExisteAsync(funcionarioInput.EscalaId))
        {
            ModelState.AddModelError(nameof(Funcionario.EscalaId), "Escala não informada ou inexistente.");
            return ValidationProblem(ModelState);
        }

        // Ativo não é alterado aqui de propósito (desativar/reativar têm endpoints próprios).
        funcionario.Nome = funcionarioInput.Nome;
        funcionario.Matricula = funcionarioInput.Matricula;
        funcionario.Cargo = funcionarioInput.Cargo;
        funcionario.EscalaId = funcionarioInput.EscalaId;
        funcionario.Antiguidade = funcionarioInput.Antiguidade;
        funcionario.PodeSegunda = funcionarioInput.PodeSegunda;
        funcionario.PodeTerca = funcionarioInput.PodeTerca;
        funcionario.PodeQuarta = funcionarioInput.PodeQuarta;
        funcionario.PodeQuinta = funcionarioInput.PodeQuinta;
        funcionario.PodeSexta = funcionarioInput.PodeSexta;
        funcionario.PodeSabado = funcionarioInput.PodeSabado;
        funcionario.PodeDomingo = funcionarioInput.PodeDomingo;
        funcionario.PodeFeriado = funcionarioInput.PodeFeriado;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> DesativarFuncionario(int id)
    {
        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
        {
            return NotFound();
        }

        funcionario.Ativo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/reativar")]
    public async Task<IActionResult> ReativarFuncionario(int id)
    {
        var funcionario = await _context.Funcionarios.FindAsync(id);

        if (funcionario == null)
        {
            return NotFound();
        }

        funcionario.Ativo = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Verdadeiro só se o Id foi informado e a escala existe no banco.
    private async Task<bool> EscalaExisteAsync(int? escalaId)
    {
        return escalaId.HasValue
            && await _context.Escalas.AnyAsync(e => e.Id == escalaId.Value);
    }
}