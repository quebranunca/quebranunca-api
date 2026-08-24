using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class NotificacaoUsuario : EntidadeBase
{
    public Guid UsuarioId { get; set; }
    public string Origem { get; set; } = string.Empty;
    public string ChaveIdempotencia { get; set; } = string.Empty;
    public TipoNotificacaoUsuario Tipo { get; set; } = TipoNotificacaoUsuario.Informativa;
    public PrioridadePendenciaUsuario Prioridade { get; set; } = PrioridadePendenciaUsuario.Baixa;
    public string Titulo { get; set; } = string.Empty;
    public string Mensagem { get; set; } = string.Empty;
    public string? LinkAcao { get; set; }
    public string? TextoAcao { get; set; }
    public string? ReferenciaTipo { get; set; }
    public string? ReferenciaId { get; set; }
    public DateTime? LidaEmUtc { get; set; }
    public DateTime? ArquivadaEmUtc { get; set; }

    public Usuario Usuario { get; set; } = default!;
}
