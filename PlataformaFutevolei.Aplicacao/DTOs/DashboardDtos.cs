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
