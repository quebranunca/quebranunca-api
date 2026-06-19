using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class BeneficioPontuacao : EntidadeBase
{
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public TipoBeneficioPontuacao Tipo { get; set; }
    public int PontosNecessarios { get; set; }
    public bool Ativo { get; set; } = true;
    public int? QuantidadeDisponivel { get; set; }
    public string? ImagemUrl { get; set; }
    public int Ordem { get; set; }
    public bool Destaque { get; set; }

    public ICollection<ResgateBeneficioPontuacao> Resgates { get; set; } = new List<ResgateBeneficioPontuacao>();
}
