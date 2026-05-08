namespace PlataformaFutevolei.Dominio.Entidades;

public class Grupo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Link { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public Guid? LocalId { get; set; }
    public Guid? UsuarioOrganizadorId { get; set; }

    public Local? Local { get; set; }
    public Usuario? UsuarioOrganizador { get; set; }
    public ICollection<GrupoAtleta> Atletas { get; set; } = new List<GrupoAtleta>();
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}
