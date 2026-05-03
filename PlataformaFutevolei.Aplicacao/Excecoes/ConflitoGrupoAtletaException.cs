namespace PlataformaFutevolei.Aplicacao.Excecoes;

public class ConflitoGrupoAtletaException(
    string mensagem,
    string codigo,
    Guid grupoAtletaId,
    Guid atletaId
) : RegraNegocioException(mensagem)
{
    public string Codigo { get; } = codigo;
    public Guid GrupoAtletaId { get; } = grupoAtletaId;
    public Guid AtletaId { get; } = atletaId;
}
