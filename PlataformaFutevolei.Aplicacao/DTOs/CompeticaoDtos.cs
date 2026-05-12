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

public record SalvarCampeonatoCategoriaDto(
    Guid? Id,
    Guid CategoriaId,
    decimal? ValorInscricao,
    int? LimiteDuplas,
    StatusInscricoesCategoriaCampeonato StatusInscricao,
    DateTime? DataAberturaInscricao,
    DateTime? DataEncerramentoInscricao,
    bool? PermiteListaEspera,
    string? Observacao
);

public record CriarCampeonatoDto(
    string Nome,
    Guid LocalId,
    DateTime DataInicio,
    DateTime? DataFim,
    string? Descricao,
    string? Status,
    Guid? LigaId,
    Guid? FormatoCampeonatoId,
    Guid? RegraCompeticaoId,
    bool? PossuiFinalReset,
    IReadOnlyList<SalvarCampeonatoCategoriaDto> Categorias
);

public record AtualizarCampeonatoDto(
    string Nome,
    Guid LocalId,
    DateTime DataInicio,
    DateTime? DataFim,
    string? Descricao,
    string? Status,
    Guid? LigaId,
    Guid? FormatoCampeonatoId,
    Guid? RegraCompeticaoId,
    bool? PossuiFinalReset,
    IReadOnlyList<SalvarCampeonatoCategoriaDto> Categorias
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
    string? StatusCampeonato,
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

public record CampeonatoDetalheDto(
    CompeticaoDto Campeonato,
    IReadOnlyList<CategoriaCompeticaoDto> Categorias
);

public record ResumoCompeticoesPublicoDto(
    int TotalAtletas,
    int TotalJogos,
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
    IReadOnlyList<GrupoResumoAtletaDto> Dupla1,
    IReadOnlyList<GrupoResumoAtletaDto> Dupla2,
    int PlacarDupla1,
    int PlacarDupla2,
    int Status,
    int StatusAprovacao
);

public record GrupoResumoAtletaDto(
    Guid Id,
    string Nome,
    string? Apelido
);

public record GrupoResumoRankingAtletaDto(
    int Posicao,
    Guid AtletaId,
    string NomeAtleta,
    string? ApelidoAtleta,
    decimal Pontuacao,
    bool UsuarioLogado
);
