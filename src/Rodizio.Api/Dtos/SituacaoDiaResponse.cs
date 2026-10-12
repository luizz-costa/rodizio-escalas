using Rodizio.Api.Models;
using Rodizio.Api.Regras;

namespace Rodizio.Api.Dtos;

// Resposta da situação do dia (tela T-01). Só leitura: cruza serviços, impedimentos e funcionários.
public record SituacaoDiaResponse(
    DateOnly Data,
    Fila Fila,
    TotaisDia Totais,
    IReadOnlyList<SituacaoEscala> Escalas,
    IReadOnlyList<AlertaDia> Alertas);

public record TotaisDia(
    int Ativos,        // ativos que pertencem a alguma escala
    int EmServico,
    int Impedidos,
    int Disponiveis);  // ativos menos impedidos

public record SituacaoEscala(
    int EscalaId,
    string Escala,
    int Ativos,
    IReadOnlyList<EscaladoDia> Escalados,
    IReadOnlyList<ImpedidoDia> Impedidos);

public record EscaladoDia(
    int ServicoId,
    int FuncionarioId,
    string Nome,
    int? EscaladoOriginalId,        // preenchido se entrou por troca
    string? EscaladoOriginalNome,
    bool Impedido);                 // verdadeiro = precisa de troca (B5-5)

public record ImpedidoDia(
    int ImpedimentoId,
    int FuncionarioId,
    string Nome,
    MotivoImpedimento Motivo,
    DateOnly DataInicio,
    DateOnly? DataFim);

public record AlertaDia(
    string Tipo,
    int EscalaId,
    int ServicoId,
    string Mensagem);