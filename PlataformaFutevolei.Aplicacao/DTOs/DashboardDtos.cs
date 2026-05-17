namespace PlataformaFutevolei.Aplicacao.DTOs;

public record DashboardAtletaDto(
    DashboardAtletaPerfilDto Perfil,
    DashboardAtletaResumoDto Resumo,
    IReadOnlyList<DashboardAtletaMetricaDto> Metricas,
    IReadOnlyList<DashboardAtletaEvolucaoDto> Evolucao,
    IReadOnlyList<DashboardAtletaPartidaDto> UltimasPartidas,
    IReadOnlyList<DashboardAtletaRelacaoDto> MelhoresParceiros,
    IReadOnlyList<DashboardAtletaRelacaoDto> RivaisMaisEnfrentados,
    IReadOnlyList<DashboardAtletaHeatmapDiaDto> Heatmap,
    IReadOnlyList<string> Insights
);

public record DashboardAtletaPerfilDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string CategoriaPrincipal,
    int? PosicaoRanking,
    decimal Aproveitamento,
    int SequenciaAtual,
    string TextoSequencia
);

public record DashboardAtletaResumoDto(
    int TotalPartidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    int SaldoPontos,
    int SequenciaAtual,
    string? MelhorParceiro,
    string? RivalMaisFrequente
);

public record DashboardAtletaMetricaDto(
    string Id,
    string Rotulo,
    string Valor,
    string? Complemento,
    string Icone,
    bool Destaque = false
);

public record DashboardAtletaEvolucaoDto(
    string Mes,
    int Ano,
    int NumeroMes,
    int Partidas,
    int Vitorias,
    decimal Aproveitamento,
    int? PosicaoRanking
);

public record DashboardAtletaPartidaDto(
    Guid Id,
    string Resultado,
    DateTime? DataPartida,
    string? Grupo,
    string? Categoria,
    string? Competicao,
    string? Status,
    int? StatusAprovacao,
    string Parceiro,
    string Adversarios,
    int PlacarSuaDupla,
    int PlacarAdversarios
);

public record DashboardAtletaRelacaoDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    int Partidas,
    int Vitorias,
    decimal Aproveitamento
);

public record DashboardAtletaHeatmapDiaDto(
    DateOnly Data,
    int Quantidade
);

public record DashboardPublicoDto(
    DashboardPublicoResumoDto Resumo,
    IReadOnlyList<DashboardPublicoMetricaDto> Metricas,
    IReadOnlyList<DashboardPublicoPartidaDto> UltimasPartidas,
    IReadOnlyList<DashboardPublicoRankingAtletaDto> Ranking,
    IReadOnlyList<DashboardPublicoAtletaDestaqueDto> AtletasDestaque,
    IReadOnlyList<DashboardPublicoGrupoDto> Grupos,
    IReadOnlyList<DashboardPublicoCampeonatoDto> Campeonatos,
    IReadOnlyList<DashboardPublicoRegiaoDto> Regioes,
    IReadOnlyList<string> Insights
);

public record DashboardPublicoResumoDto(
    int TotalPartidas,
    int TotalAtletas,
    int TotalGrupos,
    int TotalCampeonatos,
    int PartidasHoje,
    int AtletasOnline,
    int CidadesAtivas
);

public record DashboardPublicoMetricaDto(
    string Id,
    string Rotulo,
    string Valor,
    string? Complemento,
    string Icone
);

public record DashboardPublicoPartidaDto(
    Guid Id,
    DateTime Data,
    string? Grupo,
    string? Campeonato,
    string Dupla1,
    string Dupla2,
    int PontosDupla1,
    int PontosDupla2,
    string Vencedor,
    int MinutosAtras
);

public record DashboardPublicoRankingAtletaDto(
    int Posicao,
    Guid AtletaId,
    string Nome,
    string? Apelido,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    decimal Pontos,
    int SequenciaAtual
);

public record DashboardPublicoAtletaDestaqueDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string Destaque,
    string Valor,
    string Complemento
);

public record DashboardPublicoGrupoDto(
    Guid GrupoId,
    string Nome,
    int Partidas,
    int Atletas,
    DateTime? UltimaAtividade
);

public record DashboardPublicoCampeonatoDto(
    Guid CampeonatoId,
    string Nome,
    string Status,
    int Partidas,
    DateTime? ProximaPartida,
    string? Local
);

public record DashboardPublicoRegiaoDto(
    string Cidade,
    string? Estado,
    int Partidas
);
