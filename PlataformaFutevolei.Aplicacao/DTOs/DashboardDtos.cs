namespace PlataformaFutevolei.Aplicacao.DTOs;

public record DashboardAtletaDto(
    DashboardAtletaPerfilDto Perfil,
    DashboardAtletaResumoDto Resumo,
    IReadOnlyList<DashboardAtletaMetricaDto> Metricas,
    IReadOnlyList<DashboardAtletaEvolucaoDto> Evolucao,
    IReadOnlyList<DashboardAtletaPartidaDto> UltimasPartidas,
    IReadOnlyList<DashboardAtletaRelacaoDto> MelhoresParceiros,
    IReadOnlyList<DashboardAtletaRelacaoDto> RivaisMaisEnfrentados,
    IReadOnlyList<DashboardAtletaRelacaoDto> ParceirosRecentes,
    IReadOnlyList<DashboardAtletaRelacaoDto> RivaisRecentes,
    IReadOnlyList<DashboardAtletaHeatmapDiaDto> Heatmap,
    IReadOnlyList<string> Insights,
    DashboardScoutSequenciaDto? Sequencia = null,
    DashboardScoutEstatisticasPontosDto? EstatisticasPontos = null,
    IReadOnlyList<DashboardScoutResultadoRecenteDto>? FormaRecente = null,
    IReadOnlyList<DashboardAtletaGrupoDto>? DesempenhoPorGrupo = null,
    IReadOnlyList<DashboardDuplaParceiroDto>? DuplasDisponiveis = null
);

public record DashboardAtletaPerfilDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string CategoriaPrincipal,
    int? PosicaoRanking,
    decimal Aproveitamento,
    int SequenciaAtual,
    string TextoSequencia,
    string? FotoPerfilUrl
);

public record DashboardAtletaResumoDto(
    int TotalPartidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    int SaldoPontos,
    int SequenciaAtual,
    string? MelhorParceiro,
    string? RivalMaisFrequente,
    int PontosPro = 0,
    int PontosContra = 0,
    int PartidasComPlacar = 0,
    int MelhorSequenciaVitorias = 0,
    string? TipoSequenciaAtual = null,
    string? TextoSequenciaAtual = null
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
    int? PosicaoRanking,
    decimal? AproveitamentoDados = null,
    bool PossuiDados = true
);

public record DashboardAtletaPartidaDto(
    Guid Id,
    Guid? CriadoPorUsuarioId,
    string Resultado,
    DateTime? DataPartida,
    Guid? GrupoId,
    Guid? CategoriaCompeticaoId,
    string? Grupo,
    string? Categoria,
    string? Competicao,
    string? Status,
    int? StatusAprovacao,
    Guid? DuplaAId,
    Guid? DuplaAAtleta1Id,
    string? NomeDuplaAAtleta1,
    Guid? DuplaAAtleta2Id,
    string? NomeDuplaAAtleta2,
    Guid? DuplaBId,
    Guid? DuplaBAtleta1Id,
    string? NomeDuplaBAtleta1,
    Guid? DuplaBAtleta2Id,
    string? NomeDuplaBAtleta2,
    string Parceiro,
    string Adversarios,
    int? PlacarSuaDupla,
    int? PlacarAdversarios,
    bool PossuiPlacarDetalhado = false,
    string? TipoRegistroResultado = null,
    string? GrupoOuContexto = null,
    IReadOnlyList<DashboardDuplaAtletaDto>? SuaDupla = null,
    IReadOnlyList<DashboardDuplaAtletaDto>? DuplaAdversaria = null
);

public record DashboardAtletaRelacaoDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    int Partidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    DateTime? UltimaPartida,
    string? FotoPerfilUrl,
    string? TipoSequenciaAtual = null,
    int SequenciaAtual = 0,
    int PontosPro = 0,
    int PontosContra = 0,
    int SaldoPontos = 0,
    int PartidasComPlacar = 0
);

public record DashboardAtletaHeatmapDiaDto(
    DateOnly Data,
    int Quantidade
);

public record DashboardAtletaConexoesDto(
    IReadOnlyList<DashboardAtletaRelacaoDto> MelhoresParceiros,
    IReadOnlyList<DashboardAtletaRelacaoDto> RivaisMaisEnfrentados,
    IReadOnlyList<DashboardAtletaRelacaoDto> ParceirosRecentes,
    IReadOnlyList<DashboardAtletaRelacaoDto> RivaisRecentes,
    DashboardAtletaRelacaoDto? ParceiroMaisJogou = null,
    DashboardAtletaRelacaoDto? ParceiroMaisVitorias = null,
    DashboardAtletaRelacaoDto? MelhorParceria = null,
    DashboardAtletaRelacaoDto? AdversarioMaisEnfrentado = null,
    DashboardAtletaRelacaoDto? AdversarioMaisVencido = null,
    DashboardAtletaRelacaoDto? AdversarioMaisDificil = null
);

public record DashboardScoutSequenciaDto(
    string Tipo,
    int Quantidade,
    string Texto,
    int MelhorSequenciaVitorias
);

public record DashboardScoutEstatisticasPontosDto(
    bool Disponivel,
    int PartidasComPlacar,
    int? PontosPro,
    int? PontosContra,
    int? Saldo,
    decimal? MediaPontosPro,
    decimal? MediaPontosContra,
    decimal? MediaSaldo,
    int? MaiorVitoriaMargem,
    int? DerrotaMaisApertadaMargem,
    int JogosDiferencaMinima
);

public record DashboardScoutResultadoRecenteDto(
    Guid PartidaId,
    string Resultado,
    DateTime? DataPartida
);

public record DashboardAtletaGrupoDto(
    Guid? GrupoId,
    string Nome,
    bool PartidasAvulsas,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    string? TipoSequenciaAtual,
    int SequenciaAtual,
    int? PosicaoRanking,
    decimal? PontosRanking,
    DashboardScoutEstatisticasPontosDto EstatisticasPontos
);

public record DashboardDuplaParceiroDto(
    Guid ParceiroId,
    string Nome,
    string? Apelido,
    string? FotoPerfilUrl,
    int Jogos,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    DateTime? UltimaPartida
);

public record DashboardAtletaJogosDto(
    IReadOnlyList<DashboardAtletaPartidaDto> Itens,
    int Total,
    int Pagina,
    int TamanhoPagina,
    bool TemMais
);

public record DashboardDuplaDto(
    DashboardDuplaDadosDto Dupla,
    DashboardDuplaResumoDto Resumo,
    IReadOnlyList<DashboardDuplaMetricaDto> Metricas,
    IReadOnlyList<DashboardDuplaPartidaDto> UltimasPartidas,
    IReadOnlyList<DashboardDuplaAdversarioDto> MelhoresAdversarios,
    IReadOnlyList<DashboardDuplaEvolucaoDto> Evolucao,
    IReadOnlyList<string> Insights,
    DashboardScoutEstatisticasPontosDto? EstatisticasPontos = null,
    IReadOnlyList<DashboardScoutResultadoRecenteDto>? FormaRecente = null,
    IReadOnlyList<DashboardAtletaGrupoDto>? Grupos = null
);

public record DashboardDuplaDadosDto(
    DashboardDuplaAtletaDto Atleta1,
    DashboardDuplaAtletaDto Atleta2,
    string Nome,
    string? CategoriaPrincipal
);

public record DashboardDuplaAtletaDto(
    Guid AtletaId,
    string Nome,
    string? Apelido
);

public record DashboardDuplaResumoDto(
    int TotalPartidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    int PontosPro,
    int PontosContra,
    int SaldoPontos,
    int MaiorSequenciaVitorias,
    int SequenciaAtual,
    int PartidasComPlacar = 0,
    string? TipoSequenciaAtual = null
);

public record DashboardDuplaMetricaDto(
    string Id,
    string Rotulo,
    string Valor,
    string? Complemento,
    string Icone,
    bool Destaque = false
);

public record DashboardDuplaPartidaDto(
    Guid Id,
    string Resultado,
    DateTime? DataPartida,
    Guid? GrupoId,
    Guid? CategoriaCompeticaoId,
    string? Grupo,
    string? Categoria,
    string? Competicao,
    string? Status,
    int? StatusAprovacao,
    int? PlacarDupla,
    int? PlacarAdversarios,
    IReadOnlyList<DashboardDuplaAtletaDto> Adversarios
);

public record DashboardDuplaAdversarioDto(
    IReadOnlyList<DashboardDuplaAtletaDto> Atletas,
    int Partidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    DateTime? UltimaPartida = null,
    int PontosPro = 0,
    int PontosContra = 0,
    int SaldoPontos = 0,
    int PartidasComPlacar = 0
);

public record DashboardDuplaEvolucaoDto(
    string Mes,
    int Ano,
    int NumeroMes,
    int Partidas,
    int Vitorias,
    int Derrotas,
    decimal Aproveitamento,
    decimal? AproveitamentoDados = null,
    bool PossuiDados = true
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
    int? PontosDupla1,
    int? PontosDupla2,
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
    int SequenciaAtual,
    string? FotoPerfilUrl
);

public record DashboardPublicoAtletaDestaqueDto(
    Guid AtletaId,
    string Nome,
    string? Apelido,
    string Destaque,
    string Valor,
    string Complemento,
    string? FotoPerfilUrl
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
