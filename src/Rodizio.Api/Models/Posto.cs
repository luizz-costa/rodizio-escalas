namespace Rodizio.Api.Models;

public class Posto
{
    public int Id { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}