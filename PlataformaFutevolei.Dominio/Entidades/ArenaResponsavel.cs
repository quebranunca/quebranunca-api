using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ArenaResponsavel : EntidadeBase
{
    public Guid ArenaId { get; set; }
    public Guid UsuarioId { get; set; }
    public PapelArenaResponsavel Papel { get; set; } = PapelArenaResponsavel.ArenaAdmin;
    public bool Ativo { get; set; } = true;

    public Arena? Arena { get; set; }
    public Usuario? Usuario { get; set; }
}
