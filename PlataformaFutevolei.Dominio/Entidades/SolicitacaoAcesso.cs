using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class SolicitacaoAcesso : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public StatusSolicitacaoAcesso Status { get; set; } = StatusSolicitacaoAcesso.Pendente;
}
