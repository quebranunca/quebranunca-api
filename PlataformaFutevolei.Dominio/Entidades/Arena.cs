using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class Arena : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public TipoArena TipoArena { get; set; }
    public int QuantidadeEspacos { get; set; }
    public string? Endereco { get; set; }
    public string? EnderecoResumo { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Whatsapp { get; set; }
    public string? Instagram { get; set; }
    public string? Site { get; set; }
    public bool PossuiIluminacao { get; set; }
    public bool PossuiEstacionamento { get; set; }
    public bool PossuiVestiario { get; set; }
    public bool PossuiDucha { get; set; }
    public bool PossuiBarRestaurante { get; set; }
    public bool PossuiLoja { get; set; }
    public bool PossuiCobertura { get; set; }
    public string? LogoUrl { get; set; }
    public string? LogoPublicId { get; set; }
    public string? CapaUrl { get; set; }
    public string? CapaPublicId { get; set; }
    public bool Publica { get; set; } = true;
    public bool Ativa { get; set; } = true;

    public ICollection<ArenaResponsavel> Responsaveis { get; set; } = new List<ArenaResponsavel>();
    public ICollection<Competicao> Competicoes { get; set; } = new List<Competicao>();
    public ICollection<Grupo> Grupos { get; set; } = new List<Grupo>();
}
