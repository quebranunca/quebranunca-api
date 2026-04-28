using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarConviteCadastroDto(
    string Email,
    string? Telefone,
    PerfilUsuario? PerfilDestino,
    DateTime? ExpiraEmUtc,
    string? CanalEnvio
);

public record ConviteCadastroDto(
    Guid Id,
    string Email,
    string? Telefone,
    PerfilUsuario PerfilDestino,
    DateTime ExpiraEmUtc,
    DateTime? UsadoEmUtc,
    bool Ativo,
    Guid CriadoPorUsuarioId,
    string? CriadoPorUsuarioNome,
    string? CanalEnvio,
    string Situacao,
    bool PodeSerUsado,
    string SituacaoEnvioEmail,
    DateTime? UltimaTentativaEnvioEmailEmUtc,
    DateTime? EmailEnviadoEmUtc,
    string? ErroEnvioEmail,
    string SituacaoEnvioWhatsapp,
    DateTime? UltimaTentativaEnvioWhatsappEmUtc,
    DateTime? WhatsappEnviadoEmUtc,
    string? ErroEnvioWhatsapp,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record ConviteCadastroLinkAceiteDto(
    string LinkAceite,
    string CodigoConvite
);

public record ConviteCadastroPublicoDto(
    Guid Id,
    string IdentificadorPublico,
    string EmailMascarado,
    PerfilUsuario PerfilDestino,
    DateTime ExpiraEmUtc,
    string Situacao,
    bool PodeSerUsado
);

public record AtletaElegivelConviteCadastroDto(
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    string Email,
    string? Telefone
);

public record ResultadoEnvioEmailConviteDto(
    bool TentativaRealizada,
    bool Enviado,
    string? Erro,
    string? IdentificadorMensagem
);

public record ResultadoEnvioWhatsappConviteDto(
    bool TentativaRealizada,
    bool Enviado,
    string? Erro,
    string? IdentificadorMensagem
);
