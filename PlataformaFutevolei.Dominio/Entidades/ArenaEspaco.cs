using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class ArenaEspaco : EntidadeBase
{
    public Guid ArenaId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public TipoEspaco TipoEspaco { get; set; }
    public string? Descricao { get; set; }
    public bool PossuiIluminacao { get; set; }
    public bool PossuiCobertura { get; set; }
    public bool Ativo { get; set; } = true;
    public int? OrdemExibicao { get; set; }

    public Arena Arena { get; set; } = null!;
}
