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
    string? IpAddress = null,
    string? UserAgent = null
);

public record PoliticaPrivacidadeAtualDto(
    string Versao,
    DateTime VigenteDesdeUtc,
    bool ExigeAceitePoliticaPrivacidade,
    bool ExigeAceiteTermosUso
);
