using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarCompeticaoDto(
    string Nome,
    TipoCompeticao Tipo,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LigaId,
    Guid? LocalId,
    Guid? FormatoCampeonatoId,
    Guid? RegraCompeticaoId,
    bool? InscricoesAbertas,
    bool? PossuiFinalReset
);

public record AtualizarCompeticaoDto(
    string Nome,
    TipoCompeticao Tipo,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LigaId,
    Guid? LocalId,
    Guid? FormatoCampeonatoId,
    Guid? RegraCompeticaoId,
    bool? InscricoesAbertas,
    bool? PossuiFinalReset
);

public record CompeticaoDto(
    Guid Id,
    string Nome,
    TipoCompeticao Tipo,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LigaId,
    Guid? LocalId,
    Guid? FormatoCampeonatoId,
    Guid? RegraCompeticaoId,
    Guid? UsuarioOrganizadorId,
    string? NomeLiga,
    string? NomeLocal,
    string? NomeFormatoCampeonato,
    string? NomeRegraCompeticao,
    string? NomeUsuarioOrganizador,
    bool ContaRankingLiga,
    bool InscricoesAbertas,
    bool PossuiFinalReset,
    int PontosMinimosPartidaEfetivo,
    int DiferencaMinimaPartidaEfetiva,
    bool PermiteEmpateEfetivo,
    decimal PontosVitoriaEfetivo,
    decimal PontosDerrotaEfetivo,
    decimal PontosParticipacaoEfetivo,
    DateTime DataCriacao,
    DateTime DataAtualizacao
);

public record ResumoCompeticoesPublicoDto(
    int TotalGrupos
);

public record GrupoResumoUsuarioDto(
    Guid GrupoId,
    string Nome,
    GrupoResumoUltimoJogoDto? UltimoJogo,
    IReadOnlyList<GrupoResumoRankingAtletaDto> RankingTop3
);

public record GrupoResumoUltimoJogoDto(
    DateTime Data,
    IReadOnlyList<string> Dupla1,
    IReadOnlyList<string> Dupla2,
    string Placar
);

public record GrupoResumoRankingAtletaDto(
    int Posicao,
    string NomeAtleta,
    decimal Pontuacao
);
