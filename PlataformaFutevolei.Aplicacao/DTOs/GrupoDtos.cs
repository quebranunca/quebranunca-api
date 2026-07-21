namespace PlataformaFutevolei.Aplicacao.DTOs;

public record CriarGrupoDto(
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId,
    string? Privacidade = null,
    string? ImagemUrl = null,
    Guid? ArenaId = null,
    string? LocalPrincipal = null,
    IReadOnlyList<string>? DiasDaSemana = null
);

public record AtualizarGrupoDto(
    string Nome,
    bool? Publico = null,
    string? Privacidade = null,
    string? LocalPrincipal = null,
    IReadOnlyList<string>? DiasDaSemana = null
);

public record GrupoImagemRespostaDto(
    string? ImagemUrl
);

public record GrupoDto(
    Guid Id,
    string Nome,
    string? Descricao,
    string? Link,
    DateTime DataInicio,
    DateTime? DataFim,
    Guid? LocalId,
    Guid? UsuarioOrganizadorId,
    string? NomeLocal,
    string? NomeUsuarioOrganizador,
    string Privacidade,
    string? ImagemUrl,
    DateTime DataCriacao,
    DateTime DataAtualizacao,
    Guid? ArenaId,
    string? NomeArena,
    string? LocalPrincipal,
    IReadOnlyList<string> DiasDaSemana,
    bool Ativo
);

public record GrupoSelecaoDto(
    Guid Id,
    string Nome,
    int QuantidadeAtletas,
    string? ImagemUrl,
    string Privacidade
);

public record GrupoNomeSimilarDto(
    Guid Id,
    string Nome,
    int QuantidadeAtletas,
    string Privacidade
);

public record GrupoVerificacaoNomeDto(
    bool Disponivel,
    bool ExisteExato,
    IReadOnlyList<GrupoNomeSimilarDto> Similares
);

public record GrupoDashboardUsuarioDto(
    GrupoDashboardTotaisDto Totais,
    IReadOnlyList<GrupoDashboardItemDto> Grupos
);

public record GrupoDashboardTotaisDto(
    int QuantidadeGrupos,
    int QuantidadeAtletas,
    int QuantidadePartidas,
    int PendenciasGrupos
);

public record GrupoDashboardItemDto(
    Guid GrupoId,
    string Nome,
    string? ImagemUrl,
    string Privacidade,
    Guid? UsuarioOrganizadorId,
    string? NomeUsuarioOrganizador,
    int QuantidadeAtletas,
    int QuantidadePartidas,
    int Pendencias,
    DateTime? UltimaAtividade,
    IReadOnlyList<GrupoResumoRankingAtletaDto> RankingTop3
);

public record GrupoDashboardDetalheDto(
    GrupoDashboardCabecalhoDto Grupo,
    GrupoDashboardResumoDto Resumo,
    IReadOnlyList<GrupoDashboardRankingAtletaDto> Ranking,
    IReadOnlyList<GrupoDashboardPartidaDto> UltimasPartidas,
    IReadOnlyList<GrupoDashboardMembroAtivoDto> MembrosMaisAtivos
);

public record GrupoDashboardCabecalhoDto(
    Guid Id,
    string Nome,
    string? ImagemUrl,
    bool Publico,
    string Privacidade,
    int TotalMembros,
    int TotalPartidas,
    DateTime? UltimaPartidaEm,
    bool PodeEditar,
    bool PertenceAoGrupo,
    bool PodeRegistrarPartida,
    bool PodeExcluir,
    bool EhCriador,
    bool EhAdministrador
);

public record GrupoDashboardResumoDto(
    int TotalMembros,
    int TotalPartidas,
    int TotalAtletasAtivos,
    int TotalPartidasSemPlacar,
    DateTime? UltimaPartidaEm
);

public record GrupoDashboardRankingAtletaDto(
    int Posicao,
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? AvatarUrl,
    decimal Pontos,
    int Jogos,
    int Vitorias
);

public record GrupoDashboardAtletaPartidaDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? AvatarUrl
);

public record GrupoDashboardPartidaDto(
    Guid Id,
    DateTime? Data,
    IReadOnlyList<GrupoDashboardAtletaPartidaDto> Dupla1,
    IReadOnlyList<GrupoDashboardAtletaPartidaDto> Dupla2,
    int? PlacarDupla1,
    int? PlacarDupla2,
    int? DuplaVencedora,
    string TipoRegistroResultado,
    string Status,
    bool PossuiPlacarDetalhado
);

public record GrupoDashboardMembroAtivoDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string? AvatarUrl,
    int TotalPartidas
);
