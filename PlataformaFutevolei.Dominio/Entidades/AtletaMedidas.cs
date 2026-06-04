namespace PlataformaFutevolei.Dominio.Entidades;

public class AtletaMedidas : EntidadeBase
{
    public Guid AtletaId { get; set; }
    public string? Camiseta { get; set; }
    public string? Regata { get; set; }
    public string? Short { get; set; }
    public string? Sunga { get; set; }
    public string? Top { get; set; }
    public string? Biquini { get; set; }
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

    public Atleta Atleta { get; set; } = null!;
}
