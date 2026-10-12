namespace Rodizio.Api.Regras;

public enum Fila
{
    DiasUteis,              // segunda a sexta
    FinsDeSemanaEFeriados   // sábados, domingos e dias especiais
}

// Regras de calendário (RN-07, RN-08, RN-17): a fila é definida pela data, não pela pessoa.
// Separada da calculadora porque a situação do dia (B7) também vai precisar dela.
public static class Calendario
{
    public static Fila FilaDoDia(DateOnly data, ISet<DateOnly> diasEspeciais)
    {
        var fimDeSemana = data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday;
        return fimDeSemana || diasEspeciais.Contains(data) ? Fila.FinsDeSemanaEFeriados : Fila.DiasUteis;
    }

    // Quantos dias desta fila existem depois do último serviço até a data (inclusive).
    // O primeiro dia da mesma fila após o serviço vale 1.
    public static int ContarDiasDaFila(DateOnly ultimoServico, DateOnly ate, Fila fila, ISet<DateOnly> diasEspeciais)
    {
        var total = 0;

        for (var dia = ultimoServico.AddDays(1); dia <= ate; dia = dia.AddDays(1))
        {
            if (FilaDoDia(dia, diasEspeciais) == fila)
            {
                total++;
            }
        }

        return total;
    }
}