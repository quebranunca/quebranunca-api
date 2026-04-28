namespace PlataformaFutevolei.Dominio.Entidades;

public class Liga : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public ICollection<Competicao> Competicoes { get; set; } = new List<Competicao>();
}
