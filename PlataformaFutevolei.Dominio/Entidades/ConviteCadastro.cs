using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ConviteCadastro : EntidadeBase
{
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string IdentificadorPublico { get; set; } = string.Empty;
    public string? CodigoConviteHash { get; set; }
    public PerfilUsuario PerfilDestino { get; set; } = PerfilUsuario.Atleta;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? UsadoEmUtc { get; set; }
    public bool Ativo { get; set; } = true;
    public Guid CriadoPorUsuarioId { get; set; }
    public string? CanalEnvio { get; set; }
    public DateTime? UltimaTentativaEnvioEmailEmUtc { get; set; }
    public DateTime? EmailEnviadoEmUtc { get; set; }
    public string? ErroEnvioEmail { get; set; }
    public DateTime? UltimaTentativaEnvioWhatsappEmUtc { get; set; }
    public DateTime? WhatsappEnviadoEmUtc { get; set; }
    public string? ErroEnvioWhatsapp { get; set; }

    public Usuario? CriadoPorUsuario { get; set; }

    public bool FoiUtilizado() => UsadoEmUtc.HasValue;

    public bool EstaExpirado(DateTime dataUtc) => ExpiraEmUtc <= dataUtc;

    public bool PodeSerUsado(DateTime dataUtc) => Ativo && !FoiUtilizado() && !EstaExpirado(dataUtc);

    public string ObterSituacao(DateTime dataUtc)
    {
        if (FoiUtilizado())
        {
            return "Usado";
        }

        if (!Ativo)
        {
            return "Cancelado";
        }

        return EstaExpirado(dataUtc) ? "Expirado" : "Ativo";
    }

    public string ObterSituacaoEnvioEmail()
    {
        if (EmailEnviadoEmUtc.HasValue)
        {
            return "Enviado";
        }

        return UltimaTentativaEnvioEmailEmUtc.HasValue && !string.IsNullOrWhiteSpace(ErroEnvioEmail)
            ? "Falhou"
            : "Pendente";
    }

    public string ObterSituacaoEnvioWhatsapp()
    {
        if (WhatsappEnviadoEmUtc.HasValue)
        {
            return "Enviado";
        }

        return UltimaTentativaEnvioWhatsappEmUtc.HasValue && !string.IsNullOrWhiteSpace(ErroEnvioWhatsapp)
            ? "Falhou"
            : "Pendente";
    }

    public void MarcarComoUtilizado(DateTime dataUtc)
    {
        UsadoEmUtc = dataUtc;
        AtualizarDataModificacao();
    }

    public void Desativar()
    {
        Ativo = false;
        AtualizarDataModificacao();
    }

    public void DefinirCodigoConviteHash(string codigoConviteHash)
    {
        CodigoConviteHash = codigoConviteHash;
        AtualizarDataModificacao();
    }

    public void RegistrarEnvioEmailComSucesso(DateTime dataUtc)
    {
        UltimaTentativaEnvioEmailEmUtc = dataUtc;
        EmailEnviadoEmUtc = dataUtc;
        ErroEnvioEmail = null;
        AtualizarDataModificacao();
    }

    public void RegistrarFalhaEnvioEmail(string? mensagemErro, DateTime dataUtc)
    {
        UltimaTentativaEnvioEmailEmUtc = dataUtc;
        EmailEnviadoEmUtc = null;
        ErroEnvioEmail = string.IsNullOrWhiteSpace(mensagemErro)
            ? "Falha ao enviar o e-mail do convite."
            : mensagemErro.Trim();
        AtualizarDataModificacao();
    }

    public void RegistrarEnvioWhatsappComSucesso(DateTime dataUtc)
    {
        UltimaTentativaEnvioWhatsappEmUtc = dataUtc;
        WhatsappEnviadoEmUtc = dataUtc;
        ErroEnvioWhatsapp = null;
        AtualizarDataModificacao();
    }

    public void RegistrarFalhaEnvioWhatsapp(string? mensagemErro, DateTime dataUtc)
    {
        UltimaTentativaEnvioWhatsappEmUtc = dataUtc;
        WhatsappEnviadoEmUtc = null;
        ErroEnvioWhatsapp = string.IsNullOrWhiteSpace(mensagemErro)
            ? "Falha ao enviar o WhatsApp do convite."
            : mensagemErro.Trim();
        AtualizarDataModificacao();
    }
}
