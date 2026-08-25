namespace PlataformaFutevolei.Aplicacao.DTOs;

public enum CanalNotificacaoExterna
{
    Email = 1,
    Whatsapp = 2,
    Sms = 3
}

public sealed record SolicitacaoEntregaNotificacaoDto(
    string Origem,
    string ChaveIdempotencia,
    CanalNotificacaoExterna Canal,
    string TemplateKey,
    string Destinatario,
    IReadOnlyDictionary<string, string> Dados);

public sealed record ResultadoEntregaNotificacaoDto(
    bool TentativaRealizada,
    bool Enviado,
    string? Erro,
    string? IdentificadorMensagem,
    bool Aceito = false);
