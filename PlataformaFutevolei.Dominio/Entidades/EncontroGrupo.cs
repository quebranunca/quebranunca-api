namespace PlataformaFutevolei.Dominio.Entidades;

public class EncontroGrupo : EntidadeBase
{
    public Guid GrupoId { get; set; }
    public DateOnly DataJogo { get; set; }
    public TimeOnly HorarioInicio { get; set; }
    public TimeOnly HorarioFim { get; set; }
    public Guid? ArenaId { get; set; }
    public string? LocalSnapshot { get; set; }

    public Grupo Grupo { get; set; } = default!;
    public Arena? Arena { get; set; }
    public ICollection<ConfirmacaoPresencaGrupo> Confirmacoes { get; set; } = new List<ConfirmacaoPresencaGrupo>();
}
