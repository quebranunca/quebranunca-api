using PlataformaFutevolei.Aplicacao.Configuracoes;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class MassaTesteAiServicoTests
{
    [Fact]
    public async Task GarantirAsync_Desabilitada_NaoCriaDados()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.GarantirAsync(new MassaTesteAiConfiguracao
        {
            Habilitada = false
        });

        Assert.False(resultado.Executada);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Atletas.Itens);
        Assert.Empty(cenario.Grupos.Itens);
        Assert.Empty(cenario.Arenas.Itens);
        Assert.Empty(cenario.GruposAtletas.Itens);
    }

    [Fact]
    public async Task GarantirAsync_HabilitadaSemSenha_FalhaSemCriarDados()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.GarantirAsync(CriarConfiguracao(senha: " ")));

        Assert.Equal("Senha do usuário principal da massa AI não configurada.", excecao.Message);
        Assert.Empty(cenario.Usuarios.Itens);
        Assert.Empty(cenario.Atletas.Itens);
    }

    [Fact]
    public async Task GarantirAsync_CriaMassaBaseComUsuarioAtletaArenaGrupoEVinculos()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.GarantirAsync(CriarConfiguracao());

        Assert.True(resultado.Executada);
        var usuario = Assert.Single(cenario.Usuarios.Itens);
        Assert.Equal(MassaTesteAiServico.NomeUsuarioPrincipal, usuario.Nome);
        Assert.Equal("gustavodrager+qnf-ai-tester@gmail.com", usuario.Email);
        Assert.Equal(PerfilUsuario.Atleta, usuario.Perfil);
        Assert.True(usuario.Ativo);
        Assert.NotNull(usuario.AtletaId);
        Assert.NotEqual("SenhaForteLocal123", usuario.SenhaHash);
        Assert.Equal("hash:SenhaForteLocal123", usuario.SenhaHash);
        Assert.NotNull(usuario.SenhaDefinidaEmUtc);

        Assert.Equal(5, cenario.Atletas.Itens.Count);
        Assert.Contains(cenario.Atletas.Itens, x => x.Id == usuario.AtletaId && x.Email == usuario.Email);
        foreach (var nome in NomesAtletasAuxiliares())
        {
            Assert.Contains(cenario.Atletas.Itens, x => x.Nome == nome && x.Email is null);
        }

        var arena = Assert.Single(cenario.Arenas.Itens);
        Assert.Equal(MassaTesteAiServico.NomeArenaBase, arena.Nome);
        Assert.Equal("ai-teste-arena-base", arena.Slug);
        Assert.False(arena.Publica);
        Assert.True(arena.Ativa);

        var grupo = Assert.Single(cenario.Grupos.Itens);
        Assert.Equal(MassaTesteAiServico.NomeGrupoBase, grupo.Nome);
        Assert.Equal(arena.Id, grupo.ArenaId);
        Assert.Equal(usuario.Id, grupo.UsuarioOrganizadorId);
        Assert.False(grupo.Publico);

        Assert.Equal(5, cenario.GruposAtletas.Itens.Select(x => x.AtletaId).Distinct().Count());
        Assert.Equal(0, cenario.CompeticoesCriadas);
        Assert.Equal(0, cenario.PartidasCriadas);
        Assert.Equal(4, resultado.AtletasAuxiliares);
        Assert.Equal(5, resultado.VinculosGrupo);
    }

    [Fact]
    public async Task GarantirAsync_DuasExecucoes_NaoDuplicaDados()
    {
        var cenario = new Cenario();
        var configuracao = CriarConfiguracao();

        await cenario.Servico.GarantirAsync(configuracao);
        await cenario.Servico.GarantirAsync(configuracao);

        Assert.Single(cenario.Usuarios.Itens);
        Assert.Equal(5, cenario.Atletas.Itens.Count);
        Assert.Single(cenario.Arenas.Itens);
        Assert.Single(cenario.Grupos.Itens);
        Assert.Equal(5, cenario.GruposAtletas.Itens.Count);
    }

    [Fact]
    public async Task GarantirAsync_MassaParcial_CompletaSemDuplicar()
    {
        var cenario = new Cenario();
        var usuario = new Usuario
        {
            Nome = MassaTesteAiServico.NomeUsuarioPrincipal,
            Email = "gustavodrager+qnf-ai-tester@gmail.com",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            SenhaHash = "hash-antigo"
        };
        var atleta = new Atleta
        {
            Nome = MassaTesteAiServico.NomeUsuarioPrincipal,
            Email = usuario.Email,
            CadastroPendente = true
        };
        usuario.AtletaId = atleta.Id;
        cenario.Usuarios.Itens.Add(usuario);
        cenario.Atletas.Itens.Add(atleta);
        cenario.Arenas.Itens.Add(new Arena
        {
            Nome = MassaTesteAiServico.NomeArenaBase,
            Slug = "ai-teste-arena-base",
            TipoArena = TipoArena.ArenaPrivada,
            QuantidadeEspacos = 1
        });

        await cenario.Servico.GarantirAsync(CriarConfiguracao());

        Assert.Single(cenario.Usuarios.Itens);
        Assert.Equal(5, cenario.Atletas.Itens.Count);
        Assert.Single(cenario.Arenas.Itens);
        Assert.Single(cenario.Grupos.Itens);
        Assert.Equal(5, cenario.GruposAtletas.Itens.Count);
        Assert.False(atleta.CadastroPendente);
        Assert.Equal("hash:SenhaForteLocal123", usuario.SenhaHash);
    }

    private static MassaTesteAiConfiguracao CriarConfiguracao(string senha = "SenhaForteLocal123")
        => new()
        {
            Habilitada = true,
            EmailUsuarioPrincipal = "gustavodrager+qnf-ai-tester@gmail.com",
            SenhaUsuarioPrincipal = senha
        };

    private static IReadOnlyList<string> NomesAtletasAuxiliares()
        =>
        [
            "[AI TESTE] Atleta 01",
            "[AI TESTE] Atleta 02",
            "[AI TESTE] Atleta 03",
            "[AI TESTE] Atleta 04"
        ];

    private sealed class Cenario
    {
        public UsuarioRepositorioMemoria Usuarios { get; } = new();
        public AtletaRepositorioMemoria Atletas { get; } = new();
        public ArenaRepositorioMemoria Arenas { get; } = new();
        public GrupoRepositorioMemoria Grupos { get; } = new();
        public GrupoAtletaRepositorioMemoria GruposAtletas { get; } = new();
        public UnidadeTrabalhoMemoria UnidadeTrabalho { get; } = new();
        public SenhaServicoStub Senhas { get; } = new();

        public int CompeticoesCriadas { get; }
        public int PartidasCriadas { get; }

        public MassaTesteAiServico Servico { get; }

        public Cenario()
        {
            Servico = new MassaTesteAiServico(
                Usuarios,
                Atletas,
                Arenas,
                Grupos,
                GruposAtletas,
                UnidadeTrabalho,
                Senhas);
        }
    }

    private sealed class UnidadeTrabalhoMemoria : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecutarEmTransacaoAsync(
            Func<CancellationToken, Task> operacao,
            CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class SenhaServicoStub : ISenhaServico
    {
        public string GerarHash(string senha) => $"hash:{senha}";

        public bool Verificar(string senha, string hash) => hash == GerarHash(senha);
    }

    private sealed class UsuarioRepositorioMemoria : IUsuarioRepositorio
    {
        public List<Usuario> Itens { get; } = [];

        public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Usuario>>(Itens);

        public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

        public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)));

        public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
            => ObterPorEmailAsync(email, cancellationToken);

        public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.AtletaId == atletaId));

        public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => ObterPorAtletaIdAsync(atletaId, cancellationToken);

        public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            Itens.Add(usuario);
            return Task.CompletedTask;
        }

        public void Atualizar(Usuario usuario)
        {
        }
    }

    private sealed class AtletaRepositorioMemoria : IAtletaRepositorio
    {
        public List<Atleta> Itens { get; } = [];

        public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens);

        public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(Itens.Count);
        public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);
        public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default) => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());
        public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Atleta>>([]);

        public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
            => ObterPorIdAsync(id, cancellationToken);

        public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens.Where(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Atleta>>(Itens.Where(x => string.Equals(x.Email, email, StringComparison.OrdinalIgnoreCase)).ToList());

        public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default)
        {
            Itens.Add(atleta);
            return Task.CompletedTask;
        }

        public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Atleta atleta) { }
        public void AtualizarMedidas(AtletaMedidas medidas) { }
        public void Remover(Atleta atleta) => Itens.Remove(atleta);
    }

    private sealed class ArenaRepositorioMemoria : IArenaRepositorio
    {
        public List<Arena> Itens { get; } = [];

        public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(ArenaFiltroPublicoRequest filtro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>([]);
        public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<ArenaDetalhePublicoResponse?>(null);
        public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ArenaResumoPublicoResponse?>(null);
        public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(Guid usuarioId, bool incluirTodas, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>(Itens);
        public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default) => ObterPorIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>(Itens);
        public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default) => Task.FromResult(Itens.Any(x => x.Slug == slug && x.Id != idIgnorado));
        public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(Guid arenaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaEspaco>>([]);
        public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(Guid arenaId, Guid espacoId, CancellationToken cancellationToken = default) => Task.FromResult<ArenaEspaco?>(null);

        public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default)
        {
            Itens.Add(arena);
            return Task.CompletedTask;
        }

        public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Arena arena) { }
        public void AtualizarEspaco(ArenaEspaco espaco) { }
        public void Remover(Arena arena) => Itens.Remove(arena);
    }

    private sealed class GrupoRepositorioMemoria : IGrupoRepositorio
    {
        public List<Grupo> Itens { get; } = [];

        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(Itens);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(Itens);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(Itens.Count(x => x.Publico));
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(Itens.Take(limite).ToList());
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(Itens);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase) && x.UsuarioOrganizadorId == usuarioOrganizadorId));
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(Itens.Where(x => x.UsuarioOrganizadorId == usuarioOrganizadorId).ToList());
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default)
        {
            Itens.Add(grupo);
            return Task.CompletedTask;
        }

        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) => Itens.Remove(grupo);
    }

    private sealed class GrupoAtletaRepositorioMemoria : IGrupoAtletaRepositorio
    {
        public List<GrupoAtleta> Itens { get; } = [];

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.GrupoId == grupoId).ToList());

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(Itens.Where(x => x.AtletaId == atletaId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ListarPorAtletaAsync(atletaId, cancellationToken);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(Itens.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));

        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default)
        {
            Itens.Add(grupoAtleta);
            return Task.CompletedTask;
        }

        public void Remover(GrupoAtleta grupoAtleta) => Itens.Remove(grupoAtleta);
    }
}
