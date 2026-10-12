using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Data;
using Rodizio.Api.Models;

namespace Rodizio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PostosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Posto>>> GetPostos()
    {
        return await _context.Postos.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Posto>> GetPosto(int id)
    {
        var posto = await _context.Postos.FindAsync(id);

        if (posto == null)
        {
            return NotFound();
        }

        return posto;
    }

    [HttpPost]
    public async Task<ActionResult<Posto>> PostPosto(Posto posto)
    {
        _context.Postos.Add(posto);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPosto), new { id = posto.Id }, posto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutPosto(int id, Posto postoInput)
    {
        var posto = await _context.Postos.FindAsync(id);

        if (posto == null)
        {
            return NotFound();
        }

        posto.Descricao = postoInput.Descricao;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/desativar")]
    public async Task<IActionResult> DesativarPosto(int id)
    {
        var posto = await _context.Postos.FindAsync(id);

        if (posto == null)
        {
            return NotFound();
        }

        posto.Ativo = false;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}/reativar")]
    public async Task<IActionResult> ReativarPosto(int id)
    {
        var posto = await _context.Postos.FindAsync(id);

        if (posto == null)
        {
            return NotFound();
        }

        posto.Ativo = true;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}