using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class BeneficioPontuacao : EntidadeBase
{
    public const int PercentualDescontoMaximo = 30;
    private int? percentualDesconto;

    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoBeneficioPontuacao Tipo { get; set; }
    public int PontosNecessarios { get; set; }
    public int? PercentualDesconto
    {
        get => percentualDesconto;
        set
        {
            if (value is <= 0 or > PercentualDescontoMaximo)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(PercentualDesconto),
                    value,
                    $"O percentual de desconto deve ser maior que 0 e menor ou igual a {PercentualDescontoMaximo}.");
            }

            percentualDesconto = value;
        }
    }
    public bool Ativo { get; set; } = true;
    public int? QuantidadeDisponivel { get; set; }
    public string? ImagemUrl { get; set; }
    public int Ordem { get; set; }
    public bool Destaque { get; set; }

    public ICollection<ResgateBeneficioPontuacao> Resgates { get; set; } = new List<ResgateBeneficioPontuacao>();
}
