using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ConfirmacaoPresencaGrupo : EntidadeBase
{
    public Guid EncontroGrupoId { get; set; }
    public Guid AtletaId { get; set; }
    public string CodigoAcesso { get; set; } = string.Empty;
    public StatusConfirmacaoPresencaGrupo Status { get; set; } = StatusConfirmacaoPresencaGrupo.Pendente;
    public DateTime ExpiraEmUtc { get; set; }
    public DateTime? RespondidaEmUtc { get; set; }
    public int TentativasEnvioWhatsapp { get; set; }
    public DateTime? UltimaTentativaEnvioWhatsappEmUtc { get; set; }
    public DateTime? WhatsappEnviadoEmUtc { get; set; }
    public string? WhatsappMensagemId { get; set; }
    public string? ErroEnvioWhatsapp { get; set; }

    public EncontroGrupo EncontroGrupo { get; set; } = default!;
    public Atleta Atleta { get; set; } = default!;

    public void Responder(bool vaiParticipar, DateTime agoraUtc)
    {
        Status = vaiParticipar
            ? StatusConfirmacaoPresencaGrupo.Confirmada
            : StatusConfirmacaoPresencaGrupo.NaoVai;
        RespondidaEmUtc = agoraUtc;
        AtualizarDataModificacao();
    }

    public void RegistrarResultadoEnvioWhatsapp(
        bool tentativaRealizada,
        bool enviado,
        string? erro,
        string? identificadorMensagem,
        DateTime agoraUtc)
    {
        UltimaTentativaEnvioWhatsappEmUtc = agoraUtc;
        if (tentativaRealizada)
        {
            TentativasEnvioWhatsapp++;
        }

        if (enviado)
        {
            WhatsappEnviadoEmUtc = agoraUtc;
            WhatsappMensagemId = identificadorMensagem;
            ErroEnvioWhatsapp = null;
        }
        else
        {
            ErroEnvioWhatsapp = string.IsNullOrWhiteSpace(erro)
                ? "Não foi possível enviar a confirmação por WhatsApp."
                : erro.Trim();
        }

        AtualizarDataModificacao();
    }
}
