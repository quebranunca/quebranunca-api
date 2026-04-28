namespace PlataformaFutevolei.Dominio.Entidades;

public class GrupoAtleta : EntidadeBase
{
    public Guid CompeticaoId { get; set; }
    public Guid AtletaId { get; set; }

    public Competicao Competicao { get; set; } = default!;
    public Atleta Atleta { get; set; } = default!;
}
