using PlataformaFutevolei.Dominio.Enums;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record PontuacaoBeneficioResumoDto(
    bool TemAtletaVinculado,
    Guid? AtletaId,
    int SaldoAtual,
    int PontosDisponiveis,
    int PontosPendentesCompensacao,
    int TotalAcumulado,
    int TotalResgatado);

public record ReconciliacaoPontosQNAnomaliaDto(
    string Codigo,
    string Mensagem,
    bool Bloqueante,
    string Entidade,
    Guid? EntidadeId);

public record ReconciliacaoPontosQNAtletaDto(
    Guid AtletaId,
    string NomeAtleta,
    int? SaldoAtualPersistido,
    int SaldoAtualCalculado,
    int? TotalAcumuladoPersistido,
    int TotalAcumuladoCalculado,
    int? TotalResgatadoPersistido,
    int TotalResgatadoCalculado,
    int PontosDisponiveisCalculados,
    int PontosPendentesCompensacaoCalculados,
    bool PossuiDivergencia,
    bool Bloqueada,
    int EstornosPartidaPendentes,
    bool Corrigida,
    IReadOnlyList<ReconciliacaoPontosQNAnomaliaDto> Anomalias);

public record ReconciliacaoPontosQNResultadoDto(
    bool DryRun,
    bool Aplicado,
    DateTime ExecutadoEmUtc,
    Guid? AtletaIdFiltrado,
    int AtletasAvaliados,
    int AtletasConsistentes,
    int AtletasComDivergencia,
    int AtletasCorrigidos,
    int AtletasBloqueados,
    int ProjecoesCriadas,
    int ProjecoesAtualizadas,
    int EstornosPartidaPendentes,
    int EstornosPartidaCriados,
    int TotalAnomalias,
    IReadOnlyList<ReconciliacaoPontosQNAtletaDto> Detalhes,
    IReadOnlyList<string> Avisos);

public record ReconciliacaoPontosQNCandidatoDto(Guid AtletaId, string NomeAtleta);

public record ReconciliacaoPontosQNDadosDto(
    Atleta Atleta,
    PontuacaoBeneficioAtleta? Saldo,
    IReadOnlyList<ExtratoPontuacaoBeneficio> Extratos,
    IReadOnlyList<ResgateBeneficioPontuacao> Resgates,
    IReadOnlyList<Partida> Partidas);

public record NivelPontuacaoBeneficioDto(
    string Nome,
    int PontosMinimos,
    int? PontosProximaFaixa,
    int ProgressoPercentual,
    int PontosRestantes);

public record FaixaPontuacaoBeneficioDto(
    string Nome,
    int PontosMinimos,
    int? PontosProximaFaixa);

public record GamificacaoResumoDto(
    PontuacaoBeneficioResumoDto Pontuacao,
    NivelPontuacaoBeneficioDto Nivel,
    GamificacaoResumoAtividadeDto Atividade,
    IReadOnlyList<BeneficioPontuacaoDto> ProximosBeneficios,
    IReadOnlyList<MissaoPontuacaoDto> Missoes,
    IReadOnlyList<ConquistaAtletaDto> ConquistasDestaque,
    IReadOnlyList<FaixaPontuacaoBeneficioDto> FaixasMedalha);

public record GamificacaoResumoAtividadeDto(
    int PartidasNoMes,
    int CompartilhamentosNaSemana,
    int SequenciaSemanal,
    int? PosicaoRanking);

public record ExtratoPontuacaoBeneficioDto(
    Guid Id,
    Guid AtletaId,
    Guid? GrupoId,
    Guid? PartidaId,
    Guid? ResgateId,
    TipoEventoPontuacaoBeneficio TipoEvento,
    string TipoEventoNome,
    int Pontos,
    string Descricao,
    string Origem,
    DateTime CriadoEm);

public record ExtratoPontuacaoBeneficioListaDto(
    int Pagina,
    int QuantidadePorPagina,
    IReadOnlyList<ExtratoPontuacaoBeneficioDto> Itens);

public record BeneficioPontuacaoDto(
    Guid Id,
    string Titulo,
    string Descricao,
    TipoBeneficioPontuacao Tipo,
    string TipoNome,
    int? PercentualDesconto,
    int PontosNecessarios,
    bool Ativo,
    int? QuantidadeDisponivel,
    string? ImagemUrl,
    int Ordem,
    bool Destaque,
    bool SaldoSuficiente,
    int PontosFaltantes);

public record SolicitarResgateBeneficioDto(
    string? ObservacaoAtleta);

public record AtualizarStatusResgateBeneficioDto(
    string? ObservacaoAdmin,
    string? CodigoCupom);

public record ResgateBeneficioPontuacaoDto(
    Guid Id,
    Guid AtletaId,
    Guid BeneficioId,
    string BeneficioTitulo,
    TipoBeneficioPontuacao BeneficioTipo,
    int PontosUtilizados,
    StatusResgateBeneficioPontuacao Status,
    string StatusNome,
    string? CodigoCupom,
    string? ObservacaoAtleta,
    string? ObservacaoAdmin,
    DateTime SolicitadoEm,
    DateTime? AprovadoEm,
    DateTime? RejeitadoEm,
    DateTime? CanceladoEm,
    DateTime? UtilizadoEm);

public record MissaoPontuacaoDto(
    string Codigo,
    string Titulo,
    string Descricao,
    int ProgressoAtual,
    int Meta,
    int PontosRecompensa,
    bool Concluida,
    DateTime InicioEm,
    DateTime TerminaEm);

public record ConquistaAtletaDto(
    string Codigo,
    string Titulo,
    string Descricao,
    bool Desbloqueada,
    int ProgressoAtual,
    int Meta);

public record RegistrarCompartilhamentoGamificacaoDto(
    TipoCompartilhamentoGamificacao Tipo,
    Guid? PartidaId,
    Guid? GrupoId,
    Guid? AtletaId,
    Guid? DuplaId,
    string? Origem);

public record SaldoInicialRetroativoAtletaDto(
    Guid AtletaId,
    string NomeAtleta,
    int PartidasParticipadas,
    int PartidasRegistradas,
    int PartidasComPlacar,
    int Vitorias,
    int Grupos,
    int PendenciasResolvidas,
    bool PerfilCompleto,
    int TotalCalculado,
    bool JaPossuiaSaldoInicial);

public record RecalculoSaldoInicialPontuacaoResultadoDto(
    bool DryRun,
    bool Aplicado,
    int AtletasAvaliados,
    int AtletasComSaldoInicialCalculado,
    int AtletasIgnoradosPorSaldoInicialExistente,
    int AtletasComPerfilCompleto,
    int TotalPontosCalculados,
    IReadOnlyList<SaldoInicialRetroativoAtletaDto> TopSaldosCalculados,
    IReadOnlyList<string> Avisos);
