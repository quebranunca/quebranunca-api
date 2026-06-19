using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Configuracoes;

public static class PontuacaoBeneficioRegras
{
    public const int PerfilCompleto = 20;
    public const int PartidaParticipante = 5;
    public const int PartidaRegistrador = 10;
    public const int PartidaPlacarCompleto = 3;
    public const int Compartilhamento = 3;
    public const int PendenciaResolvida = 10;
    public const int SequenciaSemanal = 15;
    public const int ConviteAtletaPrimeiraPartida = 20;

    public const int LimiteCompartilhamentoDiarioPorTipo = 3;
    public const int LimiteCompartilhamentoSemanalTotal = 12;

    public static readonly IReadOnlyList<FaixaPontuacaoBeneficio> Faixas =
    [
        new("Bronze", 0, 500),
        new("Prata", 500, 1000),
        new("Ouro", 1000, 2000),
        new("Diamante", 2000, 4000),
        new("Lenda QN", 4000, null)
    ];

    public static TipoEventoPontuacaoBeneficio ObterEventoCompartilhamento(TipoCompartilhamentoGamificacao tipo)
        => tipo switch
        {
            TipoCompartilhamentoGamificacao.Partida => TipoEventoPontuacaoBeneficio.CompartilhamentoPartida,
            TipoCompartilhamentoGamificacao.Ranking => TipoEventoPontuacaoBeneficio.CompartilhamentoRanking,
            TipoCompartilhamentoGamificacao.ScoutAtleta => TipoEventoPontuacaoBeneficio.CompartilhamentoScoutAtleta,
            TipoCompartilhamentoGamificacao.ScoutDupla => TipoEventoPontuacaoBeneficio.CompartilhamentoScoutDupla,
            _ => TipoEventoPontuacaoBeneficio.CompartilhamentoPartida
        };

    public static bool EhEventoCompartilhamento(TipoEventoPontuacaoBeneficio tipo)
        => tipo is TipoEventoPontuacaoBeneficio.CompartilhamentoPartida
            or TipoEventoPontuacaoBeneficio.CompartilhamentoRanking
            or TipoEventoPontuacaoBeneficio.CompartilhamentoScoutAtleta
            or TipoEventoPontuacaoBeneficio.CompartilhamentoScoutDupla;
}

public record FaixaPontuacaoBeneficio(string Nome, int PontosMinimos, int? PontosProximaFaixa);
