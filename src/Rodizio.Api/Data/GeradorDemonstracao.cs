using Microsoft.EntityFrameworkCore;
using Rodizio.Api.Models;
using Rodizio.Api.Regras;

namespace Rodizio.Api.Data;

// B8: dados FICTÍCIOS de demonstração. Roda só por linha de comando:
//     dotnet run -- --demo
// Nunca por uma rota da API (decisão B8-2).
public class GeradorDemonstracao
{
    private readonly AppDbContext _context;
    private readonly CalculadoraPrioridade _calculadora;

    public GeradorDemonstracao(AppDbContext context, CalculadoraPrioridade calculadora)
    {
        _context = context;
        _calculadora = calculadora;
    }

    public async Task ExecutarAsync(DateOnly inicioHistorico, DateOnly hoje)
    {
        // Proteção: só gera num banco vazio.
        if (await _context.Funcionarios.AnyAsync())
        {
            throw new InvalidOperationException(
                "O banco já tem dados. Zere o banco (dotnet ef database drop) antes de gerar a demonstração.");
        }

        var agora = DateTime.UtcNow;

        // 1) Dias especiais.
        _context.DiasEspeciais.AddRange(
            new DiaEspecial { Data = new DateOnly(2026, 9, 7), Descricao = "Independência do Brasil" },
            new DiaEspecial { Data = new DateOnly(2026, 10, 12), Descricao = "Nossa Senhora Aparecida" });

        // 2) Pessoas por escala e quantas cumprem serviço por dia (valores fictícios da demonstração).
        var planos = new[]
        {
            (EscalaId: 1, Quantidade: 5,  Cargo: "Supervisor",    PorDia: 1),
            (EscalaId: 2, Quantidade: 7,  Cargo: "Coordenador",   PorDia: 1),
            (EscalaId: 3, Quantidade: 8,  Cargo: "Líder",         PorDia: 2),
            (EscalaId: 4, Quantidade: 9,  Cargo: "Agente sênior", PorDia: 1),
            (EscalaId: 5, Quantidade: 10, Cargo: "Agente",        PorDia: 2),
            (EscalaId: 6, Quantidade: 12, Cargo: "Agente",        PorDia: 3),
        };

        var pessoasPorEscala = new Dictionary<int, List<Funcionario>>();
        var numero = 0;

        foreach (var plano in planos)
        {
            var pessoas = new List<Funcionario>();

            for (var k = 1; k <= plano.Quantidade; k++)
            {
                numero++;
                pessoas.Add(new Funcionario
                {
                    Nome = $"Func. {numero:000}",
                    Matricula = $"FIC-{numero:000}",
                    Cargo = plano.Cargo,
                    EscalaId = plano.EscalaId,
                    Antiguidade = 100 + numero
                });
            }

            pessoasPorEscala[plano.EscalaId] = pessoas;
            _context.Funcionarios.AddRange(pessoas);
        }

        // Restrições de dia (RN-28): alguns casos para a demonstração. [^1] = o último da lista.
        SoFimDeSemanaEFeriado(pessoasPorEscala[2][^1]);
        SoFimDeSemanaEFeriado(pessoasPorEscala[4][^2]);
        SoFimDeSemanaEFeriado(pessoasPorEscala[6][^1]);
        SoSabado(pessoasPorEscala[5][^1]);

        // Um desativado (D-05).
        pessoasPorEscala[4][^1].Ativo = false;

        await _context.SaveChangesAsync();   // grava para as pessoas ganharem Id

        // 3) Impedimentos, espalhados pelo período (um em aberto e um logo depois de hoje).
        _context.Impedimentos.AddRange(
            NovoImpedimento(pessoasPorEscala[5][2], MotivoImpedimento.ViagemAServico,
                new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 12), agora),
            NovoImpedimento(pessoasPorEscala[4][1], MotivoImpedimento.AtestadoMedico,
                new DateOnly(2026, 9, 15), new DateOnly(2026, 9, 17), agora),
            NovoImpedimento(pessoasPorEscala[1][0], MotivoImpedimento.AtestadoMedico,
                new DateOnly(2026, 9, 21), new DateOnly(2026, 9, 22), agora),
            NovoImpedimento(pessoasPorEscala[6][3], MotivoImpedimento.ViagemAServico,
                new DateOnly(2026, 9, 23), null, agora),
            NovoImpedimento(pessoasPorEscala[5][5], MotivoImpedimento.AtestadoMedico,
                hoje.AddDays(1), hoje.AddDays(3), agora));

        await _context.SaveChangesAsync();

        // 4) Histórico: dia a dia, a PRÓPRIA calculadora decide quem cumpre o serviço.
        var totalServicos = 0;

        for (var dia = inicioHistorico; dia < hoje; dia = dia.AddDays(1))
        {
            foreach (var plano in planos)
            {
                var lista = await _calculadora.CalcularAsync(plano.EscalaId, dia);
                var escolhidos = lista!.Itens.Where(i => i.Elegivel).Take(plano.PorDia);

                foreach (var item in escolhidos)
                {
                    _context.Servicos.Add(new Servico
                    {
                        Data = dia,
                        EscalaId = plano.EscalaId,
                        FuncionarioId = item.FuncionarioId,
                        CriadoEm = agora,
                        AtualizadoEm = agora
                    });
                    totalServicos++;
                }

                // Grava já: o cálculo do próximo dia depende destes serviços.
                await _context.SaveChangesAsync();
            }
        }

        Console.WriteLine($"Demonstração gerada: {numero} pessoas, 2 dias especiais, 5 impedimentos, " +
                          $"{totalServicos} serviços de {inicioHistorico:dd/MM/yyyy} até {hoje.AddDays(-1):dd/MM/yyyy}.");
    }

    private static void SoFimDeSemanaEFeriado(Funcionario f)
    {
        f.PodeSegunda = f.PodeTerca = f.PodeQuarta = f.PodeQuinta = f.PodeSexta = false;
    }

    private static void SoSabado(Funcionario f)
    {
        f.PodeSegunda = f.PodeTerca = f.PodeQuarta = f.PodeQuinta = f.PodeSexta = false;
        f.PodeDomingo = f.PodeFeriado = false;
    }

    private static Impedimento NovoImpedimento(Funcionario f, MotivoImpedimento motivo,
        DateOnly inicio, DateOnly? fim, DateTime agora)
    {
        return new Impedimento
        {
            FuncionarioId = f.Id,
            Motivo = motivo,
            DataInicio = inicio,
            DataFim = fim,
            CriadoEm = agora,
            AtualizadoEm = agora
        };
    }
}