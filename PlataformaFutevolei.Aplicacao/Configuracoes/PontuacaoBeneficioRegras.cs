using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Configuracoes;

public static class PontuacaoBeneficioRegras
{
    public const int QnPorRealReferenciaDesconto = 100;
    public const decimal ValorReferenciaInternaPorQn = 0.01m;
    public const int LimitePercentualCupomPedido = 30;

    public const int PerfilCompleto = 50;
    public const int EntradaGrupo = 20;
    public const int PartidaParticipante = 10;
    public const int PartidaRegistrador = 5;
    public const int PartidaPlacarCompleto = 5;
    public const int PartidaVitoria = 3;
    public const int ConfirmacaoAprovacaoPartida = 2;
    public const int Compartilhamento = 5;
    public const int PendenciaResolvida = 10;
    public const int SequenciaSemanal = 30;
    public const int FrequenciaCincoPartidasSemana = 60;
    public const int SequenciaQuatroSemanas = 150;
    public const int ConviteAtletaCadastro = 100;
    public const int ConviteAtletaPrimeiraPartida = 100;

    public const int LimiteCompartilhamentoDiarioPorTipo = 3;
    public const int LimiteCompartilhamentoSemanalTotal = 12;

    public static readonly IReadOnlyList<BeneficioPontuacaoPadrao> BeneficiosPadrao =
    [
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "R$ 5 off na loja",
            "Cupom manual de R$ 5 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.",
            TipoBeneficioPontuacao.DescontoLoja,
            500,
            1,
            true),
        new(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "R$ 10 off na loja",
            "Cupom manual de R$ 10 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.",
            TipoBeneficioPontuacao.DescontoLoja,
            1000,
            2,
            false),
        new(
            Guid.Parse("66666666-6666-4666-8666-666666666666"),
            "Chaveiro QuebraNunca",
            "Chaveiro exclusivo QuebraNunca para levar a resenha com voce.",
            TipoBeneficioPontuacao.Produto,
            2000,
            3,
            false,
            "pontos-qn/beneficio-chaveiro-qn.png"),
        new(
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "R$ 20 off na loja",
            "Cupom manual de R$ 20 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.",
            TipoBeneficioPontuacao.DescontoLoja,
            2000,
            4,
            false),
        new(
            Guid.Parse("44444444-4444-4444-8444-444444444444"),
            "R$ 30 off na loja",
            "Cupom manual de R$ 30 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.",
            TipoBeneficioPontuacao.DescontoLoja,
            3000,
            5,
            false),
        new(
            Guid.Parse("55555555-5555-4555-8555-555555555555"),
            "R$ 50 off na loja",
            "Cupom manual de R$ 50 off para campanhas QuebraNunca. Pode cobrir ate 30% do pedido e nao inclui frete, salvo campanha especifica.",
            TipoBeneficioPontuacao.DescontoLoja,
            5000,
            6,
            false),
        new(
            Guid.Parse("77777777-7777-4777-8777-777777777777"),
            "Boné QuebraNunca",
            "Boné trucker QuebraNunca para usar dentro e fora da areia.",
            TipoBeneficioPontuacao.Produto,
            8000,
            7,
            false,
            "pontos-qn/beneficio-bone-qn.png")
    ];

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

public record BeneficioPontuacaoPadrao(
    Guid Id,
    string Titulo,
    string Descricao,
    TipoBeneficioPontuacao Tipo,
    int PontosNecessarios,
    int Ordem,
    bool Destaque,
    string? ImagemUrl = null);
