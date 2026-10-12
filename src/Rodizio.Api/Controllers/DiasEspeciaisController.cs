using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Models;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiasEspeciaisController : ControllerBase
{
    private readonly AppDbContext _context;

    public DiasEspeciaisController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiaEspecial>>> GetDiasEspeciais()
    {
        return await _context.DiasEspeciais
            .OrderBy(d => d.Data)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DiaEspecial>> GetDiaEspecial(int id)
    {
        var dia = await _context.DiasEspeciais.FindAsync(id);

        if (dia == null)
        {
            return NotFound();
        }

        return dia;
    }

    [HttpPost]
    public async Task<ActionResult<DiaEspecial>> PostDiaEspecial(DiaEspecial diaEspecial)
    {
        // Erro de campo: sem data, o DateOnly chegaria como 01/01/0001 e seria gravado.
        if (diaEspecial.Data == default)
        {
            ModelState.AddModelError(nameof(DiaEspecial.Data), "Informe a data.");
            return ValidationProblem(ModelState);
        }
        // Erro de campo: a mesma data não pode ser cadastrada duas vezes.
        if (await _context.DiasEspeciais.AnyAsync(d => d.Data == diaEspecial.Data))
        {
            ModelState.AddModelError(nameof(DiaEspecial.Data), "Esta data já está cadastrada como dia especial.");
            return ValidationProblem(ModelState);
        }

        _context.DiasEspeciais.Add(diaEspecial);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiaEspecial), new { id = diaEspecial.Id }, diaEspecial);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiaEspecial(int id)
    {
        var dia = await _context.DiasEspeciais.FindAsync(id);

        if (dia == null)
        {
            return NotFound();
        }

        // Regra de negócio: só remove datas futuras.
        // Remover um feriado passado mudaria a fila daquele dia e a contagem de folgas de todos.
        var hoje = DateOnly.FromDateTime(DateTime.Now);
        if (dia.Data <= hoje)
        {
            return Problem(
                title: "Remoção não permitida",
                detail: "Só é possível remover dias especiais futuros. Este dia já faz parte do histórico da escala.",
                statusCode: 400);
        }

        _context.DiasEspeciais.Remove(dia);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}