namespace PlataformaFutevolei.Dominio.Entidades;

public class GrupoAtleta : EntidadeBase
{
    public Guid GrupoId { get; set; }
    public Guid AtletaId { get; set; }

    public Grupo Grupo { get; set; } = default!;
    public Atleta Atleta { get; set; } = default!;
}
