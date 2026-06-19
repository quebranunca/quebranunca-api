using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.DTOs;

public record PontuacaoBeneficioResumoDto(
    bool TemAtletaVinculado,
    Guid? AtletaId,
    int SaldoAtual,
    int TotalAcumulado,
    int TotalResgatado);

public record NivelPontuacaoBeneficioDto(
    string Nome,
    int PontosMinimos,
    int? PontosProximaFaixa,
    int ProgressoPercentual,
    int PontosRestantes);

public record GamificacaoResumoDto(
    PontuacaoBeneficioResumoDto Pontuacao,
    NivelPontuacaoBeneficioDto Nivel,
    GamificacaoResumoAtividadeDto Atividade,
    IReadOnlyList<BeneficioPontuacaoDto> ProximosBeneficios,
    IReadOnlyList<MissaoPontuacaoDto> Missoes,
    IReadOnlyList<ConquistaAtletaDto> ConquistasDestaque);

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
