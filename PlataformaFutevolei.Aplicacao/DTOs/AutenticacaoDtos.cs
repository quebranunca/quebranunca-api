using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record RegistrarUsuarioRequisicaoDto(
    string? ConviteIdPublico,
    string? CodigoConvite,
    string Nome,
    string Email,
    string? Senha
);

public record LoginRequisicaoDto(
    string Email,
    string Senha
);

public record SolicitarCodigoLoginRequisicaoDto(
    string Email
);

public record SolicitarCodigoLoginRespostaDto(
    string Mensagem,
    string? CodigoDesenvolvimento = null
);

public record LoginCodigoRequisicaoDto(
    string Email,
    string Codigo
);

public record EsqueciSenhaRequisicaoDto(
    string Email
);

public record RedefinirSenhaRequisicaoDto(
    string Email,
    string Codigo,
    string NovaSenha
);

public record RenovarTokenRequisicaoDto(
    string Token,
    string RefreshToken
);

public record SolicitarRedefinicaoSenhaRespostaDto(
    string Mensagem
);

public record ResultadoEnvioEmailCodigoLoginDto(
    bool TentativaRealizada,
    bool Enviado,
    string? Erro,
    string? IdentificadorMensagem,
    string? CodigoDesenvolvimento = null
);

public record UsuarioLogadoDto(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil,
    bool Ativo,
    Guid? AtletaId,
    AtletaResumoDto? Atleta
);

public record UsuarioDto(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil,
    bool Ativo,
    Guid? AtletaId,
    AtletaResumoDto? Atleta,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record UsuarioResumoDto(
    int TotalPartidas,
    int TotalVitorias,
    int TotalDerrotas,
    decimal PercentualAproveitamento,
    int TotalPartidasPendentes
);

public record AtualizarMeuUsuarioDto(
    string Nome
);

public record VincularAtletaUsuarioDto(
    Guid AtletaId
);

public record AtualizarUsuarioDto(
    string Nome,
    string Email,
    PerfilUsuario Perfil,
    bool Ativo,
    Guid? AtletaId
);

public record RespostaAutenticacaoDto(
    string Token,
    string RefreshToken,
    DateTime TokenExpiraEmUtc,
    DateTime RefreshTokenExpiraEmUtc,
    UsuarioLogadoDto Usuario
);
