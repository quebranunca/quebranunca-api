using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Interfaces.Servicos;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class GrupoResumoUsuarioServicoTests
{
    [Fact]
    public async Task ObterMeuResumoAsync_UsuarioSemGrupo_RetornaNull()
    {
        var cenario = new Cenario();

        var resultado = await cenario.Servico.ObterMeuResumoAsync();

        Assert.Null(resultado);
    }

    [Fact]
    public async Task ObterMeuResumoAsync_ComGrupoUltimoJogoERanking_MontaResumo()
    {
        var cenario = new Cenario();
        var atletaUsuario = CriarAtleta("Ana", "Ana");
        var parceiro = CriarAtleta("Bruno", "Bruno");
        var rival1 = CriarAtleta("Carla", "Carla");
        var rival2 = CriarAtleta("Daniel", "Daniel");
        var grupo = new Grupo { Nome = "Treino Praia" };
        var partida = CriarPartida(grupo, atletaUsuario, parceiro, rival1, rival2, 18, 12);
        cenario.Autorizacao.UsuarioAtual.AtletaId = atletaUsuario.Id;
        cenario.Grupos.ResumoUsuario = grupo;
        cenario.Partidas.UltimaDoAtletaNoGrupo = partida;
        cenario.Ranking.RankingsPorGrupo[grupo.Id] = [CriarRanking(grupo.Id, atletaUsuario, parceiro, rival1)];

        var resultado = await cenario.Servico.ObterMeuResumoAsync();

        Assert.NotNull(resultado);
        Assert.Equal(grupo.Id, resultado.GrupoId);
        Assert.Equal("Treino Praia", resultado.Nome);
        Assert.NotNull(resultado.UltimoJogo);
        Assert.Equal(18, resultado.UltimoJogo.PlacarDupla1);
        Assert.Equal(12, resultado.UltimoJogo.PlacarDupla2);
        Assert.Equal("Ana", resultado.UltimoJogo.Dupla1[0].Apelido);
        Assert.Equal(3, resultado.RankingTop3.Count);
        Assert.True(resultado.RankingTop3.Single(x => x.AtletaId == atletaUsuario.Id).UsuarioLogado);
    }

    [Fact]
    public async Task ListarMeusResumosAsync_Administrador_RespeitaLimiteDeSeisGrupos()
    {
        var cenario = new Cenario();
        cenario.Autorizacao.UsuarioAtual.Perfil = PerfilUsuario.Administrador;
        for (var indice = 1; indice <= 8; indice++)
        {
            cenario.Grupos.Itens.Add(new Grupo { Nome = $"Grupo {indice}" });
        }

        var resultado = await cenario.Servico.ListarMeusResumosAsync();

        Assert.Equal(6, resultado.Count);
        Assert.Equal("Grupo 1", resultado[0].Nome);
        Assert.Equal("Grupo 6", resultado[5].Nome);
    }

    [Fact]
    public async Task ObterDashboardAsync_ComGrupos_MontaTotaisPendenciasEPrivacidade()
    {
        var cenario = new Cenario();
        var usuarioOrganizador = new Usuario { Nome = "Organizador", Perfil = PerfilUsuario.Organizador };
        var atletaPendente = CriarAtleta("Pendente", null);
        var atletaComEmail = CriarAtleta("Com Email", null);
        atletaComEmail.Email = "atleta@qnf.test";
        var atletaUsuario = CriarAtleta("Usuário", null);
        cenario.Autorizacao.UsuarioAtual.AtletaId = atletaUsuario.Id;
        atletaUsuario.Usuario = cenario.Autorizacao.UsuarioAtual;
        var grupoPublico = new Grupo
        {
            Nome = "Grupo Público",
            Publico = true,
            UsuarioOrganizadorId = usuarioOrganizador.Id,
            UsuarioOrganizador = usuarioOrganizador
        };
        grupoPublico.Atletas.Add(new GrupoAtleta { GrupoId = grupoPublico.Id, AtletaId = atletaPendente.Id, Atleta = atletaPendente });
        grupoPublico.Atletas.Add(new GrupoAtleta { GrupoId = grupoPublico.Id, AtletaId = atletaComEmail.Id, Atleta = atletaComEmail });
        grupoPublico.Partidas.Add(CriarPartida(grupoPublico, atletaUsuario, atletaComEmail, atletaPendente, CriarAtleta("Rival", null), 18, 15));
        var grupoPrivado = new Grupo { Nome = "Grupo Privado", Publico = false };
        grupoPrivado.Atletas.Add(new GrupoAtleta { GrupoId = grupoPrivado.Id, AtletaId = atletaUsuario.Id, Atleta = atletaUsuario });
        cenario.Grupos.DashboardUsuario.AddRange([grupoPublico, grupoPrivado]);
        cenario.Ranking.RankingsPorGrupo[grupoPublico.Id] = [CriarRanking(grupoPublico.Id, atletaUsuario, atletaComEmail, atletaPendente)];

        var resultado = await cenario.Servico.ObterDashboardAsync();

        Assert.Equal(2, resultado.Totais.QuantidadeGrupos);
        Assert.Equal(3, resultado.Totais.QuantidadeAtletas);
        Assert.Equal(1, resultado.Totais.QuantidadePartidas);
        Assert.Equal(1, resultado.Totais.PendenciasGrupos);
        Assert.Equal("Público", resultado.Grupos[0].Privacidade);
        Assert.Equal("Organizador", resultado.Grupos[0].NomeUsuarioOrganizador);
        Assert.Equal(1, resultado.Grupos[0].Pendencias);
        Assert.Equal("Privado", resultado.Grupos[1].Privacidade);
    }

    private static Atleta CriarAtleta(string nome, string? apelido)
        => new()
        {
            Nome = nome,
            Apelido = apelido
        };

    private static Partida CriarPartida(Grupo grupo, Atleta atleta1, Atleta atleta2, Atleta atleta3, Atleta atleta4, int placarA, int placarB)
    {
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
            GrupoId = grupo.Id,
            Grupo = grupo,
            Status = StatusPartida.Encerrada,
            StatusAprovacao = StatusAprovacaoPartida.Aprovada,
            DuplaAId = duplaA.Id,
            DuplaA = duplaA,
            DuplaBId = duplaB.Id,
            DuplaB = duplaB,
            DuplaVencedoraId = duplaA.Id,
            TipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
            PlacarDuplaA = placarA,
            PlacarDuplaB = placarB,
            DataPartida = DateTime.UtcNow
        };
    }

    private static RankingCategoriaDto CriarRanking(Guid grupoId, params Atleta[] atletas)
        => new(
            grupoId,
            grupoId,
            "Grupo",
            "Geral",
            GeneroCategoria.Misto,
            atletas.Select((atleta, indice) => new RankingAtletaDto(
                indice + 1,
                atleta.Id,
                atleta.Nome,
                atleta.Apelido,
                null,
                null,
                null,
                LadoAtleta.Ambos,
                true,
                false,
                true,
                "OK",
                10 - indice,
                0,
                0,
                0,
                0,
                0,
                null,
                [])).ToList());

    private sealed class Cenario
    {
        public GrupoRepositorioStub Grupos { get; } = new();
        public PartidaRepositorioStub Partidas { get; } = new();
        public RankingServicoStub Ranking { get; } = new();
        public AutorizacaoUsuarioServicoStub Autorizacao { get; } = new();

        public Cenario()
        {
            Servico = new GrupoResumoUsuarioServico(Grupos, Partidas, Ranking, Autorizacao);
        }

        public GrupoResumoUsuarioServico Servico { get; }
    }

    private sealed class GrupoRepositorioStub : IGrupoRepositorio
    {
        public List<Grupo> Itens { get; } = [];
        public List<Grupo> DashboardUsuario { get; } = [];
        public Grupo? ResumoUsuario { get; set; }

        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>(Itens);

        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>([]);

        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(ResumoUsuario);

        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>(Itens.Take(limite).ToList());

        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>(DashboardUsuario);

        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default)
            => Task.FromResult<Grupo?>(null);

        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default)
            => Task.FromResult<Grupo?>(null);

        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Grupo>>([]);

        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Itens.Concat(DashboardUsuario).FirstOrDefault(x => x.Id == id));

        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class PartidaRepositorioStub : IPartidaRepositorio
    {
        public Partida? UltimaDoAtletaNoGrupo { get; set; }
        public Partida? UltimaDoGrupo { get; set; }

        public Task<IReadOnlyList<Partida>> ListarPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<int> ContarRegistradasAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Partida>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarAdministracaoAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarFeedAsync(int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorDiaAsync(DateTime inicioUtc, DateTime fimUtc, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarPorAtletaParaRemocaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarReferenciandoPartidasAsync(IReadOnlyCollection<Guid> partidaIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult(UltimaDoGrupo);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(UltimaDoAtletaNoGrupo);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(new UsuarioResumoDto("Usuario", 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class RankingServicoStub : IRankingServico
    {
        public Dictionary<Guid, IReadOnlyList<RankingCategoriaDto>> RankingsPorGrupo { get; } = [];

        public Task<RankingFiltroInicialDto> ObterFiltroInicialAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasGeralAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RankingRegiaoFiltroDto> ListarRegioesDisponiveisAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorRegiaoAsync(string? estado, string? cidade, string? bairro, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RankingCategoriaDto>> ListarAtletasPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult(RankingsPorGrupo.GetValueOrDefault(grupoId, []));
        public Task<RankingPaginaDto<RankingDuplaItemDto>> ListarDuplasAsync(Guid? grupoId, string? periodo, int pagina, int tamanhoPagina, string? ordenacao, CancellationToken cancellationToken = default) => Task.FromResult(new RankingPaginaDto<RankingDuplaItemDto>([], Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 100), 0, 0));
        public Task<RankingDuplaDetalheDto> ObterDuplaAsync(string id, Guid? grupoId, string? periodo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RankingPaginaDto<RankingGrupoItemDto>> ListarGruposAsync(Guid? grupoId, string? periodo, int pagina, int tamanhoPagina, string? ordenacao, CancellationToken cancellationToken = default) => Task.FromResult(new RankingPaginaDto<RankingGrupoItemDto>([], Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 100), 0, 0));
        public Task<RankingGrupoDetalheDto> ObterGrupoAsync(Guid id, string? periodo, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        public Usuario UsuarioAtual { get; } = new()
        {
            Nome = "Usuário",
            Email = "usuario@qnf.test",
            Perfil = PerfilUsuario.Atleta,
            Ativo = true
        };

        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(UsuarioAtual);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => Task.FromResult(UsuarioAtual);
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
