using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarPartidaDto(
    Guid? CompeticaoId,
    Guid? GrupoId,
    string? NomeGrupo,
    Guid? CategoriaCompeticaoId,
    Guid? DuplaAId,
    Guid? DuplaBId,
    Guid? DuplaAAtleta1Id,
    string? DuplaAAtleta1Nome,
    Guid? DuplaAAtleta2Id,
    string? DuplaAAtleta2Nome,
    Guid? DuplaBAtleta1Id,
    string? DuplaBAtleta1Nome,
    Guid? DuplaBAtleta2Id,
    string? DuplaBAtleta2Nome,
    string? FaseCampeonato,
    StatusPartida Status,
    int? PlacarDuplaA,
    int? PlacarDuplaB,
    DateTime? DataPartida,
    string? Observacoes
);

public record AtualizarPartidaDto(
    Guid? CompeticaoId,
    Guid? GrupoId,
    string? NomeGrupo,
    Guid? CategoriaCompeticaoId,
    Guid? DuplaAId,
    Guid? DuplaBId,
    Guid? DuplaAAtleta1Id,
    string? DuplaAAtleta1Nome,
    Guid? DuplaAAtleta2Id,
    string? DuplaAAtleta2Nome,
    Guid? DuplaBAtleta1Id,
    string? DuplaBAtleta1Nome,
    Guid? DuplaBAtleta2Id,
    string? DuplaBAtleta2Nome,
    string? FaseCampeonato,
    StatusPartida Status,
    int? PlacarDuplaA,
    int? PlacarDuplaB,
    DateTime? DataPartida,
    string? Observacoes
);

public record GerarTabelaCategoriaDto(
    bool SubstituirTabelaExistente = false
);

public record GeracaoTabelaCategoriaDto(
    Guid CategoriaId,
    string NomeCategoria,
    int QuantidadePartidasGeradas,
    bool SubstituiuTabelaExistente,
    string Resumo,
    IReadOnlyList<PartidaDto> Partidas
);

public record ChaveamentoCategoriaDto(
    Guid CategoriaId,
    string NomeCategoria,
    bool PossuiFinalReset,
    IReadOnlyList<PartidaDto> Partidas
);

public record RemocaoTabelaCategoriaDto(
    Guid CategoriaId,
    string NomeCategoria,
    int QuantidadePartidasRemovidas,
    string Resumo
);

public record RodadaEstruturaCompeticaoDto(
    int NumeroRodada,
    string NomeRodada,
    IReadOnlyList<JogoRodadaCompeticaoDto> Jogos
);

public record JogoRodadaCompeticaoDto(
    Guid PartidaId,
    int OrdemJogo,
    string TipoJogo,
    string? NomeFase,
    StatusPartida Status,
    Guid? DuplaAId,
    string NomeDuplaA,
    Guid? DuplaBId,
    string NomeDuplaB,
    int PlacarDuplaA,
    int PlacarDuplaB,
    Guid? DuplaVencedoraId,
    string? NomeDuplaVencedora,
    DateTime? DataPartida
);

public record SituacaoDuplaCompeticaoDto(
    Guid DuplaId,
    string NomeDupla,
    int QuantidadeDerrotas,
    string Status,
    string PosicaoAtual,
    Guid? PartidaPendenteId,
    string? NomePartidaPendente
);

public record PartidaAtletaPendenteDto(
    Guid AtletaId,
    string NomeAtleta,
    string? Email,
    bool TemEmail,
    string StatusPendencia
);

public record PartidaDto(
    Guid Id,
    Guid? CategoriaCompeticaoId,
    Guid? GrupoId,
    string NomeCategoria,
    Guid? CriadoPorUsuarioId,
    string? NomeCriadoPorUsuario,
    Guid? DuplaAId,
    string NomeDuplaA,
    Guid? DuplaAAtleta1Id,
    string? NomeDuplaAAtleta1,
    Guid? DuplaAAtleta2Id,
    string? NomeDuplaAAtleta2,
    Guid? DuplaBId,
    string NomeDuplaB,
    Guid? DuplaBAtleta1Id,
    string? NomeDuplaBAtleta1,
    Guid? DuplaBAtleta2Id,
    string? NomeDuplaBAtleta2,
    string? FaseCampeonato,
    LadoDaChave? LadoDaChave,
    int? Rodada,
    int? PosicaoNaChave,
    Guid? PartidaOrigemParticipanteAId,
    OrigemClassificacaoPartida? OrigemParticipanteATipo,
    Guid? PartidaOrigemParticipanteBId,
    OrigemClassificacaoPartida? OrigemParticipanteBTipo,
    Guid? ProximaPartidaVencedorId,
    Guid? ProximaPartidaPerdedorId,
    SlotDestinoPartida? SlotDestinoVencedor,
    SlotDestinoPartida? SlotDestinoPerdedor,
    bool Ativa,
    bool EhPreliminar,
    bool EhFinal,
    bool EhFinalissima,
    StatusPartida Status,
    int PlacarDuplaA,
    int PlacarDuplaB,
    Guid? DuplaVencedoraId,
    string? NomeDuplaVencedora,
    StatusAprovacaoPartida StatusAprovacao,
    decimal PesoRankingCategoria,
    decimal PontosRankingVitoria,
    DateTime? DataPartida,
    string? Observacoes,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    int QuantidadeAtletasPendentes,
    int QuantidadeAtletasPendentesSemEmail,
    IReadOnlyList<PartidaAtletaPendenteDto> AtletasPendentes
);
