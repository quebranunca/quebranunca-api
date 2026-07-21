using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Aplicacao.Configuracoes;

public static class PontuacaoBeneficioRegras
{
    public static readonly IReadOnlySet<int> PercentuaisDescontoSuportados = new HashSet<int> { 10, 20, 30 };
    public static readonly IReadOnlyDictionary<int, int?> CustosDescontosIniciais =
        new Dictionary<int, int?>
        {
            [10] = 300,
            [20] = 600,
            [30] = null
        };

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

    public static readonly IReadOnlyList<BeneficioPontuacaoPadrao> BeneficiosPadrao = ValidarBeneficiosPadrao(
    [
        new(
            Guid.Parse("11111111-1111-4111-8111-111111111111"),
            "Cupom 10% OFF",
            "Beneficio promocional para campanhas da loja QuebraNunca.",
            TipoBeneficioPontuacao.Desconto,
            CustosDescontosIniciais[10]!.Value,
            1,
            true,
            PercentualDesconto: 10),
        new(
            Guid.Parse("22222222-2222-4222-8222-222222222222"),
            "Cupom 20% OFF",
            "Condicao promocional para produtos selecionados da loja QuebraNunca.",
            TipoBeneficioPontuacao.Desconto,
            CustosDescontosIniciais[20]!.Value,
            2,
            false,
            null,
            100,
            20),
        new(
            Guid.Parse("66666666-6666-4666-8666-666666666666"),
            "Chaveiro QuebraNunca",
            "Brinde fisico QuebraNunca disponivel por campanha.",
            TipoBeneficioPontuacao.Produto,
            700,
            3,
            false,
            "pontos-qn/beneficio-chaveiro-qn.png",
            100),
        new(
            Guid.Parse("33333333-3333-4333-8333-333333333333"),
            "Campanha promocional QuebraNunca",
            "Condicao promocional interna para campanhas QuebraNunca, sujeita a disponibilidade, regras da campanha e validacao.",
            TipoBeneficioPontuacao.Desconto,
            2000,
            4,
            false),
        new(
            Guid.Parse("44444444-4444-4444-8444-444444444444"),
            "Beneficio promocional da comunidade",
            "Beneficio interno para participantes da comunidade QuebraNunca, sujeito a disponibilidade e regras da campanha.",
            TipoBeneficioPontuacao.Desconto,
            3000,
            5,
            false),
        new(
            Guid.Parse("55555555-5555-4555-8555-555555555555"),
            "Condicao especial QuebraNunca",
            "Condicao promocional interna para campanhas selecionadas QuebraNunca, sujeita a disponibilidade e validacao.",
            TipoBeneficioPontuacao.Desconto,
            5000,
            6,
            false),
        new(
            Guid.Parse("77777777-7777-4777-8777-777777777777"),
            "Boné QuebraNunca",
            "Produto especial disponivel em campanhas da comunidade.",
            TipoBeneficioPontuacao.Produto,
            1500,
            7,
            false,
            "pontos-qn/beneficio-bone-qn.png",
            50)
    ]);

    public static readonly IReadOnlyList<FaixaPontuacaoBeneficio> Faixas =
    [
        new("Bronze", 0, 500),
        new("Prata", 500, 1500),
        new("Ouro", 1500, 4000),
        new("Diamante", 4000, 8000),
        new("Lenda QN", 8000, null)
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
            TipoBeneficioPontuacao.Desconto when pontosNecessarios >= 5000 => "Condicao especial QuebraNunca",
            TipoBeneficioPontuacao.Desconto when pontosNecessarios >= 3000 => "Beneficio promocional da comunidade",
            TipoBeneficioPontuacao.Desconto when pontosNecessarios >= 2000 => "Desconto promocional em campanha",
            TipoBeneficioPontuacao.Desconto when pontosNecessarios >= 1000 => "Condicao especial em produto QN",
            TipoBeneficioPontuacao.Desconto => "Cupom especial QuebraNunca",
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

    private static IReadOnlyList<BeneficioPontuacaoPadrao> ValidarBeneficiosPadrao(
        IReadOnlyList<BeneficioPontuacaoPadrao> beneficios)
    {
        foreach (var beneficio in beneficios)
        {
            if (beneficio.PercentualDesconto is <= 0 or > BeneficioPontuacao.PercentualDescontoMaximo)
            {
                throw new InvalidOperationException(
                    $"Benefício padrão '{beneficio.Titulo}' possui percentual de desconto inválido.");
            }

            if (beneficio.PercentualDesconto.HasValue && beneficio.Tipo != TipoBeneficioPontuacao.Desconto)
            {
                throw new InvalidOperationException(
                    $"Benefício padrão '{beneficio.Titulo}' possui percentual, mas não é do tipo Desconto.");
            }
        }

        return beneficios;
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
    string? ImagemUrl = null,
    int? QuantidadeDisponivel = null,
    int? PercentualDesconto = null);
