using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class CodigoAcessoEmail : EntidadeBase
{
    public string EmailNormalizado { get; set; } = string.Empty;
    public string CodigoHash { get; set; } = string.Empty;
    public FinalidadeCodigoAcessoEmail Finalidade { get; set; } = FinalidadeCodigoAcessoEmail.Login;
    public DateTime ExpiraEmUtc { get; set; }
    public int Tentativas { get; set; }
    public DateTime? ConsumidoEmUtc { get; set; }
    public DateTime UltimoEnvioEmUtc { get; set; } = DateTime.UtcNow;
    public string? CadastroTokenHash { get; set; }
    public DateTime? CadastroTokenExpiraEmUtc { get; set; }
}
