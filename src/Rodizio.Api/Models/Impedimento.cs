namespace Rodizio.Api.Models;

// Evento: a pessoa não pode cumprir serviço durante um período (RN-19 a RN-23).
// A contagem de folgas continua correndo durante o impedimento (RN-20, RN-21).
public class Impedimento
{
    public int Id { get; set; }
    public int FuncionarioId { get; set; }
    public MotivoImpedimento Motivo { get; set; }

    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }     // vazia = em aberto (B4-3)

    public bool Cancelado { get; set; }        // cancelamento lógico (B4-4)

    // Instantes em UTC, preenchidos pela API. Servem à carga incremental do ETL (B4-2).
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}