namespace Rodizio.Api.Models;

// Feriado definido pela organização: a data entra na fila de fins de semana e feriados (RN-17, RN-18).
public class DiaEspecial
{
    public int Id { get; set; }
    public DateOnly Data { get; set; }
    public string Descricao { get; set; } = string.Empty;
}