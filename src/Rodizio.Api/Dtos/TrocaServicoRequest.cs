namespace Rodizio.Api.Dtos;

// Mensagem de entrada da troca (B5-4). Não é uma tabela: é só o que a tela envia.
public record TrocaServicoRequest(int NovoFuncionarioId);