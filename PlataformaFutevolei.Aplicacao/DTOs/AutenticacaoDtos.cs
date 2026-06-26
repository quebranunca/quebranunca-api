using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record RegistrarUsuarioRequisicaoDto(
    string? ConviteIdPublico,
    string? CodigoConvite,
    string Nome,
    string Email,
    string? Senha,
    bool AceitouPoliticaPrivacidade = false,
    bool AceitouTermosUso = false,
    bool AceitouUsoLocalizacao = false,
    bool AceitouUsoImagem = false,
    string? IpAddress = null,
    string? UserAgent = null
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

public record DefinirSenhaRequisicaoDto(
    string Senha,
    string ConfirmacaoSenha
);

public record AlterarSenhaRequisicaoDto(
    string SenhaAtual,
    string NovaSenha,
    string ConfirmacaoNovaSenha
);

public record SegurancaUsuarioDto(
    bool PossuiSenha
);

public record RenovarTokenRequisicaoDto(
    string Token,
    string RefreshToken
);

public record IniciarAcessoRequisicaoDto(
    string Email
);

public record IniciarAcessoRespostaDto(
    string Status,
    string EmailMascarado,
    bool PodeEntrarComSenha,
    bool CadastroNovo,
    string Mensagem,
    string? CodigoDesenvolvimento = null
);

public record ConfirmarCodigoAcessoRequisicaoDto(
    string Email,
    string Codigo
);

public record ConfirmarCodigoAcessoRespostaDto(
    string Status,
    string? Token = null,
    string? RefreshToken = null,
    DateTime? TokenExpiraEmUtc = null,
    DateTime? RefreshTokenExpiraEmUtc = null,
    UsuarioLogadoDto? Usuario = null,
    string? CadastroToken = null,
    bool EmailConfirmado = false
);

public record CompletarCadastroPublicoRequisicaoDto(
    string CadastroToken,
    string NomeExibicao,
    string? Apelido,
    bool AceitouTermos,
    string? VersaoTermos,
    bool AceitouPoliticaPrivacidade,
    string? VersaoPoliticaPrivacidade,
    bool DeclarouMaiorDe18,
    bool AceitouMarketing,
    string? IpAddress = null,
    string? UserAgent = null
);

public record SolicitarRedefinicaoSenhaRespostaDto(
    string Mensagem
);

public record CriarSolicitacaoAcessoDto(
    string Nome,
    string Email
);

public record SolicitacaoAcessoRespostaDto(
    string Mensagem
);

public record SolicitacaoAcessoAdminDto(
    Guid Id,
    string Nome,
    string Email,
    StatusSolicitacaoAcesso Status,
    DateTime DataCriacao,
    DateTime DataAtualizacao
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
    AtletaResumoDto? Atleta,
    bool PerfilPublico,
    bool ExibirEmail,
    bool PermitirUsoLocalizacao,
    bool PermitirUsoImagem,
    string? FotoPerfilUrl,
    bool PossuiSenha,
    bool PoliticaPrivacidadePendente,
    bool ExclusaoSolicitada
);

public record UsuarioDto(
    Guid Id,
    string Nome,
    string Email,
    PerfilUsuario Perfil,
    bool Ativo,
    Guid? AtletaId,
    AtletaResumoDto? Atleta,
    string? FotoPerfilUrl,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record ArquivoFotoPerfilDto(
    Stream Conteudo,
    string NomeArquivo,
    string? ContentType,
    long TamanhoBytes
);

public record FotoPerfilUploadDto(
    string Url,
    string PublicId
);

public record FotoPerfilRespostaDto(
    string FotoPerfilUrl
);

public record UsuarioResumoDto(
    string Nome,
    int TotalPartidas,
    int TotalVitorias,
    int TotalDerrotas,
    decimal PercentualAproveitamento,
    int TotalPartidasPendentes,
    decimal Pontos,
    decimal PontosPendentes
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
