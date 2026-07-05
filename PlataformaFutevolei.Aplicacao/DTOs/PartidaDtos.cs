using System.Text.Json.Serialization;
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
    int? DuplaVencedora,
    TipoRegistroResultado? TipoRegistroResultado,
    DateTime? DataPartida,
    string? Observacoes,
    bool PermitirDuplicidade = false,
    LocalizacaoPartidaDto? Localizacao = null
)
{
    public bool ConfirmarDuplicidade { get; init; }

    [JsonIgnore]
    public bool DuplicidadeConfirmada => PermitirDuplicidade || ConfirmarDuplicidade;
}

public record LocalizacaoPartidaDto(
    double? Latitude,
    double? Longitude,
    double? Precisao
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
    int? DuplaVencedora,
    TipoRegistroResultado? TipoRegistroResultado,
    DateTime? DataPartida,
    string? Observacoes
);

public record AtualizarPartidaBasicaDto(
    Guid? GrupoId,
    Guid? DuplaAAtleta1Id,
    string? DuplaAAtleta1Nome,
    Guid? DuplaAAtleta2Id,
    string? DuplaAAtleta2Nome,
    Guid? DuplaBAtleta1Id,
    string? DuplaBAtleta1Nome,
    Guid? DuplaBAtleta2Id,
    string? DuplaBAtleta2Nome,
    int? PlacarDuplaA,
    int? PlacarDuplaB,
    int? DuplaVencedora,
    TipoRegistroResultado? TipoRegistroResultado
);

public record VerificarDuplicidadePartidaDuplaDto(
    string? AtletaDireita,
    string? AtletaEsquerda,
    int? Pontos
);

public record VerificarDuplicidadePartidaDto(
    VerificarDuplicidadePartidaDuplaDto? Dupla1,
    VerificarDuplicidadePartidaDuplaDto? Dupla2,
    DateTime? Data
);

public record VerificarDuplicidadePartidaResultadoDto(
    bool ExisteDuplicidade,
    Guid? PartidaId,
    string Mensagem,
    PartidaDto? Partida = null
);

public static class StatusCriacaoPartida
{
    public const string Criada = "Criada";
    public const string PossivelDuplicidade = "PossivelDuplicidade";
    public const string RequerConfirmacaoDuplicidade = "RequerConfirmacaoDuplicidade";
    public const string CodigoDuplicidadeConfirmar = "PARTIDA_DUPLICADA_CONFIRMAR";
}

public record CriarPartidaResultadoDto(
    string Status,
    PartidaDto? Partida,
    ConfirmacaoDuplicidadePartidaDto? Duplicidade,
    string? Mensagem,
    string? Codigo
);

public record ConfirmacaoDuplicidadePartidaDto(
    bool RequerConfirmacao,
    string Mensagem,
    string Codigo,
    Guid? PartidaId = null,
    PartidaDto? Partida = null
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
    StatusAprovacaoPartida StatusAprovacao,
    decimal PontosClassificacaoDuplaA,
    decimal PontosClassificacaoDuplaB,
    Guid? DuplaAId,
    string NomeDuplaA,
    Guid? DuplaBId,
    string NomeDuplaB,
    int? PlacarDuplaA,
    int? PlacarDuplaB,
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

public record PartidaCompartilhamentoAtletaDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? FotoUrl
);

public record PartidaCompartilhamentoRankingVizinhoDto(
    int Posicao,
    string Apelido,
    decimal Pontos,
    string? FotoUrl
);

public record PartidaCompartilhamentoRankingDto(
    int Posicao,
    string Apelido,
    decimal Pontos,
    int Vitorias,
    int Derrotas,
    int Jogos,
    PartidaCompartilhamentoRankingVizinhoDto? AtletaAcima,
    PartidaCompartilhamentoRankingVizinhoDto? AtletaAbaixo
);

public record PartidaCompartilhamentoDto(
    Guid PartidaId,
    Guid? GrupoId,
    string? GrupoNome,
    DateTime? DataPartida,
    IReadOnlyList<PartidaCompartilhamentoAtletaDto> Dupla1,
    IReadOnlyList<PartidaCompartilhamentoAtletaDto> Dupla2,
    int? PlacarDupla1,
    int? PlacarDupla2,
    int? DuplaVencedora,
    TipoRegistroResultado TipoRegistroResultado,
    string ResultadoAtletaLogado,
    PartidaCompartilhamentoRankingDto? RankingGrupo
);

public record ArquivoMidiaPartidaDto(
    Stream Conteudo,
    string NomeArquivo,
    string? ContentType,
    long TamanhoBytes
);

public record MidiaPartidaUploadDto(
    string Url,
    string PublicId,
    MidiaPartidaTipo Tipo
);

public record MidiaPartidaRespostaDto(
    Guid PartidaId,
    string? MidiaUrl,
    string? MidiaTipo
);

public record FeedPartidaDuplaDto(
    string? Atleta1Nome,
    string? Atleta2Nome
);

public record FeedPartidaItemDto(
    Guid PartidaId,
    DateTime? Data,
    int? PlacarDupla1,
    int? PlacarDupla2,
    int? DuplaVencedora,
    TipoRegistroResultado TipoRegistroResultado,
    FeedPartidaDuplaDto Dupla1,
    FeedPartidaDuplaDto Dupla2,
    string? CriadoPorNome,
    string? CriadoPorFotoPerfilUrl,
    string? MidiaUrl,
    string? MidiaTipo,
    string? CategoriaNome,
    string? CompeticaoNome,
    string? GrupoNome
);

public record FeedPartidasRespostaDto(
    int Page,
    int PageSize,
    IReadOnlyList<FeedPartidaItemDto> Itens
);

public record PartidaDto(
    Guid Id,
    Guid? CategoriaCompeticaoId,
    Guid? GrupoId,
    string? NomeGrupo,
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
    int? PlacarDuplaA,
    int? PlacarDuplaB,
    Guid? DuplaVencedoraId,
    string? NomeDuplaVencedora,
    int? DuplaVencedora,
    TipoRegistroResultado TipoRegistroResultado,
    bool PossuiPlacarDetalhado,
    StatusAprovacaoPartida StatusAprovacao,
    decimal PesoRankingCategoria,
    decimal PontosRankingVitoria,
    DateTime? DataPartida,
    string? MidiaUrl,
    string? MidiaTipo,
    string? Observacoes,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    int QuantidadeAtletasPendentes,
    int QuantidadeAtletasPendentesSemEmail,
    IReadOnlyList<PartidaAtletaPendenteDto> AtletasPendentes
);
