namespace PlataformaFutevolei.Aplicacao.Configuracoes;

public class MassaTesteAiConfiguracao
{
    public const string Secao = "MassaTesteAi";

    public bool Habilitada { get; set; }
    public string EmailUsuarioPrincipal { get; set; } = "gustavodrager+qnf-ai-tester@gmail.com";
    public string SenhaUsuarioPrincipal { get; set; } = string.Empty;
}
