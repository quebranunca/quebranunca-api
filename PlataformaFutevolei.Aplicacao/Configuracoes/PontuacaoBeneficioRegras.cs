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

    private static readonly IReadOnlyList<string> TermosCopyFinanceiraIndevida =
    [
        "100 QN",
        "R$",
        "cashback",
        "dinheiro",
        "saldo financeiro",
        "carteira",
        "conversao",
        "conversão",
        "credito financeiro",
        "crédito financeiro",
        "equivale",
        "reais"
    ];

    public static readonly IReadOnlyList<BeneficioPontuacaoPadrao> BeneficiosPadrao =
    [
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "Cupom especial QuebraNunca",
            "Beneficio promocional interno para campanhas QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao.",
            TipoBeneficioPontuacao.DescontoLoja,
            500,
            1,
            true),
        new(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "Condicao especial em produto QN",
            "Beneficio promocional interno em produto de campanha QuebraNunca, sujeito a disponibilidade e validacao.",
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
            "Desconto promocional em campanha",
            "Condicao promocional interna para campanhas QuebraNunca, sujeita a disponibilidade, regras da campanha e validacao.",
            TipoBeneficioPontuacao.DescontoLoja,
            2000,
            4,
            false),
        new(
            Guid.Parse("44444444-4444-4444-8444-444444444444"),
            "Beneficio promocional da comunidade",
            "Beneficio interno para participantes da comunidade QuebraNunca, sujeito a disponibilidade e regras da campanha.",
            TipoBeneficioPontuacao.DescontoLoja,
            3000,
            5,
            false),
        new(
            Guid.Parse("55555555-5555-4555-8555-555555555555"),
            "Condicao especial QuebraNunca",
            "Condicao promocional interna para campanhas selecionadas QuebraNunca, sujeita a disponibilidade e validacao.",
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

    public static bool ContemCopyFinanceiraIndevida(string? texto)
        => !string.IsNullOrWhiteSpace(texto) &&
            TermosCopyFinanceiraIndevida.Any(termo => texto.Contains(termo, StringComparison.OrdinalIgnoreCase));

    public static string ObterTituloBeneficioPublicoSeguro(TipoBeneficioPontuacao tipo, int pontosNecessarios, string? titulo)
    {
        if (!string.IsNullOrWhiteSpace(titulo) && !ContemCopyFinanceiraIndevida(titulo))
        {
            return titulo;
        }

        return tipo switch
        {
            TipoBeneficioPontuacao.DescontoLoja when pontosNecessarios >= 5000 => "Condicao especial QuebraNunca",
            TipoBeneficioPontuacao.DescontoLoja when pontosNecessarios >= 3000 => "Beneficio promocional da comunidade",
            TipoBeneficioPontuacao.DescontoLoja when pontosNecessarios >= 2000 => "Desconto promocional em campanha",
            TipoBeneficioPontuacao.DescontoLoja when pontosNecessarios >= 1000 => "Condicao especial em produto QN",
            TipoBeneficioPontuacao.DescontoLoja => "Cupom especial QuebraNunca",
            TipoBeneficioPontuacao.Brinde => "Brinde QuebraNunca",
            TipoBeneficioPontuacao.Experiencia => "Experiencia QuebraNunca",
            TipoBeneficioPontuacao.Produto => "Produto QuebraNunca em campanha",
            _ => "Beneficio promocional QuebraNunca"
        };
    }

    public static string ObterDescricaoBeneficioPublicaSegura(TipoBeneficioPontuacao tipo, string? descricao)
    {
        if (!string.IsNullOrWhiteSpace(descricao) && !ContemCopyFinanceiraIndevida(descricao))
        {
            return descricao;
        }

        return tipo switch
        {
            TipoBeneficioPontuacao.Produto => "Produto promocional QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao.",
            TipoBeneficioPontuacao.Brinde => "Brinde promocional QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao.",
            TipoBeneficioPontuacao.Experiencia => "Experiencia promocional QuebraNunca, sujeita a disponibilidade, regras da campanha e validacao.",
            _ => "Beneficio promocional interno para campanhas QuebraNunca, sujeito a disponibilidade, regras da campanha e validacao."
        };
    }
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
