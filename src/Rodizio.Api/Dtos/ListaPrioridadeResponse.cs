using Rodizio.Api.Regras;

namespace Rodizio.Api.Dtos;

// Resposta da lista de prioridade: é um cálculo, não uma tabela.
public record ListaPrioridadeResponse(
    int EscalaId,
    string Escala,
    DateOnly Data,
    Fila Fila,
    IReadOnlyList<ItemPrioridade> Itens);

public record ItemPrioridade(
    int? Posicao,                    // só para quem pode cumprir o serviço
    int FuncionarioId,
    string Nome,
    string Cargo,
    int? Antiguidade,
    DateOnly? UltimoServicoNaFila,   // vazio = nunca serviu nesta fila
    int? DiasSemServicoNaFila,       // calculado, não guardado
    bool Elegivel,
    string? Motivo,                  // por que não pode (vazio se pode)
    bool PrecisaTroca);              // escalado nesta data, mas ficou impedido depois (B5-5)    