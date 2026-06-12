using PlataformaFutevolei.Aplicacao.DTOs;
using PlataformaFutevolei.Aplicacao.Interfaces.Repositorios;
using PlataformaFutevolei.Aplicacao.Interfaces.Seguranca;
using PlataformaFutevolei.Aplicacao.Servicos;
using PlataformaFutevolei.Dominio.Entidades;
using PlataformaFutevolei.Dominio.Enums;
using Xunit;

namespace PlataformaFutevolei.Aplicacao.Tests;

public class RankingServicoGrupoTests
{
    [Fact]
    public async Task ListarAtletasPorGrupoAsync_IncluiMembrosSemPartidasOuPontuacao()
    {
        var cenario = Cenario.Criar();
        var atletaComZero = cenario.CriarAtleta("Atleta Com Zero", cadastroPendente: true);
        var atletaAtivo = cenario.CriarAtleta("Atleta Ativo");
        cenario.AdicionarMembro(atletaComZero);
        cenario.AdicionarMembro(atletaAtivo);

        var ranking = await cenario.Servico.ListarAtletasPorGrupoAsync(cenario.Grupo.Id);

        var categoria = Assert.Single(ranking);
        Assert.Equal("Ranking do grupo", categoria.NomeCategoria);
        Assert.Equal(2, categoria.Atletas.Count);
        Assert.All(categoria.Atletas, atleta =>
        {
            Assert.Equal(0, atleta.Pontos);
            Assert.Equal(0, atleta.Jogos);
            Assert.Equal(0, atleta.Vitorias);
            Assert.Equal(0, atleta.Derrotas);
        });
        Assert.Contains(categoria.Atletas, atleta => atleta.NomeAtleta == "Atleta Com Zero" && atleta.CadastroPendente);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_IncluiParticipantesQueJogaramSemVinculoDeMembro()
    {
        var cenario = Cenario.Criar();
        var membroSemPartida = cenario.CriarAtleta("Membro Sem Partida");
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        cenario.AdicionarMembro(membroSemPartida);
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Contains(categoria.Atletas, x => x.AtletaId == membroSemPartida.Id && x.Jogos == 0);
        Assert.Contains(categoria.Atletas, x => x.AtletaId == vencedor1.Id && x.Jogos == 1 && x.Vitorias == 1);
        Assert.Contains(categoria.Atletas, x => x.AtletaId == perdedor1.Id && x.Jogos == 1 && x.Derrotas == 1);
        Assert.Equal(5, categoria.Atletas.Count);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_NaoDuplicaMembroQueTambemJogou()
    {
        var cenario = Cenario.Criar();
        var membroVencedor = cenario.CriarAtleta("Membro Vencedor");
        var parceiro = cenario.CriarAtleta("Parceiro");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        cenario.AdicionarMembro(membroVencedor);
        cenario.AdicionarPartida(membroVencedor, parceiro, perdedor1, perdedor2);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Single(categoria.Atletas.Where(x => x.AtletaId == membroVencedor.Id));
        Assert.Equal(4, categoria.Atletas.Count);
        Assert.Equal(1, categoria.Atletas.Single(x => x.AtletaId == membroVencedor.Id).Jogos);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_IgnoraPartidasDeOutrosGrupos()
    {
        var cenario = Cenario.Criar();
        var grupoOutro = new Grupo { Nome = "Outro Grupo", DataInicio = DateTime.UtcNow };
        var atletaGrupo = cenario.CriarAtleta("Atleta Grupo");
        var parceiroGrupo = cenario.CriarAtleta("Parceiro Grupo");
        var adversarioGrupo1 = cenario.CriarAtleta("Adversario Grupo Um");
        var adversarioGrupo2 = cenario.CriarAtleta("Adversario Grupo Dois");
        var atletaOutroGrupo = cenario.CriarAtleta("Atleta Outro Grupo");
        var parceiroOutroGrupo = cenario.CriarAtleta("Parceiro Outro Grupo");
        var adversarioOutro1 = cenario.CriarAtleta("Adversario Outro Um");
        var adversarioOutro2 = cenario.CriarAtleta("Adversario Outro Dois");
        cenario.AdicionarPartida(atletaGrupo, parceiroGrupo, adversarioGrupo1, adversarioGrupo2);
        cenario.AdicionarPartida(
            atletaOutroGrupo,
            parceiroOutroGrupo,
            adversarioOutro1,
            adversarioOutro2,
            grupo: grupoOutro);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Contains(categoria.Atletas, x => x.AtletaId == atletaGrupo.Id);
        Assert.DoesNotContain(categoria.Atletas, x => x.AtletaId == atletaOutroGrupo.Id);
        Assert.Equal(4, categoria.Atletas.Count);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_PartidaApenasResultadoPontuaVencedoresSemPlacar()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        cenario.AdicionarPartida(
            vencedor1,
            vencedor2,
            perdedor1,
            perdedor2,
            tipoRegistroResultado: TipoRegistroResultado.ApenasResultado,
            placarA: null,
            placarB: null);

        var categoria = await cenario.ObterRankingGrupoAsync();

        var vencedor = categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id);
        var perdedor = categoria.Atletas.Single(x => x.AtletaId == perdedor1.Id);
        Assert.Equal(1, vencedor.Jogos);
        Assert.Equal(1, vencedor.Vitorias);
        Assert.Equal(2m, vencedor.Pontos);
        Assert.Equal(0, perdedor.Pontos);
        Assert.Contains("sem placar detalhado", vencedor.Partidas.Single().Confronto);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_TrataAprovadasPendentesEContestadasConformeRegraAtual()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, statusAprovacao: StatusAprovacaoPartida.Aprovada, diasAtras: 3);
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, statusAprovacao: StatusAprovacaoPartida.PendenteAprovacao, diasAtras: 2);
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, statusAprovacao: StatusAprovacaoPartida.Contestada, diasAtras: 1);

        var categoria = await cenario.ObterRankingGrupoAsync();

        var vencedor = categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id);
        Assert.Equal(3, vencedor.Jogos);
        Assert.Equal(3, vencedor.Vitorias);
        Assert.Equal(4m, vencedor.Pontos);
        Assert.Equal(2m, vencedor.PontosPendentes);
        Assert.Contains(vencedor.Partidas, x => x.Resultado == "Vitória");
        Assert.Equal(2, vencedor.Partidas.Count(x => x.Resultado == "Vitória pendente"));
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_PendenteDeVinculosContaComoPontuacaoPendente()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        cenario.AdicionarPartida(
            vencedor1,
            vencedor2,
            perdedor1,
            perdedor2,
            statusAprovacao: StatusAprovacaoPartida.PendenteDeVinculos);

        var categoria = await cenario.ObterRankingGrupoAsync();

        var vencedor = categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id);
        Assert.Equal(1, vencedor.Jogos);
        Assert.Equal(1, vencedor.Vitorias);
        Assert.Equal(1m, vencedor.Pontos);
        Assert.Equal(1m, vencedor.PontosPendentes);
        Assert.Equal("Vitória pendente", vencedor.Partidas.Single().Resultado);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_OrdenaPorPontosDecrescente()
    {
        var cenario = Cenario.Criar();
        var lider1 = cenario.CriarAtleta("Lider Um");
        var lider2 = cenario.CriarAtleta("Lider Dois");
        var segundo1 = cenario.CriarAtleta("Segundo Um");
        var segundo2 = cenario.CriarAtleta("Segundo Dois");
        var terceiro1 = cenario.CriarAtleta("Terceiro Um");
        var terceiro2 = cenario.CriarAtleta("Terceiro Dois");
        cenario.AdicionarPartida(lider1, lider2, terceiro1, terceiro2);
        cenario.AdicionarPartida(lider1, lider2, segundo1, segundo2);
        cenario.AdicionarPartida(segundo1, segundo2, terceiro1, terceiro2);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Equal(
            ["Lider Dois", "Lider Um", "Segundo Dois", "Segundo Um", "Terceiro Dois", "Terceiro Um"],
            categoria.Atletas.Select(x => x.NomeAtleta).ToList());
        Assert.Equal([4m, 4m, 2m, 2m, 0m, 0m], categoria.Atletas.Select(x => x.Pontos).ToList());
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_DesempataPorVitoriasEDepoisNome()
    {
        var cenario = Cenario.Criar();
        var maisVitorias = cenario.CriarAtleta("Ana Duas Vitorias");
        var parceiroMaisVitorias = cenario.CriarAtleta("Bruno Duas Vitorias");
        var umaVitoria = cenario.CriarAtleta("Carlos Uma Vitoria");
        var parceiroUmaVitoria = cenario.CriarAtleta("Diego Uma Vitoria");
        var adversario1 = cenario.CriarAtleta("Eduardo Adversario");
        var adversario2 = cenario.CriarAtleta("Felipe Adversario");
        cenario.AdicionarPartida(umaVitoria, parceiroUmaVitoria, adversario1, adversario2, statusAprovacao: StatusAprovacaoPartida.Aprovada);
        cenario.AdicionarPartida(maisVitorias, parceiroMaisVitorias, adversario1, adversario2, statusAprovacao: StatusAprovacaoPartida.PendenteAprovacao, diasAtras: 2);
        cenario.AdicionarPartida(maisVitorias, parceiroMaisVitorias, adversario1, adversario2, statusAprovacao: StatusAprovacaoPartida.PendenteAprovacao, diasAtras: 1);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Equal(2m, categoria.Atletas.Single(x => x.AtletaId == maisVitorias.Id).Pontos);
        Assert.Equal(2m, categoria.Atletas.Single(x => x.AtletaId == umaVitoria.Id).Pontos);
        Assert.Equal(
            ["Ana Duas Vitorias", "Bruno Duas Vitorias", "Carlos Uma Vitoria", "Diego Uma Vitoria"],
            categoria.Atletas.Take(4).Select(x => x.NomeAtleta).ToList());
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_UsaPontosParticipacaoDaRegraConfiguravelDaCompeticao()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        var categoriaCompeticao = Cenario.CriarCategoriaCompeticao(pontosParticipacao: 5m);
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, categoriaCompeticao: categoriaCompeticao);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Equal(9m, categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id).Pontos);
        Assert.Equal(5m, categoria.Atletas.Single(x => x.AtletaId == perdedor1.Id).Pontos);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_UsaVitoriaEDerrotaDaRegraConfiguravelDaCompeticao()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        var categoriaCompeticao = Cenario.CriarCategoriaCompeticao(
            pontosVitoria: 6m,
            pontosDerrota: 2m);
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, categoriaCompeticao: categoriaCompeticao);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Equal(7m, categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id).Pontos);
        Assert.Equal(2m, categoria.Atletas.Single(x => x.AtletaId == perdedor1.Id).Pontos);
    }

    [Fact]
    public async Task ListarAtletasPorGrupoAsync_UsaPontuacaoPadraoQuandoCompeticaoNaoTemRegraCustomizada()
    {
        var cenario = Cenario.Criar();
        var vencedor1 = cenario.CriarAtleta("Vencedor Um");
        var vencedor2 = cenario.CriarAtleta("Vencedor Dois");
        var perdedor1 = cenario.CriarAtleta("Perdedor Um");
        var perdedor2 = cenario.CriarAtleta("Perdedor Dois");
        var categoriaCompeticao = Cenario.CriarCategoriaCompeticao();
        cenario.AdicionarPartida(vencedor1, vencedor2, perdedor1, perdedor2, categoriaCompeticao: categoriaCompeticao);

        var categoria = await cenario.ObterRankingGrupoAsync();

        Assert.Equal(4m, categoria.Atletas.Single(x => x.AtletaId == vencedor1.Id).Pontos);
        Assert.Equal(0m, categoria.Atletas.Single(x => x.AtletaId == perdedor1.Id).Pontos);
    }

    private sealed class Cenario
    {
        private readonly List<GrupoAtleta> vinculos = [];
        private readonly List<Partida> partidas = [];

        private Cenario()
        {
            Grupo = new Grupo { Nome = "Fechadinho de Quinta", DataInicio = DateTime.UtcNow };
            Servico = new RankingServico(
                new LigaRepositorioStub(),
                new CompeticaoRepositorioStub(),
                new GrupoRepositorioStub(Grupo),
                new GrupoAtletaRepositorioMemoria(vinculos),
                new PartidaRepositorioMemoria(partidas),
                new AutorizacaoUsuarioServicoStub());
        }

        public Grupo Grupo { get; }
        public RankingServico Servico { get; }

        public static Cenario Criar() => new();

        public Atleta CriarAtleta(string nome, bool cadastroPendente = false)
            => new()
            {
                Nome = nome,
                Apelido = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
                CadastroPendente = cadastroPendente
            };

        public void AdicionarMembro(Atleta atleta)
            => vinculos.Add(new GrupoAtleta
            {
                GrupoId = Grupo.Id,
                AtletaId = atleta.Id,
                Atleta = atleta
            });

        public void AdicionarPartida(
            Atleta duplaAAtleta1,
            Atleta duplaAAtleta2,
            Atleta duplaBAtleta1,
            Atleta duplaBAtleta2,
            int? placarA = 21,
            int? placarB = 18,
            TipoRegistroResultado tipoRegistroResultado = TipoRegistroResultado.PlacarDetalhado,
            StatusAprovacaoPartida statusAprovacao = StatusAprovacaoPartida.Aprovada,
            int diasAtras = 0,
            CategoriaCompeticao? categoriaCompeticao = null,
            Grupo? grupo = null)
        {
            var grupoPartida = grupo ?? Grupo;
            var duplaA = CriarDupla(duplaAAtleta1, duplaAAtleta2);
            var duplaB = CriarDupla(duplaBAtleta1, duplaBAtleta2);
            var partida = new Partida
            {
                GrupoId = grupoPartida.Id,
                Grupo = grupoPartida,
                CategoriaCompeticaoId = categoriaCompeticao?.Id,
                CategoriaCompeticao = categoriaCompeticao,
                DuplaAId = duplaA.Id,
                DuplaA = duplaA,
                DuplaBId = duplaB.Id,
                DuplaB = duplaB,
                DuplaVencedoraId = duplaA.Id,
                DuplaVencedora = duplaA,
                PlacarDuplaA = placarA,
                PlacarDuplaB = placarB,
                TipoRegistroResultado = tipoRegistroResultado,
                Status = StatusPartida.Encerrada,
                StatusAprovacao = statusAprovacao,
                DataPartida = DateTime.UtcNow.Date.AddDays(-diasAtras)
            };

            if (categoriaCompeticao is not null)
            {
                categoriaCompeticao.Partidas.Add(partida);
            }

            partidas.Add(partida);
        }

        public async Task<RankingCategoriaDto> ObterRankingGrupoAsync()
        {
            var ranking = await Servico.ListarAtletasPorGrupoAsync(Grupo.Id);
            return Assert.Single(ranking);
        }

        public static CategoriaCompeticao CriarCategoriaCompeticao(
            decimal? pontosParticipacao = null,
            decimal? pontosVitoria = null,
            decimal? pontosDerrota = null)
        {
            var possuiRegraCustomizada = pontosParticipacao.HasValue ||
                pontosVitoria.HasValue ||
                pontosDerrota.HasValue;
            var competicao = new Competicao
            {
                Nome = "Circuito QNF",
                Tipo = TipoCompeticao.Campeonato,
                DataInicio = DateTime.UtcNow.Date,
                RegraCompeticao = possuiRegraCustomizada
                    ? new RegraCompeticao
                    {
                        Nome = "Regra customizada",
                        PontosParticipacao = pontosParticipacao ?? Competicao.PontosParticipacaoPadrao,
                        PontosVitoria = pontosVitoria ?? Competicao.PontosVitoriaPadrao,
                        PontosDerrota = pontosDerrota ?? Competicao.PontosDerrotaPadrao
                    }
                    : null
            };
            var categoria = new CategoriaCompeticao
            {
                Nome = "Open",
                CompeticaoId = competicao.Id,
                Competicao = competicao,
                Genero = GeneroCategoria.Misto,
                Nivel = NivelCategoria.Livre
            };
            competicao.Categorias.Add(categoria);
            return categoria;
        }

        private static Dupla CriarDupla(Atleta atleta1, Atleta atleta2)
            => new()
            {
                Nome = $"{atleta1.Nome} / {atleta2.Nome}",
                Atleta1Id = atleta1.Id,
                Atleta1 = atleta1,
                Atleta2Id = atleta2.Id,
                Atleta2 = atleta2
            };
    }

    private sealed class LigaRepositorioStub : ILigaRepositorio
    {
        public Task<IReadOnlyList<Liga>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Liga>>([]);
        public Task<Liga?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Liga?>(null);
        public Task<Liga?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Liga?>(null);
        public Task AdicionarAsync(Liga liga, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Liga liga) { }
        public void Remover(Liga liga) { }
    }

    private sealed class CompeticaoRepositorioStub : ICompeticaoRepositorio
    {
        public Task<IReadOnlyList<Competicao>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Competicao>>([]);
        public Task<Competicao?> ObterGrupoResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid competicaoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Competicao?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task<Competicao?> ObterPorIdComCategoriasAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Competicao?>(null);
        public Task AdicionarAsync(Competicao competicao, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Competicao competicao) { }
        public void Remover(Competicao competicao) { }
    }

    private sealed class GrupoRepositorioStub(Grupo grupo) : IGrupoRepositorio
    {
        public Task<IReadOnlyList<Grupo>> ListarAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarParaSelecaoAsync(Guid usuarioId, Guid? atletaId, bool incluirPrivadosDeTerceiros, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<int> ContarPublicosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<Grupo?> ObterResumoUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarResumosUsuarioAsync(Guid usuarioId, Guid? atletaId, int limite, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Grupo>> ListarDashboardUsuarioAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<IReadOnlyList<Guid>> ListarIdsComAcessoAtletaAsync(Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<bool> AtletaPossuiAcessoAsync(Guid grupoId, Guid usuarioId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Grupo?> ObterPorNomeEOrganizadorAsync(string nome, Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<Grupo?> ObterPorNomeNormalizadoAsync(string nome, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(null);
        public Task<IReadOnlyList<Grupo>> ListarPorUsuarioOrganizadorParaAtualizacaoAsync(Guid usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Grupo>>([]);
        public Task<Grupo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Grupo?>(id == grupo.Id ? grupo : null);
        public Task AdicionarAsync(Grupo grupo, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Grupo grupo) { }
        public void Remover(Grupo grupo) { }
    }

    private sealed class GrupoAtletaRepositorioMemoria(IReadOnlyList<GrupoAtleta> vinculos) : IGrupoAtletaRepositorio
    {
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GrupoAtleta>>(vinculos.Where(x => x.GrupoId == grupoId).ToList());

        public Task<IReadOnlyList<GrupoAtleta>> ListarPorGrupoParaAtualizacaoAsync(Guid grupoId, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> BuscarPorGrupoAsync(Guid grupoId, string termo, CancellationToken cancellationToken = default) => ListarPorGrupoAsync(grupoId, cancellationToken);
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GrupoAtleta>>(vinculos.Where(x => x.AtletaId == atletaId).ToList());
        public Task<IReadOnlyList<GrupoAtleta>> ListarPorAtletaParaAtualizacaoAsync(Guid atletaId, CancellationToken cancellationToken = default) => ListarPorAtletaAsync(atletaId, cancellationToken);
        public Task<GrupoAtleta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(vinculos.FirstOrDefault(x => x.Id == id));
        public Task<GrupoAtleta?> ObterPorGrupoEAtletaAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(vinculos.FirstOrDefault(x => x.GrupoId == grupoId && x.AtletaId == atletaId));
        public Task AdicionarAsync(GrupoAtleta grupoAtleta, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remover(GrupoAtleta grupoAtleta) { }
    }

    private sealed class PartidaRepositorioMemoria(IReadOnlyList<Partida> partidas) : IPartidaRepositorio
    {
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
        public Task<Partida?> ObterUltimaDoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<Partida?> ObterUltimaDoAtletaNoGrupoAsync(Guid grupoId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task<IReadOnlyList<Partida>> ListarComAtletasPendentesPorUsuarioCriadorAsync(Guid usuarioId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarComPendenteDeVinculoPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<bool> ExisteAtletaPendenteEmPartidaCriadaPorUsuarioAsync(Guid usuarioId, Guid atletaId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IReadOnlyList<Partida>> ListarParaRankingGeralAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorLigaAsync(Guid ligaId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingSemCompeticaoOuCategoriaAsync(Guid? usuarioOrganizadorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Partida>>([]);
        public Task<IReadOnlyList<Partida>> ListarParaRankingPorGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Partida>>(partidas.Where(x => x.GrupoId == grupoId).ToList());
        public Task<Guid?> ObterUltimaCompeticaoComPartidaEncerradaAsync(Guid? usuarioOrganizadorId, Guid? atletaId, CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(null);
        public Task<AtletasSugestoesPartidaDto> ObterSugestoesPartidaAsync(Guid atletaId, Guid? grupoId, int limitePorSecao, CancellationToken cancellationToken = default) => Task.FromResult(new AtletasSugestoesPartidaDto([], []));
        public Task<UsuarioResumoDto> ObterResumoUsuarioPorAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default)
            => Task.FromResult(new UsuarioResumoDto(string.Empty, 0, 0, 0, 0, 0, 0, 0));
        public Task<Partida?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Partida?>(null);
        public Task AdicionarAsync(Partida partida, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Atualizar(Partida partida) { }
        public void Remover(Partida partida) { }
    }

    private sealed class AutorizacaoUsuarioServicoStub : IAutorizacaoUsuarioServico
    {
        public Task<Usuario?> ObterUsuarioAtualAsync(CancellationToken cancellationToken = default) => Task.FromResult<Usuario?>(null);
        public Task<Usuario> ObterUsuarioAtualObrigatorioAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException();
        public Task GarantirAdministradorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAdminOuOrganizadorAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirAcessoAtletaAsync(Guid atletaId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoCompeticaoAsync(Guid competicaoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task GarantirGestaoGrupoAsync(Guid grupoId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
