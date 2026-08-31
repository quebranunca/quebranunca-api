namespace PlataformaFutevolei.Aplicacao.DTOs;

public record ConsultarConfirmacaoPresencaGrupoDto(string Codigo);

public record ResponderConfirmacaoPresencaGrupoDto(string Codigo, bool VaiParticipar);

public record ConfirmacaoPresencaGrupoPublicaDto(
    string NomeGrupo,
    string NomeAtleta,
    DateOnly DataJogo,
    string HorarioInicio,
    string HorarioFim,
    string? NomeArena,
    string Status,
    bool PodeResponder,
    DateTime? RespondidaEmUtc
);

public record AgendaGrupoDto(
    Guid? ArenaId,
    string? NomeArena,
    string? LocalPrincipal,
    IReadOnlyList<string> DiasDaSemana,
    string? HorarioInicio,
    string? HorarioFim,
    bool Completa
);

public record PresencaGrupoMembroDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? AvatarUrl,
    string Status,
    DateTime? RespondidaEmUtc,
    bool PossuiWhatsapp,
    string StatusEnvioWhatsapp
);

public record EncontroPresencaGrupoDto(
    Guid Id,
    DateOnly DataJogo,
    string HorarioInicio,
    string HorarioFim,
    string? NomeArena,
    int TotalMembros,
    int TotalConfirmados,
    int TotalNaoVao,
    int TotalPendentes,
    IReadOnlyList<PresencaGrupoMembroDto> Membros
);

public record PainelPresencaGrupoDto(
    Guid GrupoId,
    string NomeGrupo,
    AgendaGrupoDto Agenda,
    DateOnly? ProximaDataJogo,
    EncontroPresencaGrupoDto? EncontroHoje
);
