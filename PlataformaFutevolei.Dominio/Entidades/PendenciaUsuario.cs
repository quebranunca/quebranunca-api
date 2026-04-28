using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class PendenciaUsuario : EntidadeBase
{
    public TipoPendenciaUsuario Tipo { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid? AtletaId { get; set; }
    public Guid? PartidaId { get; set; }
    public StatusPendenciaUsuario Status { get; set; } = StatusPendenciaUsuario.Pendente;
    public DateTime? DataConclusao { get; set; }
    public string? Observacao { get; set; }

    public Usuario Usuario { get; set; } = default!;
    public Atleta? Atleta { get; set; }
    public Partida? Partida { get; set; }
}
