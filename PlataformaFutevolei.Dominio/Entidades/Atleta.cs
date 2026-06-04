using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class Atleta : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Apelido { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Instagram { get; set; }
    public string? Cpf { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public bool CadastroPendente { get; set; }
    public SexoAtleta? Sexo { get; set; }
    public NivelAtleta? Nivel { get; set; }
    public LadoAtleta Lado { get; set; } = LadoAtleta.Ambos;
    public DateTime? DataNascimento { get; set; }
    public Guid? UsuarioCriadorId { get; set; }
    public PeDominanteAtleta? PeDominante { get; set; }
    public TempoPraticaAtleta? TempoPratica { get; set; }
    public Guid? ArenaPrincipalId { get; set; }
    public ObjetivoAtualAtleta? ObjetivoAtual { get; set; }

    public Usuario? Usuario { get; set; }
    public Usuario? UsuarioCriador { get; set; }
    public Arena? ArenaPrincipal { get; set; }
    public AtletaMedidas? Medidas { get; set; }
    public ICollection<Dupla> DuplasComoAtleta1 { get; set; } = new List<Dupla>();
    public ICollection<Dupla> DuplasComoAtleta2 { get; set; } = new List<Dupla>();
    public ICollection<GrupoAtleta> Grupos { get; set; } = new List<GrupoAtleta>();
}
