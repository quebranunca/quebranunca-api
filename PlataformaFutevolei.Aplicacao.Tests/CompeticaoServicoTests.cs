using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Aplicacao.Utilitarios;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class CompeticaoServicoTests
{
    [Fact]
    public async Task ListarAsync_SemCompeticoes_RetornaVazio()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario: admin);

        var resultado = await cenario.Servico.ListarAsync();

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task ListarAsync_SemAutenticacao_RetornaApenasCampeonatosEEventosOrdenados()
    {
        var cenario = new Cenario(
            usuario: null,
            competicoes: [
                Novo("Evento B", TipoCompeticao.Evento, DateTime.UtcNow.AddDays(2)),
                Novo("Grupo C", TipoCompeticao.Grupo, DateTime.UtcNow.AddDays(1)),
                Novo("Campeonato A", TipoCompeticao.Campeonato, DateTime.UtcNow.AddDays(1)),
                Novo("Evento A", TipoCompeticao.Evento, DateTime.UtcNow.AddDays(1))
            ]);

        var resultado = await cenario.Servico.ListarAsync();

        Assert.Equal(["Campeonato A", "Evento A", "Evento B"], resultado.Select(x => x.Nome).ToArray());
        Assert.DoesNotContain(resultado, x => x.Tipo == TipoCompeticao.Grupo);
    }

    [Fact]
    public async Task ListarAsync_AtletaSemAcessoNaoAutenticadoNoPrivado_RetornaSomenteComAcessoAoInformarPublico()
    {
        var atleta = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var campeonatoPublico = Novo("Campeonato Público", TipoCompeticao.Campeonato, DateTime.UtcNow);
        var eventoPrivado = Novo("Evento Privado", TipoCompeticao.Evento, DateTime.UtcNow);

        var cenario = new Cenario(
            usuario: atleta,
            competicoes: [eventoPrivado, campeonatoPublico],
            idsCompeticoesComAcesso: [eventoPrivado.Id]);

        var publico = await cenario.Servico.ListarAsync(incluirPublicas: true);
        var somenteAcesso = await cenario.Servico.ListarAsync(incluirPublicas: false);

        Assert.Equal(2, publico.Count);
        Assert.Single(somenteAcesso);
        Assert.Equal(eventoPrivado.Id, somenteAcesso.Single().Id);
    }

    [Fact]
    public async Task ListarAsync_ComUsuarioOrganizador_RetornaSomenteSuasCompeticoes()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var outra = new Usuario { Nome = "Outro", Perfil = PerfilUsuario.Organizador, Ativo = true };

        var cenario = new Cenario(
            usuario: organizador,
            competicoes:
            [
                Novo("Minha", TipoCompeticao.Campeonato, DateTime.UtcNow, usuarioOrganizadorId: organizador.Id),
                Novo("Outra", TipoCompeticao.Campeonato, DateTime.UtcNow, usuarioOrganizadorId: outra.Id)
            ]);

        var resultado = await cenario.Servico.ListarAsync();

        Assert.Single(resultado);
        Assert.Equal(organizador.Id, resultado.Single().UsuarioOrganizadorId);
    }

    [Fact]
    public async Task ObterPorIdAsync_AdminConsegueVisualizarCompeticao_ComRelacionamentos()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var liga = new Liga { Nome = "Liga Sul", Descricao = "l" };
        var arena = new Arena { Nome = "Arena Sul" };
        var regra = new RegraCompeticao { Nome = "Regra Gold" };
        var competicao = Novo(
            "Campeonato Completo",
            TipoCompeticao.Campeonato,
            DateTime.UtcNow,
            liga: liga,
            arena: arena,
            regra: regra);

        var cenario = new Cenario(
            usuario: admin,
            competicoes: [competicao],
            ligas: [liga],
            arenas: [arena],
            regras: [regra]);

        var resultado = await cenario.Servico.ObterPorIdAsync(competicao.Id);

        Assert.Equal("Liga Sul", resultado.NomeLiga);
        Assert.Equal("Arena Sul", resultado.NomeArena);
        Assert.Equal("Regra Gold", resultado.NomeRegraCompeticao);
    }

    [Fact]
    public async Task ObterPorIdAsync_AdminConsegueVisualizarCompeticao()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var liga = new Liga { Nome = "Liga Norte", Descricao = "l" };
        var arena = new Arena { Nome = "Arena Centro" };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow, liga: liga, arena: arena);

        var cenario = new Cenario(
            usuario: admin,
            competicoes: [competicao],
            ligas: [liga],
            arenas: [arena]);

        var resultado = await cenario.Servico.ObterPorIdAsync(competicao.Id);

        Assert.Equal(competicao.Id, resultado.Id);
        Assert.Equal(liga.Id, resultado.LigaId);
        Assert.Equal(arena.Id, resultado.ArenaId);
    }

    [Fact]
    public async Task ObterPorIdAsync_OrganizadorNaoDono_Bloqueia()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow, usuarioOrganizadorId: Guid.NewGuid());

        var cenario = new Cenario(usuario: organizador, competicoes: [competicao]);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.ObterPorIdAsync(competicao.Id));
    }

    [Fact]
    public async Task ObterPorIdAsync_UsuarioComum_Bloqueia()
    {
        var atleta = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow);

        var cenario = new Cenario(usuario: atleta, competicoes: [competicao], idsCompeticoesComAcesso: []);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.ObterPorIdAsync(competicao.Id));
    }

    [Fact]
    public async Task ObterPorIdAsync_UsuarioInativo_UsuarioNaoEncontrado()
    {
        var inativo = new Usuario { Nome = "Inativo", Perfil = PerfilUsuario.Administrador, Ativo = false };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow);

        var cenario = new Cenario(usuario: inativo, competicoes: [competicao]);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => cenario.Servico.ObterPorIdAsync(competicao.Id));
    }

    [Fact]
    public async Task ObterPorIdAsync_NaoAutenticado_Bloqueia()
    {
        var cenario = new Cenario(usuario: null);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.ObterPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CriarAsync_ComOrganizador_PreencheOrganizador()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var formato = FormatoPadrao(TipoCompeticao.Campeonato);
        var cenario = new Cenario(usuario: organizador, formatos: [formato]);

        var resultado = await cenario.Servico.CriarAsync(CriarDto(TipoCompeticao.Campeonato));

        Assert.Equal(organizador.Id, resultado.UsuarioOrganizadorId);
        Assert.NotEqual(Guid.Empty, resultado.FormatoCampeonatoId);
    }

    [Fact]
    public async Task CriarAsync_ComAdmin_SemOrganizador()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var formato = FormatoPadrao(TipoCompeticao.Campeonato);
        var cenario = new Cenario(usuario: admin, formatos: [formato]);

        var resultado = await cenario.Servico.CriarAsync(CriarDto(TipoCompeticao.Campeonato));

        Assert.Null(resultado.UsuarioOrganizadorId);
    }

    [Fact]
    public async Task CriarAsync_ComArenaLigaERregraCustomizada_PersistemVinculos()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var liga = new Liga { Nome = "Liga Sul", Descricao = "l" };
        var arena = new Arena { Nome = "Arena Sul" };
        var regra = new RegraCompeticao { Nome = "Custom", PontosVitoria = 10m };
        var formato = FormatoPadrao(TipoCompeticao.Campeonato);

        var cenario = new Cenario(
            usuario: admin,
            ligas: [liga],
            arenas: [arena],
            formatos: [formato],
            regras: [regra]);

        var dto = CriarDto(
            TipoCompeticao.Campeonato,
            nome: "Campeonato Premium",
            ligaId: liga.Id,
            arenaId: arena.Id,
            regraCompeticaoId: regra.Id);

        var resultado = await cenario.Servico.CriarAsync(dto);

        Assert.Equal(liga.Id, resultado.LigaId);
        Assert.Equal(arena.Id, resultado.ArenaId);
        Assert.Equal(regra.Id, resultado.RegraCompeticaoId);
        Assert.Equal(TipoCompeticao.Campeonato, resultado.Tipo);
    }

    [Fact]
    public async Task CriarAsync_SemTipoOuFormatoInformado_UsaPadrao()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var formato = FormatoPadrao(TipoCompeticao.Campeonato);
        var cenario = new Cenario(usuario: admin, formatos: [formato]);

        var resultado = await cenario.Servico.CriarAsync(CriarDto(TipoCompeticao.Campeonato));

        Assert.Equal(formato.Id, resultado.FormatoCampeonatoId);
    }

    [Fact]
    public async Task CriarAsync_DataFimAnteriorAoInicio_Bloqueia()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario: admin, formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        var dto = CriarDto(TipoCompeticao.Campeonato, dataFim: DateTime.UtcNow.AddDays(-1));

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.CriarAsync(dto));
    }

    [Fact]
    public async Task CriarAsync_NomeVazio_Bloqueia()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario: admin, formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        var dto = CriarDto(TipoCompeticao.Campeonato, nome: "   ");
        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.CriarAsync(dto));
    }

    [Fact]
    public async Task CriarAsync_AtletaSemPermissao_Bloqueia()
    {
        var atleta = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = Guid.NewGuid() };
        var cenario = new Cenario(usuario: atleta, formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.CriarAsync(CriarDto(TipoCompeticao.Campeonato)));
    }

    [Fact]
    public async Task AtualizarAsync_Organizador_DadoValidoAtualizaCamposSemAlterarUsuario()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow.AddDays(-10), usuarioOrganizadorId: organizador.Id);

        var cenario = new Cenario(
            usuario: organizador,
            competicoes: [competicao],
            formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        var atualizado = await cenario.Servico.AtualizarAsync(competicao.Id, AtualizarDto("Campeonato X", TipoCompeticao.Campeonato));

        Assert.Equal("Campeonato X", atualizado.Nome);
        Assert.Equal(organizador.Id, atualizado.UsuarioOrganizadorId);
    }

    [Fact]
    public async Task AtualizarAsync_OrganizadorNaoDono_Bloqueia()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow, usuarioOrganizadorId: Guid.NewGuid());

        var cenario = new Cenario(
            usuario: organizador,
            competicoes: [competicao],
            formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(competicao.Id, AtualizarDto("Novo")));
    }

    [Fact]
    public async Task AtualizarAsync_NaoEncontrada_Bloqueia()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario: admin, formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() =>
            cenario.Servico.AtualizarAsync(Guid.NewGuid(), AtualizarDto("Novo")));
    }

    [Fact]
    public async Task AtualizarAsync_DadosInvalidos_Bloqueia()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow);

        var cenario = new Cenario(
            usuario: admin,
            competicoes: [competicao],
            formatos: [FormatoPadrao(TipoCompeticao.Campeonato)]);

        var dto = AtualizarDto("  ", TipoCompeticao.Campeonato, dataFim: DateTime.UtcNow.AddDays(-2));

        await Assert.ThrowsAsync<RegraNegocioException>(() =>
            cenario.Servico.AtualizarAsync(competicao.Id, dto));
    }

    [Fact]
    public async Task RemoverAsync_Admin_Remover()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow);

        var cenario = new Cenario(usuario: admin, competicoes: [competicao]);

        await cenario.Servico.RemoverAsync(competicao.Id);

        var restantes = await cenario.Servico.ListarAsync();
        Assert.Empty(restantes);
    }

    [Fact]
    public async Task RemoverAsync_OrganizadorNaoDono_Bloqueia()
    {
        var organizador = new Usuario { Nome = "Org", Perfil = PerfilUsuario.Organizador, Ativo = true };
        var competicao = Novo("Campeonato", TipoCompeticao.Campeonato, DateTime.UtcNow, usuarioOrganizadorId: Guid.NewGuid());

        var cenario = new Cenario(usuario: organizador, competicoes: [competicao]);

        await Assert.ThrowsAsync<RegraNegocioException>(() => cenario.Servico.RemoverAsync(competicao.Id));
    }

    [Fact]
    public async Task RemoverAsync_CompeticaoInexistente_Bloqueia()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var cenario = new Cenario(usuario: admin);

        await Assert.ThrowsAsync<EntidadeNaoEncontradaException>(() => cenario.Servico.RemoverAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ObterCampeonatoPorIdAsync_AdminConsegueVisualizarCategoria()
    {
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var categoriaA = new CategoriaCompeticao
        {
            Nome = "Feminino",
            Genero = GeneroCategoria.Misto,
            Nivel = NivelCategoria.Profissional,
        };
        var categoriaB = new CategoriaCompeticao
        {
            Nome = "Masculino",
            Genero = GeneroCategoria.Masculino,
            Nivel = NivelCategoria.Intermediario,
        };
        var competicao = Novo(
            "Campeonato Regional",
            TipoCompeticao.Campeonato,
            DateTime.UtcNow.AddHours(1),
            categorias: [categoriaA, categoriaB]);

        categoriaA.Competicao = competicao;
        categoriaA.CompeticaoId = competicao.Id;
        categoriaB.Competicao = competicao;
        categoriaB.CompeticaoId = competicao.Id;

        var cenario = new Cenario(
            usuario: admin,
            competicoes: [competicao]);

        var resultado = await cenario.Servico.ObterCampeonatoPorIdAsync(competicao.Id);

        Assert.Equal(2, resultado.Categorias.Count);
        Assert.Contains("Feminino", resultado.Categorias.Select(x => x.Nome));
        Assert.Contains("Masculino", resultado.Categorias.Select(x => x.Nome));
    }

    private static Competicao Novo(
        string nome,
        TipoCompeticao tipo,
        DateTime dataInicio,
        Guid? usuarioOrganizadorId = null,
        Liga? liga = null,
        Arena? arena = null,
        RegraCompeticao? regra = null,
        IReadOnlyList<CategoriaCompeticao>? categorias = null)
        => new()
        {
            Nome = nome,
            Tipo = tipo,
            Descricao = "desc",
            DataInicio = dataInicio,
            DataFim = null,
            Liga = liga,
            LigaId = liga?.Id,
            Arena = arena,
            ArenaId = arena?.Id,
            RegraCompeticao = regra,
            RegraCompeticaoId = regra?.Id,
            UsuarioOrganizadorId = usuarioOrganizadorId
,
            Categorias = categorias?.ToList() ?? []
        };

    private static CriarCompeticaoDto CriarDto(
        TipoCompeticao tipo,
        string nome = "Campeonato Teste",
        Guid? ligaId = null,
        Guid? arenaId = null,
        Guid? regraCompeticaoId = null,
        DateTime? dataFim = null)
        => new(
            nome,
            tipo,
            Descricao: "desc",
            Link: null,
            DataInicio: DateTime.UtcNow,
            DataFim: dataFim,
            LigaId: ligaId,
            LocalId: null,
            FormatoCampeonatoId: null,
            RegraCompeticaoId: regraCompeticaoId,
            InscricoesAbertas: true,
            PossuiFinalReset: null,
            ArenaId: arenaId);

    private static AtualizarCompeticaoDto AtualizarDto(
        string nome,
        TipoCompeticao tipo = TipoCompeticao.Campeonato,
        DateTime? dataFim = null)
        => new(
            nome,
            tipo,
            Descricao: "desc atualizada",
            Link: "https://exemplo.com.br",
            DataInicio: DateTime.UtcNow,
            DataFim: dataFim,
            LigaId: null,
            LocalId: null,
            FormatoCampeonatoId: null,
            RegraCompeticaoId: null,
            InscricoesAbertas: null,
            PossuiFinalReset: null,
            ArenaId: null);

    private static FormatoCampeonato FormatoPadrao(TipoCompeticao tipo)
    {
        var definicao = tipo is TipoCompeticao.Grupo
            ? FormatosCampeonatoPadrao.Lista.First(x => x.TipoFormato == TipoFormatoCampeonato.PontosCorridos)
            : FormatosCampeonatoPadrao.Lista.First(x => x.TipoFormato == TipoFormatoCampeonato.Chave);

        return new FormatoCampeonato
        {
            Nome = definicao.Nome,
            Descricao = definicao.Descricao,
            TipoFormato = definicao.TipoFormato,
            Ativo = definicao.Ativo,
            QuantidadeDerrotasParaEliminacao = definicao.QuantidadeDerrotasParaEliminacao
        };
    }

    private sealed class Cenario
    {
        public Cenario(
            Usuario? usuario,
            IReadOnlyList<Competicao>? competicoes = null,
            IReadOnlyList<Liga>? ligas = null,
            IReadOnlyList<Arena>? arenas = null,
            IReadOnlyList<FormatoCampeonato>? formatos = null,
            IReadOnlyList<RegraCompeticao>? regras = null,
            IReadOnlyList<Guid>? idsCompeticoesComAcesso = null)
        {
            Usuario = usuario;
            Competicoes = new CompeticaoRepositorioStub(competicoes ?? [], idsCompeticoesComAcesso ?? []);
            Servico = new CompeticaoServico(
                Competicoes,
                new CategoriaCompeticaoRepositorioStub(),
                new AtletaRepositorioStub(),
                new PartidaRepositorioStub(),
                new GrupoRepositorioStub(),
                new FormatoCampeonatoRepositorioStub(formatos ?? []),
                new LigaRepositorioStub(ligas ?? []),
                new ArenaRepositorioStub(arenas ?? []),
                new RegraCompeticaoRepositorioStub(regras ?? []),
                new UnidadeTrabalhoStub(),
                new AutorizacaoUsuarioServico(
                    new UsuarioRepositorioStub(Usuario is null ? [] : [Usuario]),
                    Competicoes,
                    new GrupoRepositorioStub(),
                    new UsuarioContextoStub(Usuario?.Id)));
        }

        public Usuario? Usuario { get; }
        public ICompeticaoRepositorio Competicoes { get; }
        public CompeticaoServico Servico { get; }

        private sealed class UsuarioContextoStub(Guid? usuarioId) : IUsuarioContexto
        {
            public Guid? UsuarioId { get; } = usuarioId;
        }

        private sealed class UsuarioRepositorioStub(IReadOnlyList<Usuario> usuarios) : IUsuarioRepositorio
        {
            private readonly List<Usuario> itens = usuarios.ToList();

            public Task<int> ContarAdministradoresAtivosAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(itens.Count(x => x.Perfil == PerfilUsuario.Administrador && x.Ativo));

            public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<IReadOnlyList<Usuario>> ListarAsync(string? nome, string? email, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Usuario>>(itens);

            public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task<Usuario?> ObterPorEmailParaAtualizacaoAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task<Usuario?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task<Usuario?> ObterPorAtletaIdAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task<Usuario?> ObterPorAtletaIdParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Usuario?>(null);

            public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(Usuario usuario) { }

            public void Remover(Usuario usuario) { }
        }

        private sealed class CompeticaoRepositorioStub(IReadOnlyList<Competicao> competicoes, IReadOnlyList<Guid>? idsComAcesso = null) : ICompeticaoRepositorio
        {
            private readonly List<Competicao> itens = competicoes.ToList();
            private readonly List<Guid> idsComAcesso = idsComAcesso?.ToList() ?? [];

            public IReadOnlyList<Competicao> Lista => itens;

            public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Competicao>>(itens);

            public Task<Competicao?> ObterGrupoResumoUsuarioAsync(
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<Competicao?>(null);

            public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Guid>>(idsComAcesso);

            public Task<bool> AtletaPossuiAcessoAsync(
                Guid competicaoId,
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult(idsComAcesso.Contains(competicaoId));

            public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Nome == nome));

            public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default)
            {
                itens.Add(competicao);
                return Task.CompletedTask;
            }

            public void Atualizar(Competicao competicao) { }

            public void Remover(Competicao competicao) => itens.Remove(competicao);
        }

        private sealed class AtletaRepositorioStub : IAtletaRepositorio
        {
            public Task<IReadOnlyList<Atleta>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<int> ContarAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<IReadOnlyList<Atleta>> ListarComEmailEmPartidasSemUsuarioAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<IReadOnlyList<Atleta>> ListarInscritosPorOrganizadorAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<bool> PertenceAoOrganizadorAsync(Guid atletaId, Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<IReadOnlyList<Atleta>> BuscarAsync(string? termo, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<IDictionary<Guid, int>> ContarPartidasPorAtletasAsync(IEnumerable<Guid> atletaIds, CancellationToken cancellationToken = default)
                => Task.FromResult<IDictionary<Guid, int>>(new Dictionary<Guid, int>());

            public Task<IReadOnlyList<Atleta>> BuscarSugestoesPorCompeticaoAsync(Guid competicaoId, string termo, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<Atleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Atleta?>(null);

            public Task<Atleta?> ObterPorIdParaAtualizacaoAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Atleta?>(null);

            public Task<Atleta?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<Atleta?>(null);

            public Task<IReadOnlyList<Atleta>> ListarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task<IReadOnlyList<Atleta>> ListarPorEmailAsync(string email, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Atleta>>([]);

            public Task AdicionarAsync(Atleta atleta, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task AdicionarMedidasAsync(AtletaMedidas medidas, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(Atleta atleta) { }

            public void AtualizarMedidas(AtletaMedidas medidas) { }

            public void Remover(Atleta atleta) { }
        }

        private sealed class PartidaRepositorioStub : IPartidaRepositorio
        {
            public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Partida>>([]);

            public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<Guid?>(null);

            public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(
                Guid atletaId,
                Guid? grupoId,
                int limitePorSecao,
                CancellationToken cancellationToken = default)
                => Task.FromResult<AtletasSugestoesPartidaDto>(
                    new AtletasSugestoesPartidaDto([], []));

            public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
                => Task.FromResult<UsuarioResumoDto>(new(string.Empty, 0, 0, 0, 0m, 0, 0m, 0m));

            public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Partida?>(null);

            public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(Partida partida) { }

            public void Remover(Partida partida) { }
        }

        private sealed class GrupoRepositorioStub : IGrupoRepositorio
        {
            public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Grupo>>([]);

            public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(
                Guid usuarioId,
                Guid? atletaId,
                bool incluirPrivadosDeTerceiros,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Grupo>>([]);

            public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

            public Task<Grupo?> ObterResumoUsuarioAsync(
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<Grupo?>(null);

            public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(
                Guid usuarioId,
                Guid? atletaId,
                int limite,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Grupo>>([]);

            public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Grupo>>([]);

            public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Guid>>([]);

            public Task<bool> AtletaPossuiAcessoAsync(
                Guid grupoId,
                Guid usuarioId,
                Guid? atletaId,
                CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<Grupo?> ObterPorNomeEOrganizadorAsync(
                string nome,
                Guid? usuarioOrganizadorId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<Grupo?>(null);

            public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<Grupo?>(null);

            public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(
                Guid usuarioOrganizadorId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Grupo>>([]);

            public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Grupo?>(null);

            public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Atualizar(Grupo grupo) { }

            public void Remover(Grupo grupo) { }
        }

        private sealed class FormatoCampeonatoRepositorioStub(IReadOnlyList<FormatoCampeonato> formatos) : IFormatoCampeonatoRepositorio
        {
            private readonly List<FormatoCampeonato> itens = formatos.ToList();

            public Task<IReadOnlyList<FormatoCampeonato>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<FormatoCampeonato>>(itens);

            public Task<FormatoCampeonato?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<FormatoCampeonato?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Nome == nome));

            public Task AdicionarAsync(FormatoCampeonato formato, CancellationToken cancellationToken = default)
            {
                itens.Add(formato);
                return Task.CompletedTask;
            }

            public void Atualizar(FormatoCampeonato formato) { }

            public void Remover(FormatoCampeonato formato) => itens.Remove(formato);
        }

        private sealed class LigaRepositorioStub(IReadOnlyList<Liga> ligas) : ILigaRepositorio
        {
            private readonly List<Liga> itens = ligas.ToList();

            public Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Liga>>(itens);

            public Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<Liga?>(null);

            public Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default)
            {
                itens.Add(liga);
                return Task.CompletedTask;
            }

            public void Atualizar(Liga liga) { }

            public void Remover(Liga liga) => itens.Remove(liga);
        }

        private sealed class ArenaRepositorioStub(IReadOnlyList<Arena> arenas) : IArenaRepositorio
        {
            private readonly List<Arena> itens = arenas.ToList();

            public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(
                ArenaFiltroPublicoRequest filtro,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>([]);

            public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(string slug, CancellationToken cancellationToken = default)
                => Task.FromResult<ArenaDetalhePublicoResponse?>(null);

            public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<ArenaResumoPublicoResponse?>(null);

            public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(
                Guid usuarioId,
                bool incluirTodas,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Arena>>(itens);

            public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<Arena?>(null);

            public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<Arena>>(itens);

            public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<Arena?>(null);

            public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default)
                => Task.FromResult(false);

            public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(Guid arenaId, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ArenaEspaco>>([]);

            public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(
                Guid arenaId,
                Guid espacoId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<ArenaEspaco?>(null);

            public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default)
            {
                itens.Add(arena);
                return Task.CompletedTask;
            }

            public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(Arena arena) { }

            public void AtualizarEspaco(ArenaEspaco espaco) { }

            public void Remover(Arena arena) => itens.Remove(arena);
        }

        private sealed class RegraCompeticaoRepositorioStub(IReadOnlyList<RegraCompeticao> regras) : IRegraCompeticaoRepositorio
        {
            private readonly List<RegraCompeticao> itens = regras.ToList();

            public Task<IReadOnlyList<RegraCompeticao>> ListarAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<RegraCompeticao>>(itens);

            public Task<RegraCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult(itens.FirstOrDefault(x => x.Id == id));

            public Task<RegraCompeticao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
                => Task.FromResult<RegraCompeticao?>(null);

            public Task AdicionarAsync(RegraCompeticao regra, CancellationToken cancellationToken = default)
            {
                itens.Add(regra);
                return Task.CompletedTask;
            }

            public void Atualizar(RegraCompeticao regra) { }

            public void Remover(RegraCompeticao regra) => itens.Remove(regra);
        }

        private sealed class CategoriaCompeticaoRepositorioStub : ICategoriaCompeticaoRepositorio
        {
            public Task<IReadOnlyList<CategoriaCompeticao>> ListarPorCompeticaoAsync(
                Guid competicaoId,
                CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([]);

            public Task<IReadOnlyList<CategoriaCompeticao>> ListarDisponiveisParaVinculoAsync(CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<CategoriaCompeticao>>([]);

            public Task<CategoriaCompeticao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
                => Task.FromResult<CategoriaCompeticao?>(null);

            public Task AdicionarAsync(CategoriaCompeticao categoria, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public void Atualizar(CategoriaCompeticao categoria) { }

            public void Remover(CategoriaCompeticao categoria) { }
        }

        private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
        {
            public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
                => Task.FromResult(1);

            public Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
                => operacao(cancellationToken);
        }
    }
}
