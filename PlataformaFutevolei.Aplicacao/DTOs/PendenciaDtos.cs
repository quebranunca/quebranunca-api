using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record PendenciaUsuarioDto(
    Guid Id,
    TipoPendenciaUsuario Tipo,
    PrioridadePendenciaUsuario Prioridade,
    StatusPendenciaUsuario Status,
    DateTime DataCriacao,
    DateTime? DataConclusao,
    string? Observacao,
    Guid UsuarioId,
    Guid? AtletaId,
    string? NomeAtleta,
    string? EmailAtleta,
    bool? AtletaPossuiUsuarioVinculado,
    Guid? PartidaId,
    DateTime? DataPartida,
    StatusPartida? StatusPartida,
    StatusAprovacaoPartida? StatusAprovacaoPartida,
    string? NomeDuplaA,
    string? NomeDuplaAAtleta1,
    string? NomeDuplaAAtleta2,
    string? NomeDuplaB,
    string? NomeDuplaBAtleta1,
    string? NomeDuplaBAtleta2,
    int? PlacarDuplaA,
    int? PlacarDuplaB,
    Guid? CriadoPorUsuarioId,
    string? NomeCriadoPorUsuario
);

public record PendenciasResumoDto(
    int Total,
    int AltaPrioridade,
    int MediaPrioridade,
    int BaixaPrioridade
);

public record ResponderPendenciaPartidaDto(
    string? Observacao
);

public record AtualizarContatoPendenciaDto(
    string Email
);

public record UsuarioAtletaPendenciaDto(
    Guid UsuarioId,
    Guid AtletaId,
    string Nome,
    string? Apelido
);

public record AtualizarContatoPendenciaResultadoDto(
    bool UsuarioJaCadastrado,
    PendenciaUsuarioDto Pendencia,
    UsuarioAtletaPendenciaDto? UsuarioEncontrado
);

public record ConfirmarVinculoAtletaPendenciaDto(
    Guid UsuarioId
);
