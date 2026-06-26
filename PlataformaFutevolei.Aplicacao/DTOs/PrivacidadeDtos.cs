namespace PlataformaFutevolei.Aplicacao.DTOs;

public record PreferenciasPrivacidadeDto(
    bool PerfilPublico,
    bool ExibirEmail,
    bool PermitirUsoLocalizacao,
    bool PermitirUsoImagem,
    bool PossuiFotoPerfil,
    bool ExclusaoSolicitada
);

public record AtualizarPreferenciasPrivacidadeDto(
    bool PerfilPublico,
    bool ExibirEmail,
    bool PermitirUsoLocalizacao,
    bool PermitirUsoImagem,
    bool RemoverFotoPerfil = false
);

public record RegistrarConsentimentoLgpdDto(
    bool AceitouPoliticaPrivacidade,
    bool AceitouTermosUso,
    bool AceitouUsoLocalizacao,
    bool AceitouUsoImagem,
    string? VersaoPoliticaPrivacidade = null,
    string? VersaoTermosUso = null,
    bool DeclarouMaiorDe18 = false,
    bool AceitouMarketing = false,
    string? Origem = null,
    string? IpAddress = null,
    string? UserAgent = null
);

public record PoliticaPrivacidadeAtualDto(
    string Versao,
    DateTime VigenteDesdeUtc,
    bool ExigeAceitePoliticaPrivacidade,
    bool ExigeAceiteTermosUso
);

public record TermosVersaoAtualDto(
    string VersaoTermos,
    string UrlTermos,
    string VersaoPoliticaPrivacidade,
    string UrlPoliticaPrivacidade
);
