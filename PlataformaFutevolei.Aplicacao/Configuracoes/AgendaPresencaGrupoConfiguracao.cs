namespace PlataformaFutevolei.Aplicacao.Configuracoes;

public class AgendaPresencaGrupoConfiguracao
{
    public const string Secao = "AgendaPresencaGrupos";

    public bool Habilitada { get; set; }
    public string FusoHorario { get; set; } = "America/Sao_Paulo";
    public string HoraEnvioLocal { get; set; } = "08:00";
    public int IntervaloProcessamentoMinutos { get; set; } = 15;
    public string UrlApp { get; set; } = string.Empty;
}
