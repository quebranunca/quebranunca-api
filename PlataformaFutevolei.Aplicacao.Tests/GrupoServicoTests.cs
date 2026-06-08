using Microsoft.Extensions.Logging.Abstractions;
using PlataformaFutevolei.Aplicacao.DTOs;
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

    private static GrupoServico CriarServico(
        IReadOnlyList<Grupo> grupos,
        IReadOnlyList<Partida>? partidas = null)
    {
        return new GrupoServico(
            new GrupoRepositorioStub(grupos),
            new GrupoAtletaRepositorioStub(),
            new ArenaRepositorioStub(),
            new PartidaRepositorioStub(partidas ?? []),
            new RankingServicoStub(),
            new GrupoPadraoServicoStub(),
            new UnidadeTrabalhoStub(),
            new AutorizacaoUsuarioServicoStub(),
            new FotoPerfilServiceStub(),
            NullLogger<GrupoServico>.Instance);
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
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(grupos.Count(x => x.Publico));
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault());
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>(grupos.Take(limite).ToList());
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>(grupos.Select(x => x.Id).ToList());
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(grupos.Any(x => x.Id == grupoId));
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(grupos.FirstOrDefault(x => x.Nome == nome));
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult(grupos);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(grupos.FirstOrDefault(x => x.Id == id));
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class GrupoAtletaRepositorioStub : IGrupoAtletaRepositorio
    {
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>([]);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<GrupoAtleta?>(null);
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
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

    private sealed class RankingServicoStub : IRankingServico
    {
        public Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingFiltroInicialDto(null, null));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(CancellationToken cancellationToken = default) => Task.FromResult(new RankingRegiaoFiltroDto([], [], []));
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(string? estado, string? cidade, string? bairro, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RankingCategoriaDto>>([]);
    }

    private sealed class GrupoPadraoServicoStub : IGrupoPadraoServico
    {
        public string NomeGrupoGeral => "Geral";
        public Task<Grupo> ObterOuCriarGrupoGeralAsync(CancellationToken cancellationToken = default) => Task.FromResult(new Grupo { Nome = NomeGrupoGeral, DataInicio = DateTime.UtcNow, Publico = true });
        public Task<Grupo> ResolverGrupoRegistroPartidaAsync(Guid? grupoId, string? nomeNovoGrupo, CancellationToken cancellationToken = default) => Task.FromResult(new Grupo { Nome = NomeGrupoGeral, DataInicio = DateTime.UtcNow, Publico = true });
        public Task ValidarNomeDisponivelOuAcessivelAsync(string nome, Guid? grupoIgnoradoId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class UnidadeTrabalhoStub : IUnidadeTrabalho
    {
        public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public async Task ExecutarEmTransacaoAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken = default)
        {
            await operacao(cancellationToken);
        }
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        private readonly Usuario usuario = new() { Nome = "Usuário", Perfil = PerfilUsuario.Administrador, Ativo = true };
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(usuario);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(usuario);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FotoPerfilServiceStub : IFotoPerfilService
    {
        public Task<FotoPerfilUploadDto> EnviarAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default) => Task.FromResult(new FotoPerfilUploadDto("https://cdn.example/foto.jpg", "foto"));
        public Task<FotoPerfilUploadDto> EnviarGrupoAsync(ArquivoFotoPerfilDto arquivo, CancellationToken cancellationToken = default) => Task.FromResult(new FotoPerfilUploadDto("https://cdn.example/grupo.jpg", "grupo"));
        public Task RemoverAsync(string publicId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
