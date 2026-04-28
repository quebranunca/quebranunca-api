namespace PlataformaFutevolei.Dominio.Entidades;

public class Dupla : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public Guid Atleta1Id { get; set; }
    public Guid Atleta2Id { get; set; }

    public Atleta Atleta1 { get; set; } = default!;
    public Atleta Atleta2 { get; set; } = default!;

    public ICollection<Partida> PartidasComoDuplaA { get; set; } = new List<Partida>();
    public ICollection<Partida> PartidasComoDuplaB { get; set; } = new List<Partida>();
    public ICollection<Partida> PartidasVencidas { get; set; } = new List<Partida>();
}
