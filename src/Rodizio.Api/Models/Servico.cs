namespace Rodizio.Api.Models;

// Evento: alguém cumpriu serviço numa data, numa escala (RN-02, RN-03).
// É o único evento que zera a contagem de folgas, e só na fila daquele dia (RN-10).
public class Servico
{
    public int Id { get; set; }
    public DateOnly Data { get; set; }
    public int EscalaId { get; set; }

    public int FuncionarioId { get; set; }          // quem cumpriu o serviço (RN-30)
    public int? EscaladoOriginalId { get; set; }    // só em troca: quem estava escalado antes

    public bool Cancelado { get; set; }             // cancelamento lógico (D-17)

    // Instantes em UTC, preenchidos pela API, para o ETL (D-27).
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}