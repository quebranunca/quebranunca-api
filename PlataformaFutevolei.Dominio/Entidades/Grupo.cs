namespace PlataformaFutevolei.Dominio.Entidades;

public class Grupo : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public string? Link { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public Guid? ArenaId { get; set; }
    public string? LocalPrincipal { get; set; }
    public string[]? DiasDaSemana { get; set; }
    public Guid? UsuarioOrganizadorId { get; set; }
    public bool Publico { get; set; }
    public string? ImagemUrl { get; set; }
    public string? ImagemPublicId { get; set; }
    public bool Ativo { get; set; } = true;

    public Arena? Arena { get; set; }
    public Usuario? UsuarioOrganizador { get; set; }
    public ICollection<GrupoAtleta> Atletas { get; set; } = new List<GrupoAtleta>();
    public ICollection<Partida> Partidas { get; set; } = new List<Partida>();
}
