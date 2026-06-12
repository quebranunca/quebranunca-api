using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class ArenaServicoAdminTests
{
    [Fact]
    public async Task CriarAdminAsync_CriaArenaAtivaComResponsavelESlug()
    {
        var cenario = new Cenario();

        var arena = await cenario.Servico.CriarAdminAsync(NovoRequest("Arena São José"));

        Assert.Equal("arena-sao-jose", arena.Slug);
        Assert.True(arena.Ativa);
        Assert.True(arena.PossuiIluminacao);
        var responsavel = Assert.Single(arena.Responsaveis);
        Assert.Equal(cenario.Usuario.Id, responsavel.UsuarioId);
        Assert.Equal(PapelArenaResponsavel.ArenaAdmin, responsavel.Papel);
    }

    [Fact]
    public async Task CriarAdminAsync_AdicionaSufixoQuandoSlugJaExiste()
    {
        var cenario = new Cenario();
        cenario.AdicionarArena("Arena Sol!", "arena-sol");

        var arena = await cenario.Servico.CriarAdminAsync(NovoRequest("Arena Sol"));

        Assert.Equal("arena-sol-2", arena.Slug);
    }

    [Fact]
    public async Task CriarAdminAsync_NomeObrigatorio_Bloqueia()
    {
        var cenario = new Cenario();

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAdminAsync(NovoRequest("   ")));

        Assert.Equal("Nome da arena é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task CriarAdminAsync_TipoInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        var request = NovoRequest("Arena Inválida") with { TipoArena = (TipoArena)999 };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAdminAsync(request));

        Assert.Equal("Tipo de arena inválido.", excecao.Message);
    }

    [Fact]
    public async Task CriarAdminAsync_QuantidadeEspacosNegativa_Bloqueia()
    {
        var cenario = new Cenario();
        var request = NovoRequest("Arena Inválida") with { QuantidadeEspacos = -1 };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAdminAsync(request));

        Assert.Equal("Quantidade de espaços não pode ser negativa.", excecao.Message);
    }

    [Theory]
    [InlineData(-91.0, 0.0)]
    [InlineData(91.0, 0.0)]
    [InlineData(0.0, -181.0)]
    [InlineData(0.0, 181.0)]
    [InlineData(10.0, null)]
    public async Task CriarAdminAsync_CoordenadasInvalidas_Bloqueia(double? latitude, double? longitude)
    {
        var cenario = new Cenario();
        var request = NovoRequest("Arena Inválida") with { Latitude = latitude, Longitude = longitude };

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarAdminAsync(request));

        Assert.Equal("Latitude e longitude da arena devem ser informadas juntas e válidas.", excecao.Message);
    }

    [Fact]
    public async Task CriarAdminAsync_NomeComAcentosEspacosECaracteresEspeciais_GeraSlugNormalizado()
    {
        var cenario = new Cenario();

        var arena = await cenario.Servico.CriarAdminAsync(NovoRequest("  Árena São João!!! 2026  "));

        Assert.Equal("arena-sao-joao-2026", arena.Slug);
    }

    [Fact]
    public async Task ListarMinhasAsync_RetornaSomenteArenasAdministradasPeloUsuario()
    {
        var cenario = new Cenario();
        cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);
        cenario.AdicionarArena("Arena Alheia", "arena-alheia", new Usuario { Nome = "Outro", Email = "outro@qnf.test" });

        var arenas = await cenario.Servico.ListarMinhasAsync();

        var arena = Assert.Single(arenas);
        Assert.Equal("Arena Minha", arena.Nome);
        Assert.Equal(PapelArenaResponsavel.ArenaAdmin, arena.PapelUsuario);
    }

    [Fact]
    public async Task ObterAdminAsync_RetornaDetalheParaArenaAdmin()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        var detalhe = await cenario.Servico.ObterAdminAsync(arena.Id);

        Assert.Equal(arena.Id, detalhe.Id);
        Assert.Equal(cenario.Usuario.Email, Assert.Single(detalhe.Responsaveis).Email);
    }

    [Fact]
    public async Task ObterAdminAsync_BloqueiaUsuarioSemVinculo()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Alheia", "arena-alheia");

        await Assert.ThrowsAsync<AcessoNegadoException>(() => cenario.Servico.ObterAdminAsync(arena.Id));
    }

    [Fact]
    public async Task ObterAdminAsync_PermiteAdministradorGlobalSemVinculo()
    {
        var cenario = new Cenario();
        cenario.Usuario.Perfil = PerfilUsuario.Administrador;
        var arena = cenario.AdicionarArena("Arena Alheia", "arena-alheia");

        var detalhe = await cenario.Servico.ObterAdminAsync(arena.Id);

        Assert.Equal(arena.Id, detalhe.Id);
    }

    [Fact]
    public async Task AtualizarAdminAsync_AtualizaArenaComPermissaoEBloqueiaSemPermissao()
    {
        var cenario = new Cenario();
        var minhaArena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);
        var arenaAlheia = cenario.AdicionarArena("Arena Alheia", "arena-alheia");
        var request = NovoAtualizarRequest("Arena Renovada");

        var alterada = await cenario.Servico.AtualizarAdminAsync(minhaArena.Id, request);

        Assert.Equal("Arena Renovada", alterada.Nome);
        Assert.True(alterada.PossuiCobertura);
        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            cenario.Servico.AtualizarAdminAsync(arenaAlheia.Id, request));
    }

    [Fact]
    public async Task AtualizarStatusAsync_DesativadaNaoApareceEmConsultasPublicas()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        await cenario.Servico.AtualizarStatusAsync(arena.Id, false);
        var publicas = await cenario.Servico.ListarPublicasAsync(new(null, null, null, null));

        Assert.Empty(publicas);
    }

    [Fact]
    public async Task AtualizarVisibilidadeAsync_PrivadaNaoApareceEmConsultasPublicas()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        await cenario.Servico.AtualizarVisibilidadeAsync(arena.Id, false);
        var publicas = await cenario.Servico.ListarPublicasAsync(new(null, null, null, null));

        Assert.Empty(publicas);
    }

    [Fact]
    public async Task ListarEspacosAsync_RetornaEspacosOrdenadosPorOrdem()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);
        cenario.AdicionarEspaco(arena, "Quadra 2", TipoEspaco.QuadraCoberta, 2);
        cenario.AdicionarEspaco(arena, "Quadra 1", TipoEspaco.QuadraCoberta, 1);

        var espacos = await cenario.Servico.ListarEspacosAsync(arena.Id);

        Assert.Collection(espacos,
            item => Assert.Equal("Quadra 1", item.Nome),
            item => Assert.Equal("Quadra 2", item.Nome));
    }

    [Fact]
    public async Task CriarEspacoAsync_AdicionaEspacoComInformacoesBasicas()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        var espaco = await cenario.Servico.CriarEspacoAsync(arena.Id, new CriarArenaEspacoRequest(
            "Rede central",
            TipoEspaco.RedePraia,
            "Rede com luz e cobertura",
            true,
            true,
            true,
            3));

        Assert.Equal(arena.Id, espaco.ArenaId);
        Assert.Equal("Rede central", espaco.Nome);
        Assert.Equal(TipoEspaco.RedePraia, espaco.TipoEspaco);
        Assert.True(espaco.PossuiIluminacao);
        Assert.True(espaco.PossuiCobertura);
        Assert.True(espaco.Ativo);
        Assert.Equal(3, espaco.OrdemExibicao);
    }

    [Fact]
    public async Task CriarEspacoAsync_NomeObrigatorio_Bloqueia()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarEspacoAsync(arena.Id, new CriarArenaEspacoRequest(
                " ",
                TipoEspaco.QuadraCoberta,
                null,
                false,
                false,
                true,
                1)));

        Assert.Equal("Nome do espaço é obrigatório.", excecao.Message);
    }

    [Fact]
    public async Task CriarEspacoAsync_TipoEspacoInvalido_Bloqueia()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarEspacoAsync(arena.Id, new CriarArenaEspacoRequest(
                "Quadra",
                (TipoEspaco)999,
                null,
                false,
                false,
                true,
                1)));

        Assert.Equal("Tipo de espaço inválido.", excecao.Message);
    }

    [Fact]
    public async Task CriarEspacoAsync_OrdemExibicaoNegativa_Bloqueia()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);

        var excecao = await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.CriarEspacoAsync(arena.Id, new CriarArenaEspacoRequest(
                "Quadra",
                TipoEspaco.QuadraCoberta,
                null,
                false,
                false,
                true,
                -1)));

        Assert.Equal("Ordem de exibição não pode ser negativa.", excecao.Message);
    }

    [Fact]
    public async Task AtualizarStatusEspacoAsync_DesativaEspaco()
    {
        var cenario = new Cenario();
        var arena = cenario.AdicionarArena("Arena Minha", "arena-minha", cenario.Usuario);
        var espaco = cenario.AdicionarEspaco(arena, "Treino", TipoEspaco.AreaTreino, 1);

        await cenario.Servico.AtualizarStatusEspacoAsync(arena.Id, espaco.Id, false);

        var atualizados = await cenario.Servico.ListarEspacosAsync(arena.Id);
        Assert.Single(atualizados);
        Assert.False(atualizados[0].Ativo);
    }

    private static CriarArenaRequest NovoRequest(string nome)
        => new(
            nome, "Descrição", TipoArena.ArenaPrivada, "Rua 1", "Centro", "Santos", "SP",
            null, null, null, null, null, 0, true,
            true, false, false, true, false, false, false);

    private static AtualizarArenaRequest NovoAtualizarRequest(string nome)
        => new(
            nome, "Nova descrição", TipoArena.Clube, "Rua 2", "Bairro", "Santos", "SP",
            null, null, null, null, null, 3, true,
            true, true, true, true, true, true, true);

    private sealed class Cenario
    {
        private readonly List<Arena> arenas = [];

        public Cenario()
        {
            Usuario = new Usuario { Nome = "Gestor", Email = "gestor@qnf.test", Perfil = PerfilUsuario.Atleta };
            var repositorio = new ArenaRepositorioMemoria(arenas);
            Servico = new ArenaServico(
                repositorio,
                new ArenaResponsavelRepositorioMemoria(arenas),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServicoStub(Usuario));
        }

        public Usuario Usuario { get; }
        public ArenaServico Servico { get; }

        public Arena AdicionarArena(string nome, string slug, Usuario? responsavel = null)
        {
            var arena = new Arena
            {
                Nome = nome,
                Slug = slug,
                TipoArena = TipoArena.Praia,
                QuantidadeEspacos = 1,
                Publica = true,
                Ativa = true
            };
            if (responsavel is not null)
            {
                arena.Responsaveis.Add(new ArenaResponsavel
                {
                    ArenaId = arena.Id,
                    UsuarioId = responsavel.Id,
                    Usuario = responsavel,
                    Papel = PapelArenaResponsavel.ArenaAdmin,
                    Ativo = true
                });
            }

            arenas.Add(arena);
            return arena;
        }

        public ArenaEspaco AdicionarEspaco(Arena arena, string nome, TipoEspaco tipoEspaco, int? ordemExibicao = null)
        {
            var espaco = new ArenaEspaco
            {
                ArenaId = arena.Id,
                Nome = nome,
                TipoEspaco = tipoEspaco,
                Descricao = "Descrição",
                PossuiIluminacao = true,
                PossuiCobertura = true,
                Ativo = true,
                OrdemExibicao = ordemExibicao
            };

            arena.Espacos.Add(espaco);
            return espaco;
        }
    }

    private sealed class ArenaRepositorioMemoria(List<Arena> arenas) : IArenaRepositorio
    {
        public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
            ArenaFiltroPublicoRequest filtro,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>(arenas
                .Where(x => x.Ativa && x.Publica)
                .OrderBy(x => x.Nome)
                .Select(x => new ArenaListagemPublicaResponse(
                    x.Id, x.Nome, x.Slug, x.Descricao, x.TipoArena, x.Cidade, x.Estado,
                    x.EnderecoResumo, x.QuantidadeEspacos, x.LogoUrl, x.CapaUrl, x.Instagram,
                    x.Whatsapp, x.Publica, x.Ativa))
                .ToList());

        public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ArenaDetalhePublicoResponse?>(null);

        public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(
            Guid id,
            CancellationToken cancellationToken = default)
            => Task.FromResult<ArenaResumoPublicoResponse?>(null);

        public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(
            Guid usuarioId,
            bool incluirTodas,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Arena>>(arenas
                .Where(x => incluirTodas || x.Responsaveis.Any(r =>
                    r.UsuarioId == usuarioId && r.Ativo && r.Papel == PapelArenaResponsavel.ArenaAdmin))
                .OrderBy(x => x.Nome)
                .ToList());

        public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.FirstOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Arena>>(arenas);

        public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.FirstOrDefault(x => x.Id == id));

        public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.FirstOrDefault(x =>
                string.Equals(x.Nome, nome, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.Any(x => x.Id != idIgnorado && x.Slug == slug));

        public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(
            Guid arenaId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ArenaEspaco>>(arenas
                .First(x => x.Id == arenaId)
                .Espacos
                .OrderBy(x => x.OrdemExibicao ?? int.MaxValue)
                .ThenBy(x => x.Nome)
                .ToList());

        public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(
            Guid arenaId,
            Guid espacoId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.First(x => x.Id == arenaId).Espacos.FirstOrDefault(x => x.Id == espacoId));

        public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default)
        {
            arenas.Add(arena);
            return Task.CompletedTask;
        }

        public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default)
        {
            arenas.First(x => x.Id == espaco.ArenaId).Espacos.Add(espaco);
            return Task.CompletedTask;
        }

        public void Atualizar(Arena arena)
        {
        }

        public void AtualizarEspaco(ArenaEspaco espaco)
        {
        }

        public void Remover(Arena arena)
        {
            arenas.Remove(arena);
        }
    }

    private sealed class ArenaResponsavelRepositorioMemoria(List<Arena> arenas) : IArenaResponsavelRepositorio
    {
        public Task<bool> UsuarioPodeGerenciarAsync(
            Guid arenaId,
            Guid usuarioId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(arenas.Any(x => x.Id == arenaId && x.Responsaveis.Any(r =>
                r.UsuarioId == usuarioId && r.Ativo && r.Papel == PapelArenaResponsavel.ArenaAdmin)));

        public Task AdicionarAsync(ArenaResponsavel responsavel, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task ExecutarEmTransacaoAsync(
            Func<CancellationToken, Task> operacao,
            CancellationToken cancellationToken = default)
            => operacao(cancellationToken);
    }

    private sealed class AutorizacaoUsuarioServicoStub(Usuario usuario) : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<Usuario?>(usuario);

        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(usuario);

        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
