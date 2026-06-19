using Microsoft.EntityFrameworkCore;
using PlataformaFutevolei.Dominio.Entidades;

namespace PlataformaFutevolei.Infraestrutura.Persistencia;

public class PlataformaFutevoleiDbContext(DbContextOptions<PlataformaFutevoleiDbContext> options)
    : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<UsuarioConsentimentoLgpd> UsuariosConsentimentosLgpd => Set<UsuarioConsentimentoLgpd>();
    public DbSet<ConviteCadastro> ConvitesCadastro => Set<ConviteCadastro>();
    public DbSet<SolicitacaoAcesso> SolicitacoesAcesso => Set<SolicitacaoAcesso>();
    public DbSet<Atleta> Atletas => Set<Atleta>();
    public DbSet<AtletaMedidas> AtletasMedidas => Set<AtletaMedidas>();
    public DbSet<Dupla> Duplas => Set<Dupla>();
    public DbSet<Liga> Ligas => Set<Liga>();
    public DbSet<Arena> Arenas => Set<Arena>();
    public DbSet<ArenaEspaco> ArenaEspacos => Set<ArenaEspaco>();
    public DbSet<ArenaResponsavel> ArenaResponsaveis => Set<ArenaResponsavel>();
    public DbSet<FormatoCampeonato> FormatosCampeonato => Set<FormatoCampeonato>();
    public DbSet<RegraCompeticao> RegrasCompeticao => Set<RegraCompeticao>();
    public DbSet<Competicao> Competicoes => Set<Competicao>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<GrupoAtleta> GruposAtletas => Set<GrupoAtleta>();
    public DbSet<CategoriaCompeticao> CategoriasCompeticao => Set<CategoriaCompeticao>();
    public DbSet<InscricaoCampeonato> InscricoesCampeonato => Set<InscricaoCampeonato>();
    public DbSet<Partida> Partidas => Set<Partida>();
    public DbSet<PartidaAprovacao> PartidasAprovacoes => Set<PartidaAprovacao>();
    public DbSet<PendenciaUsuario> PendenciasUsuarios => Set<PendenciaUsuario>();
    public DbSet<PontuacaoBeneficioAtleta> PontuacoesBeneficiosAtletas => Set<PontuacaoBeneficioAtleta>();
    public DbSet<ExtratoPontuacaoBeneficio> ExtratosPontuacaoBeneficio => Set<ExtratoPontuacaoBeneficio>();
    public DbSet<BeneficioPontuacao> BeneficiosPontuacao => Set<BeneficioPontuacao>();
    public DbSet<ResgateBeneficioPontuacao> ResgatesBeneficiosPontuacao => Set<ResgateBeneficioPontuacao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlataformaFutevoleiDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
