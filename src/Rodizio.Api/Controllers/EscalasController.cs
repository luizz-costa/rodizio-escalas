using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Dtos;
using Rodizio.Api.Models;
using Rodizio.Api.Regras;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EscalasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CalculadoraPrioridade _calculadora;

    // O ASP.NET entrega as duas dependências (injeção de dependência).
    public EscalasController(AppDbContext context, CalculadoraPrioridade calculadora)
    {
        _context = context;
        _calculadora = calculadora;
    }

    // As escalas são fixas (criadas pelo seed). Por isso este controller só consulta.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Escala>>> GetEscalas()
    {
        return await _context.Escalas
            .OrderBy(e => e.Id)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Escala>> GetEscala(int id)
    {
        var escala = await _context.Escalas.FindAsync(id);

        if (escala == null)
        {
            return NotFound();
        }

        return escala;
    }

    // GET /api/Escalas/5/prioridade?data=2026-09-30
    // A lista de prioridade (quem está há mais tempo sem serviço primeiro, RN-09) desta escala, para esta data.
    [HttpGet("{id}/prioridade")]
    public async Task<ActionResult<ListaPrioridadeResponse>> GetPrioridade(int id, [FromQuery] DateOnly? data)
    {
        if (!data.HasValue)
        {
            ModelState.AddModelError("data", "Informe a data da escala.");
            return ValidationProblem(ModelState);
        }

        var lista = await _calculadora.CalcularAsync(id, data.Value);

        if (lista == null)
        {
            return NotFound();
        }

        return lista;
    }
}