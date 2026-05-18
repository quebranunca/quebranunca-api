using PlataformaFutevolei.Dominio.Enums;

namespace PlataformaFutevolei.Dominio.Entidades;

public class Usuario : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public string? CodigoLoginHash { get; set; }
    public DateTime? CodigoLoginExpiraEmUtc { get; set; }
    public string? CodigoRedefinicaoSenhaHash { get; set; }
    public DateTime? CodigoRedefinicaoSenhaExpiraEmUtc { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiraEmUtc { get; set; }
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Atleta;
    public Guid? AtletaId { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime? ExcluidoEm { get; set; }
    public Guid? ExcluidoPorUsuarioId { get; set; }
    public bool DadosAnonimizados { get; set; }
    public bool PerfilPublico { get; set; } = true;
    public bool ExibirEmail { get; set; }
    public bool PermitirUsoLocalizacao { get; set; }
    public bool PermitirUsoImagem { get; set; }
    public string? FotoPerfilUrl { get; private set; }
    public string? FotoPerfilPublicId { get; private set; }
    public DateTime? ExclusaoSolicitadaEmUtc { get; set; }

    public Atleta? Atleta { get; set; }
    public Usuario? ExcluidoPorUsuario { get; set; }
    public ICollection<UsuarioConsentimentoLgpd> ConsentimentosLgpd { get; set; } = new List<UsuarioConsentimentoLgpd>();

    public void AtualizarFotoPerfil(string fotoPerfilUrl, string fotoPerfilPublicId)
    {
        FotoPerfilUrl = fotoPerfilUrl;
        FotoPerfilPublicId = fotoPerfilPublicId;
        AtualizarDataModificacao();
    }

    public void LimparFotoPerfil()
    {
        FotoPerfilUrl = null;
        FotoPerfilPublicId = null;
        AtualizarDataModificacao();
    }
}
