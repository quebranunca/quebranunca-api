namespace PlataformaFutevolei.Dominio.Entidades;

public class PontuacaoBeneficioAtleta : EntidadeBase
{
    public Guid AtletaId { get; set; }
    public int SaldoAtual { get; set; }
    public int TotalAcumulado { get; set; }
    public int TotalResgatado { get; set; }

    public Atleta Atleta { get; set; } = default!;
}
