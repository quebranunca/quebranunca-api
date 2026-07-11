using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Excecoes;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class GrupoServicoTests
{
    [Fact]
    public async Task ListarAsync_SemGrupos_RetornaListaVazia()
    {
        var servico = CriarServico(grupos: []);

        var grupos = await servico.ListarAsync();

        Assert.Empty(grupos);
    }

    [Fact]
    public async Task ListarAsync_GrupoSemImagemOuAtletas_RetornaGrupoSemErro()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo sem imagem",
            DataInicio = DateTime.UtcNow,
            Publico = true,
            ImagemUrl = null
        };
        var servico = CriarServico(grupos: [grupo]);

        var grupos = await servico.ListarAsync();

        var dto = Assert.Single(grupos);
        Assert.Equal(grupo.Id, dto.Id);
        Assert.Equal("Grupo sem imagem", dto.Nome);
        Assert.Null(dto.ImagemUrl);
        Assert.Equal("Público", dto.Privacidade);
    }

    [Fact]
    public async Task ObterDashboardAsync_GrupoSemAtletasPartidasOuRanking_RetornaEstadoVazio()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo novo",
            DataInicio = DateTime.UtcNow,
            Publico = true
        };
        var servico = CriarServico(grupos: [grupo]);

        var dashboard = await servico.ObterDashboardAsync(grupo.Id);

        Assert.Equal(0, dashboard.Resumo.TotalMembros);
        Assert.Equal(0, dashboard.Resumo.TotalPartidas);
        Assert.Empty(dashboard.Ranking);
        Assert.Empty(dashboard.UltimasPartidas);
        Assert.Empty(dashboard.MembrosMaisAtivos);
    }

    [Fact]
    public async Task ObterDashboardAsync_UsuarioMembro_HabilitaRegistroDePartida()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo com membro",
            DataInicio = DateTime.UtcNow,
            Publico = true
        };
        var atleta = new Atleta { Nome = "Usuário Membro" };
        var usuario = new Usuario
        {
            Nome = "Usuário Membro",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            AtletaId = atleta.Id
        };
        var membro = new GrupoAtleta
        {
            GrupoId = grupo.Id,
            AtletaId = atleta.Id,
            Atleta = atleta
        };
        var servico = CriarServico(
            grupos: [grupo],
            grupoAtletas: new GrupoAtletaRepositorioStub([membro]),
            autorizacao: new AutorizacaoUsuarioServicoStub(usuario));

        var dashboard = await servico.ObterDashboardAsync(grupo.Id);

        Assert.True(dashboard.Grupo.PertenceAoGrupo);
        Assert.True(dashboard.Grupo.PodeRegistrarPartida);
    }

    [Fact]
    public async Task ObterDashboardAsync_UsuarioNaoMembroGrupoPublico_NaoHabilitaRegistroNemEdicao()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo público",
            DataInicio = DateTime.UtcNow,
            Publico = true
        };
        var atleta = new Atleta { Nome = "Visitante" };
        var usuario = new Usuario
        {
            Nome = "Visitante",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true,
            AtletaId = atleta.Id
        };
        var servico = CriarServico(
            grupos: [grupo],
            autorizacao: new AutorizacaoUsuarioServicoStub(usuario));

        var dashboard = await servico.ObterDashboardAsync(grupo.Id);

        Assert.False(dashboard.Grupo.PertenceAoGrupo);
        Assert.False(dashboard.Grupo.PodeRegistrarPartida);
        Assert.False(dashboard.Grupo.PodeEditar);
    }

    [Fact]
    public async Task ObterDashboardAsync_PartidaComAtletasSemFoto_RetornaAvataresNulos()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo com partida",
            DataInicio = DateTime.UtcNow,
            Publico = true
        };
        var partida = CriarPartidaDoGrupo(grupo.Id);
        var servico = CriarServico(grupos: [grupo], partidas: [partida]);

        var dashboard = await servico.ObterDashboardAsync(grupo.Id);

        var partidaDto = Assert.Single(dashboard.UltimasPartidas);
        Assert.All(partidaDto.Dupla1.Concat(partidaDto.Dupla2), atleta => Assert.Null(atleta.AvatarUrl));
    }

    [Fact]
    public async Task CriarAsync_PrivacidadePublica_CriaGrupoPublico()
    {
        var servico = CriarServico(grupos: []);

        var grupo = await servico.CriarAsync(new CriarGrupoDto(
            "Grupo Público",
            Descricao: null,
            Link: null,
            DataInicio: DateTime.UtcNow,
            DataFim: null,
            LocalId: null,
            Privacidade: "Público"));

        Assert.Equal("Grupo Público", grupo.Nome);
        Assert.Equal("Público", grupo.Privacidade);
    }

    [Fact]
    public async Task CriarAsync_PrivacidadePrivada_CriaGrupoPrivado()
    {
        var servico = CriarServico(grupos: []);

        var grupo = await servico.CriarAsync(new CriarGrupoDto(
            "Grupo Privado",
            Descricao: null,
            Link: null,
            DataInicio: DateTime.UtcNow,
            DataFim: null,
            LocalId: null,
            Privacidade: "Privado"));

        Assert.Equal("Grupo Privado", grupo.Nome);
        Assert.Equal("Privado", grupo.Privacidade);
    }

    [Fact]
    public async Task AtualizarAsync_AlteraNomeEPrivacidade()
    {
        var grupo = new Grupo
        {
            Nome = "Nome antigo",
            DataInicio = DateTime.UtcNow,
            Publico = true
        };
        var servico = CriarServico(grupos: [grupo]);

        var atualizado = await servico.AtualizarAsync(grupo.Id, new AtualizarGrupoDto(
            "Nome novo",
            Privacidade: "Privado"));

        Assert.Equal("Nome novo", atualizado.Nome);
        Assert.Equal("Privado", atualizado.Privacidade);
    }

    [Fact]
    public async Task ListarParaSelecaoAsync_UsuarioAdministrador_NaoSolicitaPrivadosDeTerceiros()
    {
        var usuario = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var repositorio = new GrupoRepositorioStub([]);
        var servico = CriarServico(grupos: [], grupoRepositorio: repositorio, autorizacao: new AutorizacaoUsuarioServicoStub(usuario));

        await servico.ListarParaSelecaoAsync();

        Assert.Equal(usuario.Id, repositorio.UltimoUsuarioSelecaoId);
        Assert.False(repositorio.UltimoIncluirPrivadosDeTerceiros);
    }

    [Fact]
    public async Task ListarParaSelecaoAsync_UsuarioAtleta_NaoSolicitaPrivadosDeTerceiros()
    {
        var atletaId = Guid.NewGuid();
        var usuario = new Usuario { Nome = "Atleta", Perfil = PerfilUsuario.Atleta, Ativo = true, AtletaId = atletaId };
        var repositorio = new GrupoRepositorioStub([]);
        var servico = CriarServico(grupos: [], grupoRepositorio: repositorio, autorizacao: new AutorizacaoUsuarioServicoStub(usuario));

        await servico.ListarParaSelecaoAsync();

        Assert.Equal(usuario.Id, repositorio.UltimoUsuarioSelecaoId);
        Assert.Equal(atletaId, repositorio.UltimoAtletaSelecaoId);
        Assert.False(repositorio.UltimoIncluirPrivadosDeTerceiros);
    }

    [Fact]
    public async Task AtualizarAsync_SemPermissaoDeGestao_BloqueiaEdicao()
    {
        var grupo = new Grupo { Nome = "Grupo", DataInicio = DateTime.UtcNow, Publico = true };
        var autorizacao = new AutorizacaoUsuarioServicoStub(excecaoGestaoGrupo: new AcessoNegadoException("sem acesso"));
        var servico = CriarServico(grupos: [grupo], autorizacao: autorizacao);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.AtualizarAsync(grupo.Id, new AtualizarGrupoDto("Novo nome")));
    }

    [Fact]
    public async Task AtualizarAsync_NomeDuplicado_BloqueiaEdicao()
    {
        var grupo = new Grupo { Nome = "Grupo", DataInicio = DateTime.UtcNow, Publico = true };
        var grupoPadrao = new GrupoPadraoServicoStub(new RegraNegocioException("Nome indisponível."));
        var servico = CriarServico(grupos: [grupo], grupoPadrao: grupoPadrao);

        await Assert.ThrowsAsync<RegraNegocioException>(() => servico.AtualizarAsync(grupo.Id, new AtualizarGrupoDto("Outro grupo")));
    }

    [Fact]
    public async Task AtualizarImagemAsync_GrupoComImagemAnterior_AtualizaImagemERemoveArquivoAnterior()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo",
            DataInicio = DateTime.UtcNow,
            Publico = true,
            ImagemUrl = "https://cdn.example/antiga.jpg",
            ImagemPublicId = "imagem-antiga"
        };
        var fotos = new FotoPerfilServiceStub
        {
            UploadGrupo = new FotoPerfilUploadDto("https://cdn.example/nova.jpg", "imagem-nova")
        };
        var servico = CriarServico(grupos: [grupo], fotos: fotos);

        var resposta = await servico.AtualizarImagemAsync(grupo.Id, CriarArquivoFoto());

        Assert.Equal("https://cdn.example/nova.jpg", resposta.ImagemUrl);
        Assert.Equal("https://cdn.example/nova.jpg", grupo.ImagemUrl);
        Assert.Equal("imagem-nova", grupo.ImagemPublicId);
        Assert.Equal(1, fotos.EnviosGrupo);
        Assert.Contains("imagem-antiga", fotos.PublicIdsRemovidos);
    }

    [Fact]
    public async Task RemoverImagemAsync_GrupoComImagem_LimpaImagemERemoveArquivo()
    {
        var grupo = new Grupo
        {
            Nome = "Grupo",
            DataInicio = DateTime.UtcNow,
            Publico = true,
            ImagemUrl = "https://cdn.example/grupo.jpg",
            ImagemPublicId = "grupo-public-id"
        };
        var fotos = new FotoPerfilServiceStub();
        var servico = CriarServico(grupos: [grupo], fotos: fotos);

        await servico.RemoverImagemAsync(grupo.Id);

        Assert.Null(grupo.ImagemUrl);
        Assert.Null(grupo.ImagemPublicId);
        Assert.Contains("grupo-public-id", fotos.PublicIdsRemovidos);
    }

    [Fact]
    public async Task RemoverAsync_CriadorSemPartidas_RemoveGrupo()
    {
        var usuario = new Usuario { Nome = "Owner", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var grupo = new Grupo
        {
            Nome = "Grupo",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = usuario.Id,
            ImagemPublicId = "grupo-public-id"
        };
        var grupos = new GrupoRepositorioStub([grupo]);
        var fotos = new FotoPerfilServiceStub();
        var servico = CriarServico(
            grupos: [grupo],
            grupoRepositorio: grupos,
            autorizacao: new AutorizacaoUsuarioServicoStub(usuario),
            fotos: fotos);

        await servico.RemoverAsync(grupo.Id);

        Assert.Empty(await grupos.ListarAsync());
        Assert.Contains("grupo-public-id", fotos.PublicIdsRemovidos);
    }

    [Fact]
    public async Task RemoverAsync_AdministradorNaoCriador_BloqueiaExclusao()
    {
        var usuarioCriadorId = Guid.NewGuid();
        var admin = new Usuario { Nome = "Admin", Perfil = PerfilUsuario.Administrador, Ativo = true };
        var grupo = new Grupo
        {
            Nome = "Grupo",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = usuarioCriadorId
        };
        var servico = CriarServico(
            grupos: [grupo],
            autorizacao: new AutorizacaoUsuarioServicoStub(admin));

        var erro = await Assert.ThrowsAsync<AcessoNegadoException>(() => servico.RemoverAsync(grupo.Id));

        Assert.Equal("Apenas o criador do grupo pode excluir este grupo.", erro.Message);
    }

    [Fact]
    public async Task RemoverAsync_GrupoComPartidas_BloqueiaParaPreservarHistorico()
    {
        var usuario = new Usuario { Nome = "Owner", Perfil = PerfilUsuario.Atleta, Ativo = true };
        var grupo = new Grupo
        {
            Nome = "Grupo com partidas",
            DataInicio = DateTime.UtcNow,
            UsuarioOrganizadorId = usuario.Id
        };
        var partida = CriarPartidaDoGrupo(grupo.Id);
        var servico = CriarServico(
            grupos: [grupo],
            partidas: [partida],
            autorizacao: new AutorizacaoUsuarioServicoStub(usuario));

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => servico.RemoverAsync(grupo.Id));

        Assert.Equal("Não é possível excluir grupo com partidas vinculadas. Preserve o histórico esportivo.", erro.Message);
    }

    [Fact]
    public async Task ObterDashboardAsync_ComMembrosPartidasERanking_RetornaResumoRankingEAtivos()
    {
        var grupo = new Grupo { Nome = "Grupo dashboard", DataInicio = DateTime.UtcNow, Publico = true };
        var partida = CriarPartidaDoGrupo(grupo.Id);
        partida.PlacarDuplaA = 18;
        partida.PlacarDuplaB = 16;
        var membros = new[] { partida.DuplaA!.Atleta1, partida.DuplaA.Atleta2, partida.DuplaB!.Atleta1, partida.DuplaB.Atleta2 }
            .Where(atleta => atleta is not null)
            .Cast<Atleta>()
            .Select(atleta => new GrupoAtleta { GrupoId = grupo.Id, AtletaId = atleta.Id, Atleta = atleta })
            .ToList();
        var ranking = new RankingServicoStub([
            new RankingCategoriaDto(
                Guid.NewGuid(),
                grupo.Id,
                "Grupo dashboard",
                "Geral",
                null,
                [
                    new RankingAtletaDto(
                        1,
                        membros[0].AtletaId,
                        membros[0].Atleta.Nome,
                        membros[0].Atleta.Apelido,
                        null,
                        null,
                        null,
                        LadoAtleta.Esquerdo,
                        false,
                        false,
                        false,
                        "Regular",
                        3,
                        2,
                        1,
                        0,
                        6m,
                        0m,
                        null,
                        [])
                ])
        ]);
        var servico = CriarServico(
            grupos: [grupo],
            partidas: [partida],
            grupoAtletas: new GrupoAtletaRepositorioStub(membros),
            ranking: ranking);

        var dashboard = await servico.ObterDashboardAsync(grupo.Id);

        Assert.Equal(4, dashboard.Resumo.TotalMembros);
        Assert.Equal(1, dashboard.Resumo.TotalPartidas);
        Assert.Equal(4, dashboard.Resumo.TotalAtletasAtivos);
        Assert.Equal(1, dashboard.Resumo.TotalPartidasSemPlacar);
        Assert.Single(dashboard.Ranking);
        Assert.Single(dashboard.UltimasPartidas);
        Assert.Equal(4, dashboard.MembrosMaisAtivos.Count);
    }

    private static GrupoServico CriarServico(
        IReadOnlyList<Grupo> grupos,
        IReadOnlyList<Partida>? partidas = null,
        GrupoRepositorioStub? grupoRepositorio = null,
        GrupoAtletaRepositorioStub? grupoAtletas = null,
        RankingServicoStub? ranking = null,
        GrupoPadraoServicoStub? grupoPadrao = null,
        AutorizacaoUsuarioServicoStub? autorizacao = null,
        FotoPerfilServiceStub? fotos = null)
    {
        return new GrupoServico(
            grupoRepositorio ?? new GrupoRepositorioStub(grupos),
            grupoAtletas ?? new GrupoAtletaRepositorioStub(),
            new ArenaRepositorioStub(),
            new PartidaRepositorioStub(partidas ?? []),
            ranking ?? new RankingServicoStub(),
            grupoPadrao ?? new GrupoPadraoServicoStub(),
            new UnidadeTrabalhoStub(),
            autorizacao ?? new AutorizacaoUsuarioServicoStub(),
            fotos ?? new FotoPerfilServiceStub(),
            NullLogger<GrupoServico>.Instance);
    }

    private static ArquivoFotoPerfilDto CriarArquivoFoto()
    {
        return new ArquivoFotoPerfilDto(new MemoryStream([1, 2, 3]), "grupo.jpg", "image/jpeg", 3);
    }

    private static Partida CriarPartidaDoGrupo(Guid grupoId)
    {
        var atleta1 = new Atleta { Nome = "Ana Silva" };
        var atleta2 = new Atleta { Nome = "Bruno Costa" };
        var atleta3 = new Atleta { Nome = "Carlos Lima" };
        var atleta4 = new Atleta { Nome = "Daniel Rocha" };
        var duplaA = new Dupla
        {
            Atleta1Id = atleta1.Id,
            Atleta1 = atleta1,
            Atleta2Id = atleta2.Id,
            Atleta2 = atleta2
        };
        var duplaB = new Dupla
        {
            Atleta1Id = atleta3.Id,
            Atleta1 = atleta3,
            Atleta2Id = atleta4.Id,
            Atleta2 = atleta4
        };

        return new Partida
        {
            GrupoId = grupoId,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            DuplaVencedoraId = duplaA.Id,
            TipoRegistroResultado = TipoRegistroResultado.ApenasResultado,
            DataPartida = DateTime.UtcNow
        };
    }

    private sealed class GrupoRepositorioStub(IReadOnlyList<Grupo> grupos) : IGrupoRepositorio
    {
        private readonly List<Grupo> grupos = grupos.ToList();

        public Guid? UltimoUsuarioSelecaoId { get; private set; }
        public Guid? UltimoAtletaSelecaoId { get; private set; }
        public bool? UltimoIncluirPrivadosDeTerceiros { get; private set; }

        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default)
        {
            UltimoUsuarioSelecaoId = usuarioId;
            UltimoAtletaSelecaoId = atletaId;
            UltimoIncluirPrivadosDeTerceiros = incluirPrivadosDeTerceiros;
            return Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        }
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos.Count(x => x.Publico));
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault());
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos.Take(limite).ToList());
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(grupos.Select(x => x.Id).ToList());
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.Any(x => x.Id == grupoId));
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(grupos.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) { grupos.Add(grupo); return Task.CompletedTask; }
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) => grupos.Remove(grupo);
    }

    private sealed class GrupoAtletaRepositorioStub(IReadOnlyList<GrupoAtleta>? membros = null) : IGrupoAtletaRepositorio
    {
        private readonly List<GrupoAtleta> membros = membros?.ToList() ?? [];

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(membros.Where(x => x.GrupoId == grupoId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(membros.Where(x => x.GrupoId == grupoId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) { membros.Add(grupoAtleta); return Task.CompletedTask; }
        public void Remover(GrupoAtleta grupoAtleta) { }
    }

    private sealed class ArenaRepositorioStub : IArenaRepositorio
    {
        public Task<IReadOnlyList<ArenaListagemPublicaResponse>> ListarPublicasAsync(ArenaFiltroPublicoRequest filtro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaListagemPublicaResponse>>([]);
        public Task<ArenaDetalhePublicoResponse?> ObterPublicaPorSlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<ArenaDetalhePublicoResponse?>(null);
        public Task<ArenaResumoPublicoResponse?> ObterResumoPublicoAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ArenaResumoPublicoResponse?>(null);
        public Task<IReadOnlyList<Arena>> ListarAdministradasAsync(Guid usuarioId, bool incluirTodas, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>([]);
        public Task<Arena?> ObterAdminPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<IReadOnlyList<Arena>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Arena>>([]);
        public Task<Arena?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<Arena?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Arena?>(null);
        public Task<bool> ExisteSlugAsync(string slug, Guid? idIgnorado, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<ArenaEspaco>> ListarEspacosPorArenaAsync(Guid arenaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ArenaEspaco>>([]);
        public Task<ArenaEspaco?> ObterEspacoPorIdEArenaAsync(Guid arenaId, Guid espacoId, CancellationToken cancellationToken = default) => Task.FromResult<ArenaEspaco?>(null);
        public Task AdicionarAsync(Arena arena, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AdicionarEspacoAsync(ArenaEspaco espaco, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Arena arena) { }
        public void AtualizarEspaco(ArenaEspaco espaco) { }
        public void Remover(Arena arena) { }
    }

    private sealed class PartidaRepositorioStub(IReadOnlyList<Partida> partidas) : IPartidaRepositorio
    {
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>(partidas.Where(x => x.GrupoId == grupoId).ToList());
        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult(partidas.FirstOrDefault(x => x.GrupoId == grupoId));
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(partidas.FirstOrDefault(x => x.GrupoId == grupoId));
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class RankingServicoStub(IReadOnlyList<RankingCategoriaDto>? rankingGrupo = null) : IRankingServico
    {
        private readonly IReadOnlyList<RankingCategoriaDto> rankingGrupo = rankingGrupo ?? [];

        public Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingFiltroInicialDto(null, null));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingRegiaoFiltroDto([], [], []));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(string? estado, string? cidade, string? bairro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult(rankingGrupo);
        public Task<RankingPaginaDto<RankingDuplaItemDto>> ListarDuplasAsync(Guid? grupoId, string? periodo, int pagina, int tamanhoPagina, string? ordenacao, CancellationToken cancellationToken = default) => Task.FromResult(new RankingPaginaDto<RankingDuplaItemDto>([], Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 100), 0, 0));
        public Task<RankingDuplaDetalheDto> ObterDuplaAsync(string id, Guid? grupoId, string? periodo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RankingPaginaDto<RankingGrupoItemDto>> ListarGruposAsync(Guid? grupoId, string? periodo, int pagina, int tamanhoPagina, string? ordenacao, CancellationToken cancellationToken = default) => Task.FromResult(new RankingPaginaDto<RankingGrupoItemDto>([], Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 100), 0, 0));
        public Task<RankingGrupoDetalheDto> ObterGrupoAsync(Guid id, string? periodo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class GrupoPadraoServicoStub(Exception? excecaoValidacaoNome = null) : IGrupoPadraoServico
    {
        public string NomeGrupoGeral => "Geral";
        public Task<Grupo> ObterOuCriarGrupoGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult(new Grupo { Nome = NomeGrupoGeral, DataInicio = DateTime.UtcNow, Publico = true });
        public Task<Grupo> ResolverGrupoRegistroPartidaAsync(Guid? grupoId, string? nomeNovoGrupo, CancellationToken cancellationToken = default) => Task.FromResult(new Grupo { Nome = NomeGrupoGeral, DataInicio = DateTime.UtcNow, Publico = true });
        public Task ValidarNomeDisponivelOuAcessivelAsync(string nome, Guid? grupoIgnoradoId = null, CancellationToken cancellationToken = default)
        {
            if (excecaoValidacaoNome is not null)
            {
                throw excecaoValidacaoNome;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
        {
            await operacao(cancellationToken);
        }
    }

    private sealed class AutorizacaoUsuarioServicoStub(
        Usuario? usuario = null,
        Exception? excecaoGestaoGrupo = null) : IAutorizacaoUsuarioServico
    {
        private readonly Usuario usuario = usuario ?? new() { Nome = "Usuário", Perfil = PerfilUsuario.Administrador, Ativo = true };

        public bool EhAdministrador(Usuario? usuario) => usuario?.Perfil == PerfilUsuario.Administrador;
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
        {
            if (excecaoGestaoGrupo is not null)
            {
                throw excecaoGestaoGrupo;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FotoPerfilServiceStub : IFotoPerfilService
    {
        public FotoPerfilUploadDto UploadGrupo { get; set; } = new("https://cdn.example/grupo.jpg", "grupo");
        public int EnviosGrupo { get; private set; }
        public List<string> PublicIdsRemovidos { get; } = [];

        public Task<FotoPerfilUploadDto> EnviarAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default) => Task.FromResult(new FotoPerfilUploadDto("https://cdn.example/foto.jpg", "foto"));
        public Task<FotoPerfilUploadDto> EnviarGrupoAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default)
        {
            EnviosGrupo++;
            return Task.FromResult(UploadGrupo);
        }

        public Task RemoverAsync(string publicId, CancellationToken cancellationToken = default)
        {
            PublicIdsRemovidos.Add(publicId);
            return Task.CompletedTask;
        }
    }
}
