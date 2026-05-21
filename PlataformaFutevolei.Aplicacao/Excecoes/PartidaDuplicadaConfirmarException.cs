namespace PlataformaFutevolei.Aplicacao.Excecoes;

public class PartidaDuplicadaConfirmarException(string mensagem) : RegraNegocioException(mensagem)
{
    public const string CodigoErro = "PARTIDA_DUPLICADA_CONFIRMAR";

    public string Codigo { get; } = CodigoErro;
}
