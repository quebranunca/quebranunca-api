using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class PartidaAprovacao : EntidadeBase
{
    public Guid PartidaId { get; set; }
    public Guid AtletaId { get; set; }
    public Guid UsuarioId { get; set; }
    public StatusPartidaAprovacao Status { get; set; } = StatusPartidaAprovacao.Pendente;
    public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataResposta { get; set; }
    public string? Observacao { get; set; }

    public Partida Partida { get; set; } = default!;
    public Atleta Atleta { get; set; } = default!;
    public Usuario Usuario { get; set; } = default!;
}
