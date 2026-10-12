namespace Rodizio.Api.Models;

public class Funcionario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Matricula { get; set; } = string.Empty;
    public string Cargo { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;

    // Escala atual (decisão B2-1): opcional no banco; a API exige nos cadastros e edições.
    public int? EscalaId { get; set; }

    // Antiguidade (decisão B2-2): opcional; sem valor fica por último no desempate.
    public int? Antiguidade { get; set; }

    // Dias em que pode cumprir serviço (decisão B2-3).
    // Padrão: todos, porque por regra todos estão aptos (RN-19).
    public bool PodeSegunda { get; set; } = true;
    public bool PodeTerca { get; set; } = true;
    public bool PodeQuarta { get; set; } = true;
    public bool PodeQuinta { get; set; } = true;
    public bool PodeSexta { get; set; } = true;
    public bool PodeSabado { get; set; } = true;
    public bool PodeDomingo { get; set; } = true;
    public bool PodeFeriado { get; set; } = true;

    
    // RN-28: em qual dia esta pessoa pode cumprir serviço.
    // Em dia especial cadastrado vale PodeFeriado; nos demais, o dia da semana.
    // Num feriado que cai no fim de semana, vale PodeFeriado (o dia especial tem precedência).
    public bool PodeServirEm(DateOnly data, bool ehDiaEspecial)
    {
        if (ehDiaEspecial)
        {
            return PodeFeriado;
        }

        return data.DayOfWeek switch
        {
            DayOfWeek.Monday => PodeSegunda,
            DayOfWeek.Tuesday => PodeTerca,
            DayOfWeek.Wednesday => PodeQuarta,
            DayOfWeek.Thursday => PodeQuinta,
            DayOfWeek.Friday => PodeSexta,
            DayOfWeek.Saturday => PodeSabado,
            _ => PodeDomingo
        };
    }
}