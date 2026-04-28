using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class Local : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public TipoLocal Tipo { get; set; }
    public int QuantidadeQuadras { get; set; }
    public Guid? UsuarioCriadorId { get; set; }

    public Usuario? UsuarioCriador { get; set; }
    public ICollection<Competicao> Competicoes { get; set; } = new List<Competicao>();
}
