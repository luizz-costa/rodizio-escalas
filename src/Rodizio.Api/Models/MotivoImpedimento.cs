namespace Rodizio.Api.Models;

// Motivos de impedimento (RN-19, RN-23).
// Novos motivos, como férias, entram aqui sem migration: o motivo é gravado como texto.
public enum MotivoImpedimento
{
    AtestadoMedico,
    ViagemAServico
}