using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ConviteCadastro : EntidadeBase
{
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string IdentificadorPublico { get; set; } = string.Empty;
    public string? CodigoConvite { get; set; }
    public string? CodigoConviteHash { get; set; }
    public PerfilUsuario PerfilDestino { get; set; } = PerfilUsuario.Atleta;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? UsadoEmUtc { get; set; }
    public bool Ativo { get; set; } = true;
    public Guid CriadoPorUsuarioId { get; set; }
    public Guid? AtletaId { get; set; }
    public Guid? PartidaId { get; set; }
    public string? CanalEnvio { get; set; }
    public DateTime? UltimaTentativaEnvioEmailEmUtc { get; set; }
    public DateTime? EmailEnviadoEmUtc { get; set; }
    public string? ErroEnvioEmail { get; set; }
    public DateTime? UltimaTentativaEnvioWhatsappEmUtc { get; set; }
    public DateTime? WhatsappEnviadoEmUtc { get; set; }
    public string? ErroEnvioWhatsapp { get; set; }
    public string? WhatsappEntregaId { get; set; }
    public string? WhatsappIdempotencyKey { get; set; }
    public DateTime? UltimaTentativaEnvioSmsEmUtc { get; set; }
    public DateTime? SmsEnviadoEmUtc { get; set; }
    public string? ErroEnvioSms { get; set; }
    public string? SmsEntregaId { get; set; }
    public string? SmsIdempotencyKey { get; set; }

    public Usuario? CriadoPorUsuario { get; set; }
    public Atleta? Atleta { get; set; }
    public Partida? Partida { get; set; }

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
            : !string.IsNullOrWhiteSpace(WhatsappEntregaId)
                ? "Processando"
                : "Pendente";
    }

    public string ObterSituacaoEnvioSms()
    {
        if (SmsEnviadoEmUtc.HasValue) return "Enviado";
        if (UltimaTentativaEnvioSmsEmUtc.HasValue && !string.IsNullOrWhiteSpace(ErroEnvioSms)) return "Falhou";
        return !string.IsNullOrWhiteSpace(SmsEntregaId) ? "Processando" : "Pendente";
    }

    public void MarcarComoUtilizado(DateTime dataUtc)
    {
        UsadoEmUtc = dataUtc;
        CodigoConvite = null;
        AtualizarDataModificacao();
    }

    public void Desativar()
    {
        Ativo = false;
        AtualizarDataModificacao();
    }

    public void DefinirCodigoConvite(string codigoConvite, string codigoConviteHash)
    {
        CodigoConvite = codigoConvite;
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

    public void RegistrarEnvioWhatsappComSucesso(DateTime dataUtc, string? identificadorMensagem = null)
    {
        UltimaTentativaEnvioWhatsappEmUtc = dataUtc;
        WhatsappEnviadoEmUtc = dataUtc;
        ErroEnvioWhatsapp = null;
        WhatsappEntregaId = identificadorMensagem;
        AtualizarDataModificacao();
    }

    public void PrepararSolicitacaoWhatsapp(string idempotencyKey, DateTime dataUtc)
    {
        WhatsappIdempotencyKey = idempotencyKey;
        WhatsappEntregaId = null;
        UltimaTentativaEnvioWhatsappEmUtc = dataUtc;
        WhatsappEnviadoEmUtc = null;
        ErroEnvioWhatsapp = null;
        AtualizarDataModificacao();
    }

    public void RegistrarSolicitacaoWhatsappAceita(string identificadorEntrega, DateTime dataUtc)
    {
        WhatsappEntregaId = identificadorEntrega;
        UltimaTentativaEnvioWhatsappEmUtc = dataUtc;
        WhatsappEnviadoEmUtc = null;
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

    public void PrepararSolicitacaoSms(string idempotencyKey, DateTime dataUtc)
    {
        SmsIdempotencyKey = idempotencyKey;
        SmsEntregaId = null;
        UltimaTentativaEnvioSmsEmUtc = dataUtc;
        SmsEnviadoEmUtc = null;
        ErroEnvioSms = null;
        AtualizarDataModificacao();
    }

    public void RegistrarSolicitacaoSmsAceita(string identificadorEntrega, DateTime dataUtc)
    {
        SmsEntregaId = identificadorEntrega;
        UltimaTentativaEnvioSmsEmUtc = dataUtc;
        SmsEnviadoEmUtc = null;
        ErroEnvioSms = null;
        AtualizarDataModificacao();
    }

    public void RegistrarEnvioSmsComSucesso(DateTime dataUtc, string? identificadorMensagem = null)
    {
        SmsEntregaId = identificadorMensagem;
        UltimaTentativaEnvioSmsEmUtc = dataUtc;
        SmsEnviadoEmUtc = dataUtc;
        ErroEnvioSms = null;
        AtualizarDataModificacao();
    }

    public void RegistrarFalhaEnvioSms(string? mensagemErro, DateTime dataUtc)
    {
        UltimaTentativaEnvioSmsEmUtc = dataUtc;
        SmsEnviadoEmUtc = null;
        ErroEnvioSms = string.IsNullOrWhiteSpace(mensagemErro) ? "Falha ao enviar o SMS do convite." : mensagemErro.Trim();
        AtualizarDataModificacao();
    }
}
